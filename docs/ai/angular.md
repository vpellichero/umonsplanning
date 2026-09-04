# Module — Angular (v21 minimum)

Complète le socle `CLAUDE.md`. Version utilisée : `21` (épinglée dans `src/UMonsPlanning.Frontend/package.json`).

Toute réponse doit correspondre aux API de cette version. En cas de doute sur une API, lire `node_modules/@angular/**` du projet ou la documentation de la version épinglée — ne jamais reprendre un pattern des versions 8–16 par habitude.

## 1. Architecture des composants

- **Composants standalone uniquement.** Aucun `NgModule`, y compris pour les tests et le routage.
- **`ChangeDetectionStrategy.OnPush` partout**, application **zoneless** (`provideZonelessChangeDetection()`).
- API fonctionnelles : `input()`, `input.required()`, `output()`, `model()`, `viewChild()`, `contentChild()`. Aucun décorateur `@Input`/`@Output`/`@ViewChild`.
- `inject()` plutôt que l'injection par constructeur ; services `providedIn: 'root'` par défaut.
- Nouveau flux de contrôle : `@if`, `@for` (avec `track` obligatoire et pertinent), `@switch`, `@let`, `@defer` (+ `@placeholder`, `@loading`, `@error`). Jamais `*ngIf` / `*ngFor` / `ngSwitch`.
- Un composant = une responsabilité d'affichage. La logique métier vit dans un service, pas dans le composant.
- Composants « dumb » (entrées/sorties, aucune dépendance) séparés des composants « container » (accès aux services).

## 2. État et réactivité

- **Signals** pour l'état local et dérivé : `signal()`, `computed()`, `linkedSignal()`, `resource()` / `httpResource()` pour les données asynchrones.
- `effect()` est un dernier recours (synchronisation avec un monde extérieur non réactif). Jamais d'`effect()` pour dériver un état : c'est `computed()`.
- RxJS uniquement là où un flux est réellement nécessaire (websockets, événements, debounce, annulation complexe). Conversion via `toSignal()` / `toObservable()`.
- Toute souscription manuelle est désabonnée (`takeUntilDestroyed()`), sans exception.
- État partagé : services à signals. Pas de store global (NgRx/SignalStore) — une seule page, état limité aux dropdowns et à l'URL générée, ne justifie pas l'infrastructure d'un store.
- Immutabilité : ne jamais muter un objet contenu dans un signal ; remplacer via `update()`.

## 3. Typage et qualité

- `strict: true` et `strictTemplates: true` dans `tsconfig`. Aucun `any` ; `unknown` + narrowing si nécessaire.
- Formulaires réactifs **typés** (`FormGroup<...>`, `NonNullableFormBuilder`). Pas de template-driven forms hors cas trivial.
- Interfaces/`type` pour tous les DTO d'API ; validation ou mapping à la frontière plutôt que confiance aveugle dans la réponse HTTP.
- Pas d'accès DOM direct : API Angular, `Renderer2`, ou signals de host binding. Jamais d'`innerHTML` avec du contenu non assaini (`DomSanitizer` justifié par un commentaire).

## 4. Routage et chargement

- Routes lazy via `loadComponent` / `loadChildren`, découpées par fonctionnalité.
- Guards et resolvers fonctionnels (`CanActivateFn`, `ResolveFn`).
- `@defer` avec triggers (`on viewport`, `on interaction`, `on idle`) pour tout bloc lourd sous la ligne de flottaison.
- Préchargement : aucun (deux routes statiques seulement, `/` et `/aide` — voir `docs/adr/0008-angular-router-for-help-page.md`)
- Gestion du focus au changement de route et titre de page unique par route (`title` sur la route + `TitleStrategy`).

## 5. HTTP et erreurs

- `provideHttpClient(withFetch(), withInterceptors([...]))` ; intercepteurs fonctionnels.
- Intercepteurs pour : authentification, corrélation, gestion centralisée des erreurs, indicateur de chargement.
- Jamais d'URL en dur dans un composant : passer par un service et la configuration d'environnement.
- Toute erreur HTTP produit un message utilisateur localisé et compréhensible ; aucune trace technique affichée.

## 6. Rendu serveur

- **Prerender statique (SSG) au build**, pas de serveur Node en production (hébergement mutualisé
  sans Node — voir `docs/adr/0004-mutualized-hosting-topology.md`). `ng build` produit du HTML
  statique pour la page unique ; tout contenu dynamique (dropdowns, lien généré, aperçu du
  calendrier) est peuplé côté client après hydratation, déclenché par une interaction utilisateur.
- `provideClientHydration(withIncrementalHydration())` ; éviter tout code dépendant de `window`/`document` hors garde (`afterNextRender`, `isPlatformBrowser`).
- Vérifier l'absence de « flash » d'hydratation et de divergence serveur/client (aucun warning d'hydratation en console).

## 7. Style et structure

- Tailwind CSS par défaut (voir `frontend-ui.md`) ; styles de composant réservés à ce que Tailwind ne couvre pas.
- `ViewEncapsulation` par défaut ; `::ng-deep` interdit sauf justification écrite.
- Organisation par fonctionnalité : `features/<domaine>/{components,services,models}`, `core/` (singletons), `shared/` (composants réutilisables). Barrels seulement là où ils ne créent pas de cycle.
- Nommage : `kebab-case` pour les fichiers, `PascalCase` pour les classes, sélecteurs préfixés `app`.

## 8. Budgets et performance

Configurer les budgets dans `angular.json` (voir `performance.md` pour les valeurs par type de projet) et échouer le build en cas de dépassement, plutôt que d'avertir.

- Pas de bibliothèque lourde importée en entier (`import { debounce } from 'lodash-es'`, jamais `import _ from 'lodash'`).
- Images via `NgOptimizedImage` (`ngSrc`, `priority` sur le LCP, `width`/`height` obligatoires).
- `trackBy` / `track` correct sur toute liste dynamique, sous peine de re-rendus complets.

## Checklist du module

- [ ] Standalone, OnPush, zoneless, nouveau flux de contrôle.
- [ ] `input()`/`output()`/`model()` et `inject()`, aucun décorateur hérité.
- [ ] Signals pour l'état ; aucun `effect()` utilisé pour dériver une valeur.
- [ ] Aucune souscription non désabonnée.
- [ ] `strict` + `strictTemplates` respectés, aucun `any`.
- [ ] Formulaires typés, validation côté client **et** serveur.
- [ ] Routes lazy, `@defer` sur les blocs lourds.
- [ ] Budgets de bundle respectés (build en erreur si dépassement).
- [ ] Aucun warning d'hydratation si SSR.
