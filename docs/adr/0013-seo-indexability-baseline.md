# 0013 — Base d'indexabilité (vrai 404, sitemap généré, canonical/OG par route)

- Statut : accepté
- Date : 2026-09-04

## Contexte

Le site n'avait aucune page indexée par Google : `robots.txt` et `sitemap.xml` étaient absents, et
`Program.cs` routait **toute** URL non `/api/*` vers `index.html` avec un statut 200
(`MapFallbackToFile("{*path:regex(^(?!api/).*$):nonfile}", "index.html")`) — y compris une URL
inexistante (soft-404 généralisé) et `/aide`, dont le HTML prérendu propre existe bien
(`dist/browser/aide/index.html`, avec le bon `<title>` via le `TitleStrategy` natif du Router) mais
dont `<link rel="canonical">`/`og:url`/`og:title` restaient ceux de l'accueil : rien dans l'app ne
les mettait à jour par route.

Une piste initialement envisagée (calquée sur un audit générique) supposait un frontal nginx avec
directives `location`/`gzip`/`add_header`, et un rendu serveur (`RenderMode.Server`) pour la 404.
Aucune des deux ne s'applique ici : ce site est une unique application ASP.NET Core sur IIS/ANCM
(`docs/adr/0004`, `docs/adr/0009`), sans nginx, et le frontend est un prérendu statique (SSG,
`outputMode: "static"`) sans Node en production (`docs/adr/0004` décision 2) — un `RenderMode.Server`
sur la route 404 exigerait justement le runtime Node que cette décision écarte.

## Décision

**Fallback serveur déterministe.** `Program.cs` remplace `MapFallbackToFile` par un
`MapFallback("{*path:nonfile}", ...)` qui décide explicitement, sans dépendre du comportement (non
vérifié empiriquement, et donc jugé trop fragile pour en dépendre) de `UseDefaultFiles` sur une URL
sans slash final :
- un chemin sous `api/` reçoit un 404 nu, jamais le HTML de la page 404 ;
- si `wwwroot/<chemin>/index.html` existe, il est servi avec 200 (couvre `/`, `/aide`, `/aide/`,
  et toute route prérendue future, avec ou sans slash final — le chemin est normalisé en amont) ;
- sinon, `wwwroot/404/index.html` est servi mais avec le statut **404** explicite : le contenu est
  la page stylée du site, mais le code HTTP vu par Google (et par `curl -sI`) est un vrai 404, pas
  un 200 déguisé.

