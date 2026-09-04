# 0002 — Ne pas activer `InvariantGlobalization`

- Statut : accepté
- Date : 2026-09-02

## Contexte

`InvariantGlobalization=true` réduit le poids du déploiement (utile sur un hébergement mutualisé,
voir 0004) en désactivant le chargement des données ICU. Il a été activé par défaut dans
`Directory.Build.props` lors de la mise en place du socle .NET 10.

## Incident

`Slug.From` (`UMonsPlanning.Pronote.Internal`) dérive l'identifiant stable d'une ressource PRONOTE
en retirant les diacritiques via `string.Normalize(NormalizationForm.FormD)` puis en filtrant les
`UnicodeCategory.NonSpacingMark`. Sous `InvariantGlobalization=true`, `string.Normalize` est un
no-op silencieux sur le runtime utilisé : `"interprétation"` reste composé (`é` non décomposé en
`e` + accent), et le filtre ne retire donc rien. Le slug produit contenait alors un caractère
accentué, cassant à la fois l'URL et la stabilité de l'identifiant.

Détecté par `SlugTests.From_RemovesDiacriticsAndPunctuation` (`tests/UMonsPlanning.Pronote.Tests`),
qui échouait après l'activation du mode invariant.

## Décision

`InvariantGlobalization` reste à `false` (valeur par défaut du SDK). Le gain de taille de
déploiement ne justifie pas un identifiant public corrompu de façon dépendante de la plateforme
d'exécution ("comportement non spécifié" selon la documentation .NET pour `Normalize` en mode
invariant).

## Conséquences

- Le paquet ICU est chargé au runtime ; taille de déploiement légèrement supérieure sur
  l'hébergement mutualisé (0004).
- Aucun changement de code nécessaire ailleurs : `StringComparer.InvariantCulture` (tri des
  formations) reste correct dans les deux modes, seul `Normalize` était affecté.
