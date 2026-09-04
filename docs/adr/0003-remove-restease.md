# 0003 — Retirer RestEase, appeler PRONOTE via `HttpClient` nu

- Statut : accepté
- Date : 2026-09-02

## Contexte

Le client PRONOTE (`UMonsPlanning.Pronote`) utilisait **RestEase** (client REST déclaratif) pour
définir `IPronoteApi` (deux méthodes : page d'entrée GET, appel de fonction POST) avec des
sérialiseurs `System.Text.Json` sur mesure pour contourner des contraintes du protocole PRONOTE
(pas de paramètre `charset` sur le `Content-Type`).

## Constat (audit `docs/ai/dependances.md` §5)

RestEase 1.6.4, dernière version publiée le **2023-04-19** : plus de 3 ans sans nouvelle version au
moment de l'audit (2026-09), très au-delà du seuil de 18 mois. Rien n'indique que le projet soit
"manifestement terminé" au sens de l'exception prévue par la règle — c'est un projet à mainteneur
unique, sans commit ni release récente.

## Décision

Retrait de RestEase. Les deux appels HTTP (page d'entrée, appel de fonction) sont réécrits avec un
`HttpClient` nu dans `PronoteSession` : la surface exposée par RestEase (deux méthodes) ne
justifiait pas une bibliothèque de client REST déclaratif, encore moins une abandonnée. Les
sérialiseurs `SystemTextJsonRequestBodySerializer`/`SystemTextJsonResponseDeserializer` (plomberie
RestEase) sont supprimés en même temps : le `Content-Type` sans `charset` est désormais construit
directement sur le `StringContent`.

## Conséquences

- Une dépendance de moins (`RestEase`), donc une ligne de moins dans `THIRD-PARTY-NOTICES.md`.
- `Protocol/IPronoteApi.cs` et `Protocol/SystemTextJsonSerializers.cs` supprimés.
- Comportement inchangé : couvert par les tests existants (`ProtocolTests`) et vérifié
  manuellement contre le serveur PRONOTE réel après migration.
