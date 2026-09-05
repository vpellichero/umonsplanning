# 0017 — Pages de destination, données structurées étendues, pivot `/aide`

- Statut : accepté
- Date : 2026-09-05

## Contexte

Suite et fin du LOT 3 du brief SEO : les 6 pages de destination (3.2), les données structurées
étendues (3.3, qui en dépendent), et la partie de 3.4 laissée de côté au tour précédent faute de
pages à lier dans le footer (`docs/adr/0016`).

## Décision

**Un seul composant, six contenus.** Les 6 pages (`/horaire-umons-google-calendar`,
`-outlook`, `-apple-calendar`, `-thunderbird`, `-proton-calendar`, `/hyperplanning-umons`)
partagent `src/app/features/guide/guide-page.ts` + `.html`, paramétré par un `GuideContent`
(`src/app/features/guide/guide-content.ts`) — un fichier par page sous
`src/app/features/guide/content/`. `GuidePage` lit `ActivatedRoute.snapshot.data['guide']`
directement (il est déjà la feuille de route, contrairement à `SeoMetaService` qui doit remonter
l'arbre). Chaque route est produite par une fonction `guideRoute(content)` dans `app.routes.ts`,
qui construit à la fois la config Angular (`path`, `title`, `data.description`) et les schémas
JSON-LD (`HowTo`, `BreadcrumbList`) **à partir du même contenu que celui affiché** — aucun risque
que le balisage structuré diverge du texte visible.

**Correction de trajectoire pendant la conception, avant d'écrire du code inutile** : la première
approche envisagée injectait les scripts JSON-LD via `afterNextRender` dans chaque page. Ça
n'aurait pas fonctionné : `afterNextRender`/`afterRender` ne s'exécutent qu'en environnement
navigateur, jamais pendant le prérendu SSG (c'est exactement pourquoi `CalendarLinkForm` et
`LegalDisclaimerDialog` s'en servent déjà pour du code qui a besoin du navigateur, `docs/adr/0016`).
Les données structurées doivent au contraire apparaître dans le HTML statique servi. `SeoMetaService`
(`docs/adr/0013`) est le seul mécanisme du projet déjà prouvé pour ça — son abonnement à
`NavigationEnd` s'exécute bien pendant le prérendu, ce qui explique déjà pourquoi le `<title>`/
canonical par route fonctionnent. `SeoMetaService` gagne donc un champ `jsonLd?: readonly object[]`
lu depuis `data`, et une méthode qui retire les `<script data-route-json-ld>` de la route
précédente avant d'insérer ceux de la nouvelle — même mécanique que le retrait/ajout du
`<meta name="robots">` déjà en place. `src/app/core/structured-data-builders.ts` fournit les
fonctions pures (`buildBreadcrumbJsonLd`, `buildHowToJsonLd`, `buildFaqJsonLd`) — testables sans
DOM, séparées de leur injection.

**FAQ de l'accueil : une seule source pour le HTML et le JSON-LD.** Extraite de `home-page.html`
vers `src/app/features/home/home-faq.ts` (tableau `{question, answer}[]`), consommée par un `@for`
dans le template (rendu identique) et par `buildFaqJsonLd(...)` dans la route accueil.

**`WebSite`/`Person` posés dynamiquement (route accueil), pas en script statique dans
`index.html`** — écart volontaire par rapport au plan initial : puisque `SeoMetaService` gère déjà
tous les schémas par route, les y ajouter aussi (plutôt que dans un second script statique séparé)
évite de mélanger deux mécanismes d'injection pour le même type de contenu. Le script
`WebApplication` statique existant gagne simplement `sameAs` (GitHub) et `author` (`Person`,
Vincent Pellichero — cohérent avec `CLAUDE.md` §1, projet personnel non commercial).
**`dateModified` non ajouté** : sans mécanisme d'injection de date de build, une valeur statique se
périmerait silencieusement à la prochaine modification de contenu — pas de valeur à en ajouter une
fausse.

**`/aide` devient la vraie page pivot.** `help-page.ts` corrige un manque signalé par le brief
(Apple Calendar promis par l'accueil/la meta description, absent de la liste) et ajoute un lien
interne « Guide détaillé » vers la page dédiée de chaque application, en plus du lien externe
« Guide officiel » déjà présent. L'URL Apple utilisée
([Use iCloud calendar subscriptions](https://support.apple.com/en-us/102301)) a été vérifiée par
recherche avant d'être utilisée (CLAUDE.md §9 : ne jamais fabriquer un lien).

**Footer.** `app.html` gagne les 6 liens vers les pages de destination (`app.ts`,
`footerGuideLinks`) — la seule chose qu'il manquait, anticipée par `docs/adr/0016`.

**Longueur du contenu : écart assumé, signalé plutôt que masqué.** Le brief demandait 400 à 700
mots par page. Après plusieurs passes d'enrichissement (explication technique du mécanisme
d'abonnement, dépannage « si l'agenda n'apparaît pas », note de confidentialité), les pages
atteignent 317 à 378 mots — en dessous de la fourchette basse. Choix délibéré de ne pas pousser
plus loin par du remplissage artificiel : le contenu reste dense et utile (étapes numérotées,
pièges réels, mécanisme technique expliqué), ce qui compte plus pour la qualité perçue par Google
qu'un compte de mots atteint par du texte creux — à revoir si la position réelle sur les mots-clés
cibles suggère qu'il en faut davantage.

**Pas de captures d'écran/schémas** — confirmé avec le mainteneur en amont : texte seul, étapes
numérotées.

## Conséquences

- Aucune nouvelle dépendance npm.
- Nouveaux tests : `structured-data-builders.spec.ts` (les 3 builders), `seo-meta.service.spec.ts`
  étendu (schémas injectés/retirés par route), `guide-page.spec.ts` (rendu à partir d'un contenu
  d'exemple), `help-page.spec.ts` étendu (lien interne vers chaque guide).
- Vérifié contre une instance publiée réelle : les 9 routes (accueil, aide, 404, 6 guides) répondent
  correctement, `sitemap.xml` liste automatiquement les 8 routes indexables (généré depuis
  `prerendered-routes.json`, LOT 0 — aucun changement de script nécessaire), chaque page de
  destination a son propre `<title>`/canonical/JSON-LD (`HowTo`+`BreadcrumbList`) visible sans
  exécuter de JS, `<h1>` toujours le LCP, CSS toujours non bloquant.
- LOT 4 (accessibilité, propreté) reste hors périmètre de cet ADR — c'est tout ce qu'il reste du
  brief SEO d'origine.
