# Module — Interface : mobile-first, Tailwind, accessibilité

S'applique à toute production de HTML, quelle que soit la technologie : vues Razor, templates Liquid, composants Angular.

## 1. Mobile-first et responsive

- **Écrire d'abord les styles du plus petit écran**, puis enrichir avec `sm: md: lg: xl: 2xl:`. Ne jamais partir du desktop pour rétrograder ensuite.
- Points de contrôle obligatoires : **320, 375, 768, 1024, 1440 px** et **zoom navigateur 200 %** (exigence WCAG 1.4.4).
- Aucun défilement horizontal à 320 px. Aucun contenu tronqué ou inaccessible en zoom 200 %.
- Cibles tactiles ≥ **44 × 44 px** CSS, avec un espacement suffisant entre deux cibles adjacentes.
- Respecter `prefers-reduced-motion` (animations désactivées ou réduites) et `prefers-color-scheme` si un mode sombre est prévu.
- Utiliser les unités logiques modernes (`dvh` plutôt que `vh` sur mobile, `clamp()` pour la typographie fluide) plutôt que des valeurs fixes multipliées par breakpoint.
- Tenir compte des zones sûres (`env(safe-area-inset-*)`) sur les en-têtes et barres fixes.

## 2. Tailwind CSS

Tailwind est la solution de style par défaut, sauf mention contraire dans `CLAUDE.md` §12.

- **Les tokens de design sont centralisés** (couleurs, espacements, typographie, rayons, ombres, breakpoints) dans `src/UMonsPlanning.Frontend/src/styles/theme.css` (via `@theme`). Aucune couleur ni taille en dur dans un template.
- Les valeurs arbitraires (`w-[437px]`, `text-[#3a3a3a]`) sont interdites sauf justification écrite ; si une valeur revient deux fois, elle devient un token.
- La répétition d'un groupe d'utilitaires se factorise en **composant** (composant Angular, partial Razor, snippet Liquid), pas en empilement d'`@apply`.
- `@apply` reste réservé aux cas où le HTML n'est pas maîtrisé (contenu éditeur, composant tiers).
- Classes ordonnées par le plugin Prettier officiel ; ordre manuel interdit.
- Contenu généré par un éditeur (WYSIWYG, Markdown) : styliser via `@tailwindcss/typography` (`prose`), pas par une cascade de sélecteurs maison.
- Purge/scan correctement configuré : vérifier qu'aucune classe construite dynamiquement (`` `text-${color}-500` ``) n'est supprimée au build — utiliser des mappings explicites.

## 3. Accessibilité — WCAG 2.2 niveau AA, exigence stricte

### Structure
- HTML sémantique d'abord : `<header> <nav> <main> <article> <section> <aside> <footer> <button> <a>`. ARIA sert à combler un manque, jamais à remplacer la sémantique.
- **Un `<div>` avec un gestionnaire de clic est un bug.** Un élément interactif est un `<button>` (action) ou un `<a href>` (navigation).
- Un seul `<h1>` par page ; les niveaux de titre ne sautent jamais un cran ; les titres décrivent le contenu, ils ne servent pas à obtenir une taille de police.
- Lien d'évitement vers le contenu principal en premier élément focusable.
- Attribut `lang` sur `<html>`, mis à jour par locale, et sur tout passage dans une autre langue.
- Landmark `<main>` unique ; `<nav>` multiples distingués par `aria-label`.

### Clavier et focus
- Tout élément interactif est atteignable et actionnable au clavier, dans un ordre de tabulation logique.
- **Indicateur de focus visible** et contrasté (≥ 3:1) sur tous les éléments. `outline: none` sans remplacement est interdit.
- Aucun piège au clavier. Les modales : focus déplacé à l'ouverture, piégé pendant, restitué à l'élément déclencheur à la fermeture, fermeture par `Échap`.
- `tabindex` positif interdit ; `tabindex="-1"` uniquement pour le focus programmatique.
- WCAG 2.2 : aucun élément focusé ne doit être masqué par un en-tête collant (2.4.11) ; pas de glisser-déposer sans alternative simple (2.5.7) ; aucune saisie redemandée si déjà fournie dans le même parcours (3.3.7).

### Contenu
- Contraste : **4.5:1** pour le texte courant, **3:1** pour le grand texte (≥ 24 px, ou ≥ 19 px gras), les composants d'interface et les objets graphiques porteurs de sens.
- L'information n'est jamais portée par la seule couleur (erreurs, statuts, graphiques) : ajouter icône, texte ou motif.
- Images : `alt` pertinent et descriptif, ou `alt=""` si purement décoratif. Une icône porteuse de sens a un nom accessible ; une icône décorative est `aria-hidden="true"`.
- Vidéos : sous-titres ; audio : transcription. Aucune lecture automatique avec son.

### Formulaires
- Chaque champ a un `<label>` associé programmatiquement (`for`/`id`). Un placeholder n'est pas un label.
- Erreurs : identifiées en texte, associées au champ (`aria-describedby`, `aria-invalid`), annoncées via une région live, et décrivant la correction attendue.
- Groupes de champs dans un `<fieldset>` + `<legend>`.
- `autocomplete` renseigné sur les champs d'identité, d'adresse et de contact.
- Pas de délai d'expiration sans avertissement ni possibilité de prolonger.

### Dynamique
- Changements de contenu asynchrones annoncés (`aria-live="polite"`, `role="status"`, `role="alert"` pour l'urgent).
- Changement de route : focus déplacé en tête de contenu et titre de page mis à jour.
- États des composants exposés (`aria-expanded`, `aria-selected`, `aria-current`, `aria-pressed`, `aria-disabled`).

### Vérification
- Passe **clavier uniquement** sur le parcours modifié.
- Audit automatisé (axe DevTools ou Lighthouse) sans violation.
- Test lecteur d'écran de fumée sur les composants nouveaux ou complexes : NVDA + Firefox (non exécuté dans cette session — à faire avant mise en production).
- L'outillage automatique ne détecte qu'environ 40 % des problèmes : **la passe manuelle est obligatoire**, elle ne se remplace pas par un score Lighthouse.

## Checklist du module

- [ ] Écrit en mobile-first ; vérifié à 320 / 768 / 1024 / 1440 px et zoom 200 %.
- [ ] Aucun défilement horizontal, aucune cible tactile < 44 px.
- [ ] Tokens Tailwind utilisés, aucune valeur arbitraire injustifiée.
- [ ] HTML sémantique ; aucun `div` cliquable.
- [ ] Navigation clavier complète, focus visible, focus géré dans les modales et au changement de route.
- [ ] Contrastes 4.5:1 / 3:1 vérifiés, information jamais portée par la seule couleur.
- [ ] Labels, erreurs annoncées, `autocomplete` renseigné.
- [ ] `alt` sur toutes les images, `aria-hidden` sur les icônes décoratives.
- [ ] Audit axe sans violation + passe clavier manuelle effectuée.