**Piège découvert en vérifiant contre une instance réelle (`dotnet publish` + `wwwroot` copié +
`curl`, pas seulement les tests) : la contrainte de route `nonfile` est indispensable, pas
cosmétique.** Une première version utilisait un motif `{**path}` sans contrainte, en supposant que
`UseStaticFiles`/`UseDefaultFiles` (positionnés avant dans le fichier) auraient de toute façon la
priorité sur un fallback positionné après. C'est faux : le routage par endpoints d'ASP.NET Core
sélectionne l'endpoint correspondant **avant** que `UseStaticFiles` ne s'exécute, et ce middleware,
conscient du routage, **cède la place** dès qu'un endpoint est déjà sélectionné pour la requête —
y compris pour un fichier bien réel sur disque. Un motif de fallback non contraint matche
littéralement tout, donc `UseStaticFiles` ne servait plus aucun fichier : `main-*.js`, `robots.txt`,
`sitemap.xml`, toutes les images se retrouvaient à recevoir la page 404 stylée. La contrainte
`nonfile` (même mécanisme que l'ancien motif, dont c'était déjà tout l'intérêt) exclut du fallback
tout chemin dont le dernier segment ressemble à un fichier (contient un point) : ces requêtes ne
matchent alors plus aucun endpoint, laissant `UseStaticFiles` faire son travail normalement.
`tests/UMonsPlanning.Backend.Tests/StaticFallbackTests.cs` porte un test de non-régression dédié
(`GetRealStaticFile_IsServedByStaticFileMiddleware_NotSwallowedByTheFallback`).

**Page 404 comme route Angular concrète.** `app.routes.ts` ajoute `{ path: '404', ... }` (donc
prérendue en `dist/browser/404/index.html`, comme n'importe quelle autre route) et un `{ path: '**' }`
qui pointe vers le même composant — filet de sécurité purement côté client (une navigation Angular
interne vers une route inconnue), jamais atteint pour une URL frappée directement puisque le
fallback serveur ci-dessus l'intercepte avant même que l'Angular ne s'exécute.
`app.routes.server.ts` reste inchangé : son unique route `**` en `RenderMode.Prerender` signifie
déjà "prérends chaque route concrète du Router", pas "prérends une page à l'URL littérale `**`" —
confirmé par `prerendered-routes.json`, qui ne liste que des chemins concrets.

**Sitemap généré depuis le manifeste réel de prérendu, pas une liste maintenue à la main.** Nouveau
script `src/UMonsPlanning.Frontend/scripts/generate-sitemap.mjs`, câblé en `postbuild` dans
`package.json` (se déclenche automatiquement après tout `npm run build`, y compris avec des
`--configuration` supplémentaires — donc aucune modification de `.github/workflows/ci-cd.yml`
n'était nécessaire). Il lit `dist/UMonsPlanning.Frontend/prerendered-routes.json` (déjà écrit par
Angular à chaque build) plutôt qu'une liste de routes dupliquée dans le script : une route future
n'a rien à changer ici pour apparaître dans le sitemap, et une route jamais réellement prérendue
(donc jamais un vrai 200) ne peut structurellement pas s'y retrouver. La route `/404` en est
explicitement exclue. L'origine absolue est relue depuis le `<link rel="canonical">` déjà présent
dans `browser/index.html` (voir point suivant) plutôt que dupliquée depuis `environment.ts` dans un
script Node séparé — une seule source de vérité, cohérente que le build soit prod ou
prod+staging.

**`robots.txt` reste un asset statique** (`public/robots.txt`, copié tel quel), identique en test et
en production (le `Sitemap:` qu'il déclare pointe toujours vers le domaine de prod). Écart assumé :
le sous-domaine de test n'a aujourd'hui aucun backlink ni sitemap soumis à Search Console, le risque
d'indexation croisée est jugé négligeable — à revoir si ce sous-domaine venait à être exposé
publiquement d'une autre façon.

**Canonical/OG/description corrects par route, gravés dans le HTML prérendu.** Nouveau
`src/app/core/seo-meta.service.ts` : sur chaque `NavigationEnd`, lit `data.description`/
`data.noIndex` de la route active et met à jour, via `Meta` (`@angular/platform-browser`) et
`DOCUMENT` (pour `<link rel="canonical">`, que `Meta` ne gère pas) : la meta description, `og:url`/
`og:title`/`og:description`, `twitter:title`/`twitter:description`, et un `<meta name="robots"
content="noindex">` ajouté/retiré selon `data.noIndex`. Démarré via `provideAppInitializer` dans
`app.config.ts` (sans ça, le service ne serait jamais instancié : rien d'autre ne l'injecte). Ce
service tourne aussi pendant le prérendu — le Router y déclenche déjà `NavigationEnd`, c'est
exactement pourquoi le `<title>` par route (mécanisme natif du Router) est déjà correct aujourd'hui
avant même ce lot — donc les valeurs sont correctes dans le HTML statique servi, pas seulement
après hydratation.

**Nouveau champ d'environnement `baseUrl`**, même mécanisme que `isTestEnvironment`
(`docs/adr/0011`, qui anticipait déjà que toute variable propre à un environnement suivrait ce
même mécanisme) : `https://umonsplanning.pellichero.be` en production,
`https://test.umonsplanning.pellichero.be` en `staging`. Sert à construire le canonical/`og:url`
absolu (`{baseUrl}{router.url}`) — nécessairement une constante de build, puisqu'il n'y a pas de
requête serveur au moment du prérendu (contrairement au lien de calendrier généré côté client, qui
peut lui utiliser `window.location.origin` sans configuration par environnement, `docs/adr/0004`
décision 3).

## Conséquences

- Aucune nouvelle dépendance (NuGet ou npm) : `MapFallback`, `Meta`, `provideAppInitializer` sont
  tous natifs des paquets déjà référencés.
- `Program.cs` a désormais un unique point de décision pour le routage des URL non gérées par une
  API/un fichier statique exact, au lieu de la combinaison implicite précédente
  (`UseDefaultFiles`/`UseStaticFiles`/`MapFallbackToFile`) — plus simple à raisonner et à tester
  (`tests/UMonsPlanning.Backend.Tests/StaticFallbackTests.cs`, `WebApplicationFactory` avec un
  `wwwroot` de fixture, sans dépendance réseau).
- `AllowedHosts`/en-têtes de sécurité/compression (LOT 1 du brief SEO d'origine), contenu et pages
  de destination (LOT 3), poids du bundle (LOT 2) et accessibilité restante (LOT 4) sont hors
  périmètre de cet ADR — traités dans des sessions et commits séparés.
