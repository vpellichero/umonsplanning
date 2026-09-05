# 0014 — Compression, en-têtes de cache et HSTS à 1 an

- Statut : accepté
- Date : 2026-09-04

## Contexte

Mesuré (audit SEO) : aucune compression (`content-encoding` absent, ratio transféré/décodé de 1,00
partout — environ 470 ko transférés au premier chargement au lieu de ~130 ko attendus), aucun
`Cache-Control` nulle part (ni sur les fichiers hachés du build Angular, ni sur `/api/formations`),
HSTS à sa valeur par défaut non configurée (30 jours).

Comme pour `docs/adr/0013`, le brief SEO d'origine décrit ce lot en directives nginx
(`gzip`/`brotli_static`/`add_header`), qui ne s'appliquent pas ici : ce site est une unique
application ASP.NET Core sur IIS/ANCM, sans nginx ni `web.config` commité (`docs/adr/0004`,
`docs/adr/0009`). Tout ce lot vit donc dans `Program.cs`/les endpoints.

**CSP explicitement hors périmètre de ce lot**, décision du mainteneur confirmant `docs/adr/0009` :
le build inline un `<style>` critique (beasties) et des `<script>` d'hydratation ; une CSP correcte
(sans `unsafe-inline`) demande un mécanisme de hachage SHA-256 par build qui n'existe pas encore et
mérite son propre travail. Une CSP avec `unsafe-inline` (ce que suggère le brief) n'apporterait
presque aucune protection réelle contre l'injection de script/style — pas ajoutée pour cette
raison. L'écart reste documenté dans `docs/adr/0009`, inchangé.

## Décision

**Compression** : `Microsoft.AspNetCore.ResponseCompression` (déjà disponible via le shared
framework `Microsoft.AspNetCore.App`, aucune nouvelle dépendance — même situation que
`Microsoft.AspNetCore.RateLimiting` déjà en place). Brotli et Gzip activés, `EnableForHttps = true`
— sûr ici car l'application ne traite ni authentification, ni session, ni secret jamais reflété
dans une réponse (`CLAUDE.md` §12), condition sous laquelle le risque BREACH qui justifie
habituellement de laisser la compression HTTPS désactivée ne s'applique pas. `MimeTypes` étend la
liste par défaut du framework par `Concat(...)` plutôt que de la remplacer, pour ne pas avoir à
revérifier/reproduire la liste par défaut — n'ajoute que ce qui manquait
(`application/manifest+json`, pour `site.webmanifest`). `UseResponseCompression()` enregistré tôt
dans le pipeline (avant `UseRateLimiter`/`UseOutputCache`/`UseStaticFiles`) pour envelopper aussi
bien les fichiers statiques que les réponses JSON de l'API.

**Content-Type de `site.webmanifest`** : nouveau `StaticAssetContentTypes`
(`src/UMonsPlanning.Backend/StaticAssets/`) mappe explicitement `.webmanifest` vers
`application/manifest+json` — sans quoi ce fichier n'aurait pas été éligible à la compression
ci-dessus (mauvais Content-Type = jamais dans la liste `MimeTypes`), en plus d'être le type MIME
correct pour ce format.

**`Cache-Control` par type de fichier statique** : nouveau `StaticAssetCacheControl`
(même dossier), branché via `StaticFileOptions.OnPrepareResponse` :
- bundle nommé selon le pattern haché d'Angular (`outputHashing: "all"`, ex. `main-XXXXXXXX.js`) →
  `public, max-age=31536000, immutable` (jamais réutilisé après un nouveau build) ;
- `.html` → `no-cache` (le nom de fichier ne change jamais d'un déploiement à l'autre, contrairement
  aux bundles hachés — donc jamais de cache long sans revalidation) ;
- images/icônes/polices (`.webp`, `.png`, `.ico`, `.woff`, `.woff2`) → `public, max-age=2592000` ;
- tout le reste (`robots.txt`, `sitemap.xml`, `site.webmanifest`) → `public, max-age=3600`.

Le fallback serveur de `docs/adr/0013` (`MapFallback`, qui sert `/`, `/aide`, `/404` directement via
`SendFileAsync` plutôt que via `UseStaticFiles`) pose le même `Cache-Control: no-cache` par lui-même
— ces pages ne passent jamais par `StaticAssetCacheControl`, qui ne s'applique qu'aux requêtes
traitées par `UseStaticFiles`.

**`Cache-Control` sur `/api/formations` et `/api/formations/{formation}/sections`** :
`public, max-age=3600, stale-while-revalidate=86400` — cohérent avec le rythme réel de
rafraîchissement (`FormationCatalogCache`, au plus une fois par mois). **Pas d'`ETag`** : un `ETag`
correct demanderait de gérer soi-même les requêtes conditionnelles (`If-None-Match` → 304), un
mécanisme à part entière pour un gain marginal ici — le `Cache-Control` seul évite déjà l'aller-
retour réseau pendant l'heure de fraîcheur, et l'`OutputCache` déjà en place évite déjà le travail
serveur au-delà. Non implémenté à moitié pour cocher une case du brief — écart assumé.

**HSTS à 1 an** : `AddHsts(options => options.MaxAge = TimeSpan.FromDays(365))`. Le
`UseHsts()` existant (déjà conditionné à `!IsDevelopment()`) n'a pas changé. Non vérifiable en
local sans TLS : le middleware HSTS n'ajoute l'en-tête que sur une requête déjà en HTTPS — vérifié
que le reste du comportement (toutes les autres en-têtes, la compression, le cache) n'est pas
affecté, en HTTP comme en HTTPS.

**Permissions-Policy** déjà conforme à ce que demandait le brief, aucun changement. Pas de
`X-Frame-Options` séparé ajouté : `Cross-Origin-Opener-Policy: same-origin` déjà en place couvre un
risque proche, une vraie protection anti-framing complète passerait par `frame-ancestors` en CSP —
hors périmètre ci-dessus.

## Conséquences

- Aucune nouvelle dépendance NuGet.
- Nouveaux tests d'intégration (`WebApplicationFactory`) : `StaticFallbackTests` (bundle haché →
  immutable, page HTML → no-cache, `robots.txt` → 1h) et `CatalogCacheHeadersTests`
  (`/api/formations` → `Cache-Control` attendu, `FormationCatalogCache` remplacée par une instance
  construite avec un `IPronoteClient` mocké — aucun appel réseau réel).
- Vérifié contre une instance publiée réelle (`dotnet publish` + `wwwroot` copié + `curl`) :
  `content-encoding: br` sur le bundle JS et sur `/api/formations`, `Cache-Control` correct sur
  chaque catégorie de fichier, `site.webmanifest` servi en `application/manifest+json`, et aucune
  régression sur les critères de recette du LOT 0 (404, canonical/`<title>` de `/aide`).
- LOT 2 (poids du bundle, découpage, sortie de `/api/formations` du chemin critique), LOT 3
  (contenu, pages de destination) et LOT 4 (accessibilité, propreté) restent hors périmètre de cet
  ADR.
