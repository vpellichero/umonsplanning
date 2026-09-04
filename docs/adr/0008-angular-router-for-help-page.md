# 0008 — Ajout du routeur Angular pour une page « Aide » séparée

- Statut : accepté
- Date : 2026-09-03

## Contexte

Le frontend n'avait qu'un seul écran (composant racine `App`), sans `@angular/router` — cohérent
avec la description initiale du site comme SPA mono-page (`docs/ai/angular.md` §4, « aucun,
une seule route »). Le mainteneur a ensuite demandé une page « Aide » listant, pour chaque
application de calendrier courante, la marche à suivre pour s'abonner par URL.

## Options considérées

- **Nouvelle route `/aide`** — nécessite `@angular/router` (déjà présent dans `package.json` au
  scaffolding, mais jamais câblé).
- Section ancrée sur la page unique (scroll vers `#aide`).
- Modale supplémentaire, cohérente avec les deux `<dialog>` déjà en place.

Le mainteneur a choisi la route dédiée.

## Décision

`@angular/router` est activé : `app.routes.ts` déclare deux routes statiques, `''` (page
d'accueil) et `'aide'` (page d'aide), chacune avec son propre `title` de route
(`TitleStrategy`, cohérent avec `docs/ai/angular.md` §4). Le composant racine `App` devient une
coquille (en-tête avec navigation, `<router-outlet>`, pied de page) ; le contenu de l'ancienne
page unique est déplacé dans `features/home/HomePage`, le nouveau contenu dans
`features/help/HelpPage`.

`app.routes.server.ts` conserve sa seule entrée `path: '**', renderMode: Prerender` : Angular
énumère automatiquement les routes statiques du routeur et les prérend toutes — confirmé par le
build (« Prerendered 2 static routes. », un fichier `browser/aide/index.html` généré en plus de
`browser/index.html`). Aucun changement à `docs/adr/0004-mutualized-hosting-topology.md` : la
sortie reste 100 % statique, servable telle quelle par IIS.

## Conséquences

- `docs/ai/angular.md` §4 (préchargement) mis à jour : « deux routes statiques » au lieu de
  « une seule route ».
- Chaque route a son propre titre de page et son contenu prérendu, favorable au référencement et
  au partage de lien direct vers `/aide`.
- Aucune nouvelle dépendance npm : `@angular/router` était déjà listé (scaffolding Angular CLI),
  simplement inutilisé jusqu'ici.
