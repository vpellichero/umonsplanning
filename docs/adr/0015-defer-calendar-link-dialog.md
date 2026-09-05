# 0015 — `@defer` sur le générateur de lien, image OG en WebP

- Statut : accepté
- Date : 2026-09-04

## Contexte

Mesuré via `ng build --stats-json` (`dist/UMonsPlanning.Frontend/stats.json`, métadonnées esbuild
groupées par package/fichier source — pas une estimation) : le chunk initial pesait 406 ko brut
(~110 ko Brotli). Composition réelle des dix plus gros contributeurs :

| Contributeur | Octets |
|---|---|
| `@angular/core` | 149 260 |
| `ical.js` | 78 209 |
| `@angular/router` | 72 888 |
| `@angular/common` | 38 317 |
| `rxjs` | 15 997 |
| `@angular/platform-browser` | 14 015 |
| `calendar-link-dialog.ts` | 13 293 |
| `schedule-preview-dialog.ts` | 4 744 |
| `legal-disclaimer-dialog.ts` | 2 349 |

`ical.js` (78 ko, presque un cinquième du bundle à lui seul) n'est importé que par
`ics-parser.ts`, lui-même utilisé uniquement par `CalendarLinkDialog.testCalendar()` (bouton
« Tester votre calendrier », dans la modale du générateur). Le zoneless (confirmé toujours actif,
aucune trace de `zone.js`) n'était pas en cause : le poids venait de code applicatif et de
dépendances jamais découpés, exactement le diagnostic à établir avant toute découpe (évite de
différer ce qui ne pèse presque rien, comme `legal-disclaimer-dialog.ts` à 2,3 ko).

## Décision

**Un seul `@defer` couvre les deux `<dialog>` qui pèsent réellement.** `SchedulePreviewDialog` est
déjà imbriquée dans le template de `CalendarLinkDialog` : différer ce dernier avec
`@defer (on interaction(genLinkButton))` (`src/app/features/home/home-page.html`) retire les deux
d'un coup, plus `ics-parser.ts`/`ical.js` — environ 96 ko (78 209 + 13 293 + 4 744) du chunk
initial. Mesuré après coup : chunk initial 331,88 ko brut / 91,89 ko Brotli (contre 406,09 / 113,56
avant), nouveau chunk paresseux `calendar-link-dialog` de 98,20 ko brut / 26,19 ko Brotli, chargé
uniquement à l'interaction. `legal-disclaimer-dialog.ts` n'est pas différée : elle doit s'afficher
immédiatement au premier chargement sans déclencheur d'interaction pertinent, et son poids est
marginal face à `ical.js`.

**`CalendarLinkDialog` s'ouvre lui-même à la création** (`afterNextRender(() => { ...; this.open(); })`,
même mécanisme déjà utilisé par `LegalDisclaimerDialog`) plutôt que d'être ouverte depuis
`HomePage` via `viewChild`+méthode : le composant n'existe désormais que parce que le bouton a été
actionné (c'est le déclencheur du `@defer`), donc s'auto-ouvrir à l'initialisation est le
comportement attendu — `HomePage` n'a plus besoin de connaître `CalendarLinkDialog` autrement que
pour l'afficher dans son template.

**`/api/formations` sort du chemin critique sans code de fetch à changer.** `CatalogService`
(`src/app/core/catalog.service.ts`, ses deux `httpResource`) n'est injecté nulle part ailleurs que
dans `CalendarLinkDialog` (`protected readonly catalog = inject(CatalogService)`) — confirmé par
recherche exhaustive dans `src/app`. En différant ce composant, l'injection (donc le déclenchement
des `httpResource`) ne se produit plus qu'à l'ouverture réelle du générateur.

**Pas de `@placeholder`/`@loading`** : la `<dialog>` fermée n'avait déjà aucune empreinte visuelle
avant ouverture (CLS resté à 0 avec ou sans ce lot) ; un état de chargement ajouterait de la
complexité pour un gain perceptible marginal sur un chunk de cette taille servi compressé
(§ précédent, `docs/adr/0014`).

**Image Open Graph en WebP en plus du PNG existant.** `scripts/generate-icons.mjs` gagne
`generateOgImageWebp()` (même canevas que `generateOgImage()`, factorisé dans `buildOgCanvas()`,
seul l'encodage final diffère) — `public/og-image.webp` : 7,5 ko contre 53 ko pour le PNG.
`src/index.html` garde `og:image` (PNG) en premier pour la compatibilité crawler déjà actée dans ce
script, et ajoute un second `og:image` (WebP, avec son propre `og:image:type`). **Sans effet sur
Lighthouse/CWV** : cette image n'est jamais chargée par le navigateur d'un visiteur, seulement par
les robots d'aperçu (Facebook/Twitter/etc.), hors du chemin de rendu de la page — fait par
cohérence avec le brief SEO, pas parce qu'il améliore une métrique mesurée.

## Conséquences

- Aucune nouvelle dépendance (le split est un pur mécanisme du compilateur Angular sur un import
  déjà existant).
- Nouveaux tests dans `home-page.spec.ts` : le générateur n'est pas dans le DOM avant interaction,
  et y apparaît une fois le bloc `@defer` rendu (API officielle `DeferBlockBehavior.Manual` +
  `fixture.getDeferBlocks()` + `DeferBlockState.Complete`, déjà le comportement par défaut de
  `TestBed` dans cette version d'Angular — fixé explicitement pour ne pas dépendre d'un défaut
  susceptible de changer).
- Vérifié : `<h1>` toujours prérendu (LCP inchangé), CSS toujours non bloquant, aucune trace de
  `<app-calendar-link-dialog>` dans le HTML prérendu (CLS toujours à 0 — cette section n'existe
  simplement plus dans la sortie tant qu'elle n'est pas déclenchée).
- Non vérifié en conditions réelles de navigateur (onglet Réseau) faute d'outil d'automatisation de
  navigateur dans cet environnement : la garantie repose sur l'analyse du code (seul point
  d'injection de `CatalogService`) et sur le test `@defer` ci-dessus, pas sur une capture réseau
  visuelle.
- LOT 3 (contenu, pages de destination) et LOT 4 (accessibilité, propreté) restent hors périmètre
  de cet ADR.
