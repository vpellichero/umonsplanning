# 0005 — Exception de licence : ical.js (MPL-2.0) dans le frontend

- Statut : accepté
- Date : 2026-09-02

## Contexte

Le bouton « Tester votre calendrier » récupère le `.ics` généré par le backend et le convertit en
JSON côté Angular pour l'afficher dans une grille horaire. `docs/ai/dependances.md` §3 classe
MPL-2.0 en « accord préalable uniquement », pas dans la liste autorisée par défaut.

## Options considérées

| Option | Licence | Dernière version | Maintenance |
|---|---|---|---|
| **ical.js** (Mozilla/Kewisch) | MPL-2.0 | 2.2.1 (2025-08-08) | active, utilisée par Thunderbird |
| ical2json | MIT | ancienne, parseur naïf | pas de gestion du pliage de lignes ni de l'échappement |
| Parseur maison | — | — | réinvente un format standard déjà bien résolu |

## Décision

**ical.js** est retenu, avec l'accord explicite du mainteneur obtenu après signalement de la
licence. Utilisation en **dépendance npm non modifiée** (bundlée telle quelle par le build
Angular) : la clause copyleft de MPL-2.0 s'applique fichier par fichier aux fichiers *modifiés* de
la bibliothèque, pas au code propriétaire qui la consomme sans la modifier. Aucune obligation de
publication du code de `UMonsPlanning.Frontend` n'en découle.

## Conséquences

- `src/UMonsPlanning.Frontend/package.json` référence `ical.js` en dépendance directe, version
  épinglée.
- `THIRD-PARTY-NOTICES.md` mentionne explicitement ical.js et sa licence MPL-2.0.
- Si `ical.js` devait un jour être forké ou patché localement, cette ADR ne couvre plus l'usage :
  revenir demander un nouvel accord avant de le faire.
