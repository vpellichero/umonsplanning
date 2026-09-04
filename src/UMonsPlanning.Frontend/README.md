# UMonsPlanning.Frontend

SPA Angular 21 : explique le service, et permet de générer un lien de calendrier (`.ics`) à
souscrire dans Google Calendar / Outlook / Apple Calendar / Thunderbird / Proton Calendar, à
partir des horaires PRONOTE de l'UMONS. Deux routes : `/` (accueil) et `/aide` (marche à suivre
par application de calendrier). Voir `CLAUDE.md` (racine du dépôt) et `docs/adr/` pour les
décisions d'architecture derrière cette application.

## Rendu : prérendu statique (SSG), pas de serveur Node en production

`ng build` prérend les deux routes au moment du build (`outputMode: "static"` dans
`angular.json`, route `**` en `RenderMode.Prerender`) et ne produit qu'un dossier `browser/` —
aucun `server/` n'est émis, aucun processus Node ne tourne en production (hébergement mutualisé
sans Node, voir `docs/adr/0004-mutualized-hosting-topology.md`). Les listes de dropdown et
l'aperçu du calendrier sont peuplés côté client après hydratation, au clic de l'utilisateur : rien
de dynamique n'est requis pendant le prérendu.

## Développement local

```bash
npm install
npm start          # ng serve — http://localhost:4200, proxy /api -> http://localhost:5199
```

Le proxy de développement (`proxy.conf.json`) redirige `/api/*` vers `UMonsPlanning.Backend`
lancé en local (`dotnet run --project ../UMonsPlanning.Backend`, port 5199 par défaut). En
production, le backend est publié sous `/api` du même site (même origine) : voir
`docs/adr/0004-mutualized-hosting-topology.md`.

## Build

```bash
npm run build       # sortie statique dans dist/UMonsPlanning.Frontend/browser
```

Budgets (`docs/ai/performance.md`, profil B) appliqués dans `angular.json` : le build échoue si le
bundle initial dépasse 800 Ko brut (avertissement dès 600 Ko).

## Tests

```bash
npm test            # Vitest — lanceur retenu par défaut par Angular CLI 21 (`--test-runner=vitest`)
```

Aucun test end-to-end (Playwright) dans cette première version — voir CLAUDE.md §12, « Hors
périmètre ».

## Identité visuelle et favicons

`public/logo.webp`, `public/logo-horizontal.webp` et `public/icon.webp` sont les fichiers *source*
du logo, en pleine résolution (voir la note du `README.md` racine sur leur origine) — ils ne sont
pas affichés tels quels dans l'application, seulement utilisés comme matière première par
`scripts/generate-icons.mjs`, qui **génère** tous les autres fichiers de `public/` :

- `favicon.ico`, `apple-touch-icon.png`, `icon-192.png`, `icon-512.png`, `icon-maskable-512.png`,
  `site.webmanifest` — depuis `icon.webp`.
- `og-image.png` (1200×630, pour l'aperçu Open Graph) — depuis `logo-horizontal.webp` en pleine
  résolution : une image de cette taille a besoin de plus de pixels qu'un petit logo d'en-tête.
- `logo-horizontal-header.webp` (440×64) et `logo-hero.webp` (400×197) — dérivés redimensionnés à
  2× la taille d'affichage réelle (en-tête `app.html` : 220×32 ; hero `home-page.html` : 200×100),
  ce sont ceux réellement référencés par `ngSrc` dans les templates. Aucune des deux tailles
  d'affichage ne varie par breakpoint : un seul fichier suffit, pas de `srcset`.

Ne pas modifier ces fichiers générés à la main, relancer le script à la place :

```bash
npm install --no-save sharp png-to-ico   # outils de génération, volontairement absents de package.json
node scripts/generate-icons.mjs
```

Ces deux paquets ne sont pas des dépendances du projet (pas dans `package.json`) : ils ne servent
qu'à cette régénération ponctuelle, à réinstaller à la demande plutôt qu'à auditer/maintenir en
permanence pour un usage aussi occasionnel.

## Dépendances notables

- **Tailwind CSS** — solution de style par défaut, tokens centralisés dans `src/styles/theme.css`.
- **Aucun kit de composants** (PrimeNG écarté, voir `docs/adr/0007-primeng-ui-kit.md`) : les
  dropdowns sont des `<select>` natifs, les modales des `<dialog>` natifs, les accordéons de
  l'aperçu sont des boutons de divulgation (`aria-expanded`/`aria-controls`) pilotés par signal —
  pas de `<details>`/`<summary>` natifs (comportement de bascule signalé comme peu fiable selon
  les navigateurs).
- **ical.js** (MPL-2.0, exception documentée dans `docs/adr/0005-icaljs-mpl-license-exception.md`)
  pour décoder le `.ics` récupéré par le bouton « Tester votre calendrier ».
