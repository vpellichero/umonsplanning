# 0009 — Le backend sert le frontend ; renforcement de la sécurité pour une mise en ligne publique

- Statut : accepté
- Date : 2026-09-04

## Contexte

Le dépôt passe d'un usage local à une mise en ligne publique réelle (dépôt GitHub public, CI/CD,
deux environnements `test`/`production` sur l'hébergement mutualisé). Deux décisions en découlent,
suffisamment liées pour un seul ADR : comment le frontend statique est servi, et le durcissement
de sécurité que cette mise en ligne publique impose.

`docs/adr/0004-mutualized-hosting-topology.md` décision 3 prévoyait le frontend statique servi
directement par IIS à la racine du site, et le backend publié séparément comme application IIS
sous `/api`. Le mainteneur a demandé de revenir sur ce point : le frontend généré doit être servi
**par le backend**, pas directement par IIS.

## Décision — topologie de service

Une seule application ASP.NET Core sert désormais tout le site : `Program.cs` ajoute
`UseDefaultFiles()`/`UseStaticFiles()` (sert `wwwroot/`) et `MapFallbackToFile(...)` pour les
routes Angular accédées directement (`/aide`). La redirection `"/" -> "/scalar"` est retirée —
`/scalar` reste joignable directement, simplement plus en page d'accueil.

`wwwroot/` n'est ni committé ni généré par `dotnet build` : c'est le pipeline de déploiement (voir
CI/CD, `.github/workflows/ci-cd.yml`) qui publie le backend puis copie
`dist/UMonsPlanning.Frontend/browser/*` dans `publish/wwwroot/` avant l'envoi FTPS. Aucune
dépendance à Node n'est introduite dans le projet backend lui-même ; le flux de développement
local (`ng serve` + proxy) est inchangé.

Un piège découvert en vérifiant le résultat publié : `MapFallbackToFile` intercepte **toute**
combinaison verbe+chemin non déjà routée, y compris une requête `HEAD` sur `/api/health` (ce
qu'envoie un moniteur de disponibilité) — sans exclusion explicite, une telle requête tombait sur
le HTML du frontend avec un 200 trompeur au lieu d'un 404 honnête. Le pattern de route exclut donc
`api/` : `MapFallbackToFile("{*path:regex(^(?!api/).*$):nonfile}", "index.html")`.

Ceci amende formellement la décision 3 de l'ADR 0004 : le sous-chemin `/api` sans sous-domaine
reste vrai, mais comme routage interne d'une seule application ASP.NET Core plutôt que comme deux
applications IIS distinctes. Conséquence notable côté exploitation : `app_offline.htm`, reconnu
nativement par le module ASP.NET Core pour IIS (ANCM) à la racine d'une application, couvre
désormais **tout le site** (API et fichiers statiques) avec un seul fichier — sans lui, la même
exigence de maintenance site-wide aurait demandé une règle IIS URL Rewrite supplémentaire pour
couvrir la partie statique, non vérifiable sans accès au serveur.

## Décision — durcissement de sécurité

`docs/ai/securite-rgpd.md` §4 documentait déjà plusieurs manques comme des reports volontaires,
conditionnés à « si l'API devient plus visible » — condition désormais remplie :

- **Rate limiting** (`Microsoft.AspNetCore.RateLimiting`, natif ASP.NET Core, aucune nouvelle
  dépendance) : un plafond global par IP sur tout `/api/*`, et un plafond plus strict
  (`ScheduleEndpoints.PronoteRateLimitPolicyName`) sur les trois endpoints qui appellent PRONOTE en
  direct (`/api/schedule`, `/api/schedule.ics`, `/api/weeks/by-date/{date}`) — objectif explicite :
  protéger la session PRONOTE partagée d'un usage excessif. Vient en complément du `OutputCache`
  déjà en place (10 min / 30 min / 6 h) : le cache neutralise les requêtes identiques répétées, le
  rate limit neutralise le volume total, y compris avec des paramètres qui varient à chaque appel.
- **CORS retiré entièrement** plutôt que restreint à une liste d'origines : avec la topologie
  ci-dessus, le frontend et l'API sont toujours servis par le même processus (y compris en
  développement, le proxy `ng serve` faisant déjà apparaître les appels comme same-origin au
  navigateur) — plus aucun scénario cross-origin légitime à couvrir.
- `UseHsts()` (hors `Development`) et `UseHttpsRedirection()`.
- En-têtes de réponse : `X-Content-Type-Options: nosniff`, `Referrer-Policy:
  strict-origin-when-cross-origin`, `Permissions-Policy` (caméra/micro/géoloc/paiement refusés,
  aucun n'étant utilisé), `Cross-Origin-Opener-Policy: same-origin`.
- `AllowedHosts` restreint aux domaines réels (`umonsplanning.pellichero.be`,
  `test.umonsplanning.pellichero.be`, `localhost`) plutôt que `*` — `localhost` réactivé à `*` dans
  `appsettings.Development.json` pour ne pas gêner le développement local.

**Écart assumé, signalé plutôt que masqué** : pas de Content-Security-Policy dans cette passe. Le
build Angular en production inline un `<style>` critique et plusieurs `<script>` (contrat
d'événements, état d'hydratation) directement dans `index.html` — une CSP stricte sans
`unsafe-inline` (exigée par `securite-rgpd.md` §4) demanderait des hachages SHA-256 recalculés à
chaque build et injectés dans la réponse du backend, un mécanisme qui n'existe pas encore et
mérite son propre travail plutôt qu'un `unsafe-inline` qui viderait la CSP de son intérêt. À
reprendre si le besoin se confirme.

**Ce qui ne peut pas être fait, et pourquoi** : restreindre techniquement le backend au seul
frontend (« empêcher une application tierce de l'utiliser ») n'est pas possible via CORS — c'est
une politique appliquée par le *navigateur*, pas par le serveur ; elle n'arrête ni un script, ni
`curl`, ni un appelant serveur à serveur. Un jeton intégré au SPA n'apporterait pas plus de
protection : le code JS livré au navigateur est public. Le rate limiting ci-dessus est le contrôle
serveur réel contre un usage abusif ou une tentative de saturation ; au-delà (WAF/CDN), hors
périmètre tant qu'aucun incident réel ne le justifie (même principe déjà appliqué à la résilience
réseau PRONOTE, CLAUDE.md §12).

## Conséquences

- `docs/adr/0004-mutualized-hosting-topology.md` décision 3 amendée par le présent ADR pour la
  partie topologie de service (le reste — pas de `BackgroundService`, prérendu statique — reste
  valable).
- `docs/ai/securite-rgpd.md` §4 à jour : rate limiting, CORS et en-têtes ne sont plus des reports.
- CSP non implémentée dans cette passe — écart assumé et documenté ci-dessus, pas un oubli.
- Un déploiement doit désormais fusionner deux artefacts (publication backend + build frontend)
  dans un seul dossier avant l'envoi FTPS, plutôt que publier deux arborescences indépendantes.
