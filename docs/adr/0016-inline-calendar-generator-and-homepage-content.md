# 0016 — Générateur inline, page d'accueil enrichie, fil d'ariane

- Statut : accepté
- Date : 2026-09-05

## Contexte

Suite du brief SEO (LOT 3, sous-points 3.1 et 3.4 uniquement — le mainteneur a explicitement laissé
3.2 « pages de destination » et 3.3 « données structurées étendues » pour une session séparée, 3.3
dépendant directement des pages de 3.2). Le générateur (formulaire formation/section/lien) vivait
dans une `<dialog>` fermée par défaut : contenu invisible pour Google, et un clic de plus entre
l'arrivée et le lien généré. La page d'accueil ne contenait que 757 caractères de texte visible, ne
mentionnait « Hyperplanning » que dans `<meta name="keywords">`, et n'avait pas de FAQ.

**Tension avec le LOT 2** (`docs/adr/0015`) : le générateur venait justement d'être passé en
`@defer (on interaction(...))` parce qu'il embarque `ical.js` (78 Ko) via `ics-parser.ts`, utilisé
par le bouton « Tester votre calendrier ». Rendre le générateur toujours visible aurait dû annuler
ce gain si `ics-parser.ts` restait un import statique du composant. Décision du mainteneur : le
formulaire devient inline, mais `ical.js` doit rester différé.

## Décision

**Le formulaire devient une section de page, plus une modale.** `CalendarLinkDialog` renommée
`CalendarLinkForm` (fichiers, classe, sélecteur `app-calendar-link-form`) — garder « Dialog » dans
le nom aurait été trompeur pour un composant qui n'est plus une `<dialog>`. Le template perd son
`<dialog>`/en-tête/bouton de fermeture ; il ne restait aucun test pour ce composant malgré ses 261
lignes — `calendar-link-form.spec.ts` ajouté (couverture minimale : le select est bien rendu sans
interaction, aucun lien avant le choix d'une formation).

**`ical.js` reste hors du chunk initial via un import dynamique, pas un `@defer` de template.**
`@defer` ne diffère qu'un composant référencé dans un template ; `ics-parser.ts` est importé pour
sa fonction `parseIcsToEvents`, utilisée dans la méthode `testCalendar()`, pas comme composant — un
`@defer` de template n'aurait donc rien pu y faire. `testCalendar()` fait maintenant
`const { parseIcsToEvents } = await import('./ics-parser')`, chargé seulement au clic sur
« Tester votre calendrier ». Mesuré après coup : chunk initial 351,08 Ko brut / 95,31 Ko Brotli
(contre 331,88/91,89 juste après le LOT 2, et 406/113,56 avant celui-ci) — la hausse d'environ
19 Ko vient du formulaire et de la FAQ désormais toujours rendus, `ical.js` reste dans son propre
chunk paresseux (`ics-parser`, 78,26 Ko), inchangé.

**Contenu de l'accueil.** Nouvelle structure : hero → générateur (inline, remonté au-dessus de
« Comment ça marche ») → « Comment ça marche » (inchangé) → FAQ (nouvelle, 7 questions du brief,
rendues en `<dl>` visible — pas de `<details>` fermé). Le paragraphe d'intro mentionne désormais
« Hyperplanning » et « PRONOTE » dans le texte visible (auparavant seulement dans
`<meta name="keywords">`) et l'année académique, tirée d'une nouvelle constante
`src/app/core/academic-year.ts` (`CURRENT_ACADEMIC_YEAR`) — le point unique à mettre à jour chaque
rentrée que demandait le brief, équivalent frontend de `Pronote:BaseUrl` côté backend
(CLAUDE.md §12). Les réponses de la FAQ restent factuelles : aucune ne promet un intervalle de
rafraîchissement précis (ça dépend de l'application de calendrier, pas d'UMonsPlanning), et
« données collectées » mentionne explicitement l'absence de compte/cookie et le seul compteur
anonyme déjà affiché sur la page. **Pas de JSON-LD `FAQPage`** pour ces questions — 3.3, une
session séparée, pour éviter des données structurées à moitié posées avant que les pages du LOT 3.2
n'existent.

**Fil d'ariane.** Nouveau composant partagé `src/app/core/breadcrumb/` (`items: readonly
{label, link?}[]` en entrée), affiché en haut de `HelpPage` et `NotFoundPage`. Uniquement le rendu
visible (`<nav aria-label="Fil d'ariane">` + `<ol>`) — le balisage `BreadcrumbList` structuré reste
3.3.

**Footer.** Le `<footer>` de `app.html` contenait déjà la mention « non officiel » et le lien
GitHub à l'intérieur de la balise (contrairement au constat du brief d'origine — déjà corrigé dans
une session antérieure, hors de ce travail SEO). Ce qui manquait réellement : une navigation.
Ajout d'un `<nav aria-label="Pied de page">` (Accueil/Aide/GitHub) — les futures pages par
application (LOT 3.2) s'y ajouteront quand elles existeront, pas avant.

## Conséquences

- Aucune nouvelle dépendance npm.
- `help-page.spec.ts` : le sélecteur `li a` du test « liens https ouverts en nouvel onglet » a dû
  être resserré à `#calendar-apps a` — le fil d'ariane introduit ses propres `<li>`/`<a>` (liens
  internes, pas externes) qui auraient sinon fait échouer cette assertion à tort.
- Vérifié contre une instance publiée réelle : `<h1>` toujours le LCP, CSS toujours non bloquant,
  404/canonical du LOT 0 toujours corrects, compression/cache du LOT 1 inchangés.
- LOT 3.2 (6 pages de destination) et 3.3 (données structurées étendues : `HowTo`, `FAQPage`,
  `BreadcrumbList`, `WebSite`/`Organization`) restent hors périmètre de cet ADR. LOT 4
  (accessibilité, propreté) également.
