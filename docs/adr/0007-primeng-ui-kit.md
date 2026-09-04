# 0007 — Pas de kit UI : Tailwind seul, composants sur mesure

- Statut : accepté (remplace une décision initiale en faveur de PrimeNG)
- Date : 2026-09-02

## Contexte

Le frontend a besoin de deux menus déroulants, d'une modale de configuration, d'une seconde
modale affichant une grille horaire, et d'un formulaire de dates optionnel. `docs/ai/angular.md`
laisse le choix du kit UI ouvert (aucun / Sakai-ng / PrimeNG / Angular Material / sur mesure).

## Décision initiale (revenue)

PrimeNG 21.0.0 (MIT) avait été retenu. Vérification a posteriori du paquet réellement installé
(`node_modules/primeng/LICENSE.md`) : la bibliothèque de composants elle-même est bien MIT
(« PRIMENG COMMUNITY VERSIONS LICENSE »). Mais son mécanisme de thème officiel pour la v21,
`@primeuix/themes` (requis pour `providePrimeNG({ theme: { preset } })`), est passé sous une
licence **PrimeUI** distincte, vérifiée dans son propre `LICENSE.md` :

- « Community License (Free) » soumise à des critères d'éligibilité (revenu, effectif, financement) ;
- **clé de licence obligatoire**, y compris pour l'usage gratuit, avec **renouvellement annuel** ;
- absence ou expiration de la clé pouvant afficher un avertissement dans l'application.

C'est un logiciel « source-available » à restriction et enregistrement obligatoire — la catégorie
que `docs/ai/dependances.md` §3 classe **interdite** (plus stricte encore que MPL-2.0, qui elle
n'exige ni clé ni renouvellement). L'ancien paquet `@primeng/themes@21.0.0` (MIT, mais annoncé
« deprecated » par PrimeTek au profit de `@primeuix/themes`) aurait évité le problème de licence
mais reste une bibliothèque figée, non maintenue, comme seule option pour rester sous MIT.

## Décision finale

Aucun kit UI. **Tailwind CSS seul**, composants sur mesure :

- Les deux menus déroulants : `<select>` natif stylé Tailwind — accessible et clavier-natif par
  construction (WCAG 2.2, `docs/ai/frontend-ui.md`), sans bibliothèque.
- Les deux modales : élément `<dialog>` natif (`showModal()`), qui gère nativement le piège de
  focus, la restitution du focus au déclencheur et la fermeture par `Échap` — exactement ce
  qu'exige `docs/ai/frontend-ui.md` §3, sans code de gestion de focus à écrire à la main.
- L'aperçu du calendrier : liste empilée jour par jour (`<section>`/`<ul>` Tailwind), sans bibliothèque de tableau ni de grille horaire.

## Conséquences

- Aucune dépendance UI dans `src/UMonsPlanning.Frontend/package.json` au-delà de Tailwind CSS.
- Plus de code de présentation à écrire qu'avec un kit de composants, mais zéro dépendance externe
  et zéro risque de licence à surveiller dans le temps (pas de clé à renouveler, pas de version à
  auditer pour un kit de composants).
- Si un besoin futur (ex. table de données complexe, calendrier avancé) dépasse ce que Tailwind
  seul couvre raisonnablement, réévaluer alors une bibliothèque ciblée — pas un kit complet — avec
  un nouvel audit de licence explicite avant intégration.
