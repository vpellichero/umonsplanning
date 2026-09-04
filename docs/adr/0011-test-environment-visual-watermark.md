# 0011 — Filigrane visuel sur l'environnement de test

- Statut : accepté
- Date : 2026-09-04

## Contexte

Les environnements test et production sont visuellement identiques une fois le site ouvert dans
le navigateur — seule l'URL (`test.umonsplanning.pellichero.be` vs `umonsplanning.pellichero.be`)
les distingue. Le mainteneur a demandé un marquage visuel (filigrane de triangles d'avertissement
répétés) affiché uniquement en test, pour éviter toute confusion lors des vérifications manuelles.

Deux mécanismes de détection d'environnement ont été envisagés :

- **Runtime** : un endpoint backend (`GET /api/environment`) exposant `IHostEnvironment`, appelé
  une fois par l'app Angular au démarrage pour poser une classe sur le body.
- **Build-time** : deux configurations Angular (`production`/`staging`), chacune avec son propre
  `environment.ts` (`fileReplacements`), le frontend étant déjà buildé séparément par
  `deploy-test`/`deploy-prod` (`docs/adr/0010`).

Le mainteneur a choisi le build-time : pas d'appel réseau supplémentaire, pas de nouvelle variable
GitHub à introduire (`ASPNETCORE_ENVIRONMENT`, déjà injecté dans `web.config` par site selon
`docs/ai/securite-rgpd.md` §3, aurait fait doublon avec un flag dédié), et cohérent avec
`docs/ai/angular.md` §5 qui anticipait déjà « passer par un service et la configuration
d'environnement ».

## Décision

`src/environments/environment.ts` (`isTestEnvironment: false`) et `environment.staging.ts`
(`isTestEnvironment: true`) sont introduits — première utilisation de ce mécanisme dans le projet.
`angular.json` gagne une configuration `staging` limitée à un `fileReplacements` sur ce fichier ;
elle n'existe pas seule, elle est toujours combinée à `production` (`ng build
--configuration=production,staging`) pour conserver les budgets de bundle et `outputHashing`.

`.github/workflows/ci-cd.yml` : seul le job `deploy-test` passe `--configuration=production,staging`
à `npm run build`. `deploy-prod` et le job `build-frontend` (vérification à chaque push) restent
sur le build par défaut (`production` seule), donc sans le filigrane.

**En local aussi** : le mainteneur a demandé que `ng serve` (`npm start`) affiche également le
filigrane, les données PRONOTE servies en développement local n'étant jamais garanties à jour ou
stables (session PRONOTE partagée, cache local potentiellement périmé). La configuration `serve.
development.buildTarget` combine désormais `development,staging` (au lieu de `development` seule)
— même principe de composition que `production,staging` en CI, `staging` restant un overlay jamais
utilisé isolément. `npm run watch` (`ng build --watch --configuration development`), script hérité
du scaffolding Angular CLI et non documenté dans `CLAUDE.md` §6, n'a pas été modifié : il n'est pas
partie du flux de développement local réel de ce projet.

Le composant racine (`App`) expose `isTestEnvironment` depuis `environment`, consommé par un
`@if` dans `app.html` qui rend un `<div class="test-watermark" aria-hidden="true">` — décoratif
pur, donc `aria-hidden` et `pointer-events: none` (`docs/ai/frontend-ui.md` §3). La valeur étant
une constante résolue à la compilation, ce bloc est directement présent dans le HTML prérendu
(SSG) : aucun accès à `window`/`document` nécessaire, aucune garde `afterNextRender`/
`isPlatformBrowser` à ajouter (`docs/ai/angular.md` §6).

Le style (`app.css`, classe `.test-watermark`) est un `<svg>` de triangle d'avertissement encodé en
data URI, répété via `background-repeat` sur un calque `position: fixed; inset: 0` à faible
opacité (0.12), au-dessus du contenu mais sans jamais intercepter les clics. La couleur est
déclarée comme token dans `styles/theme.css` (`--color-test-watermark-500`) par cohérence avec
`docs/ai/frontend-ui.md` §2, bien qu'elle doive être dupliquée en dur dans le data URI (un
`background-image` ne peut pas référencer une variable CSS) — synchronisation signalée par
commentaire dans `app.css`.

## Conséquences

- Aucune nouvelle dépendance, aucun nouvel appel réseau, aucune variable GitHub Actions
  supplémentaire.
- Le job `build-frontend` (CI sur chaque push) ne compile que la configuration `production` : une
  erreur qui n'existerait que dans `environment.staging.ts` ou dans la combinaison
  `production,staging` ne serait détectée qu'au déploiement sur `develop`. Risque jugé négligeable
  (le fichier ne contient qu'une constante booléenne) — à revoir si ce mécanisme se complexifie.
- Toute variable propre à un environnement suivra désormais ce même mécanisme
  (`environment.ts`/`environment.staging.ts`) plutôt qu'un flag ad hoc supplémentaire.
