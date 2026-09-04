# 0001 — Générer les fichiers .ics avec Ical.Net plutôt qu'à la main

- Statut : accepté
- Date : 2026-09-02

## Contexte

Le backend doit produire un flux iCalendar (RFC 5545) exposé par `/api/schedule.ics`, destiné à
être souscrit tel quel par Google Calendar, Outlook et Apple Calendar. Le format impose des règles
non triviales : pliage des lignes à 75 octets, échappement des virgules/points-virgules/retours à
la ligne dans les valeurs, sérialisation `VTIMEZONE` correcte pour les fuseaux avec heure d'été.

## Options considérées

| Option | Licence | Dernière version | Poids |
|---|---|---|---|
| **Ical.Net** | MIT | 5.2.3 (2026) | ~1 dépendance transitive (NodaTime) |
| Implémentation maison | — | — | zéro dépendance, mais réimplémente le pliage de lignes, l'échappement et les règles `VTIMEZONE` |

## Décision

Utilisation d'**Ical.Net** (MIT), retenue explicitement par le mainteneur après présentation de
l'arbitrage (voir `docs/ai/dependances.md` §2). Vérifié par une sonde de réflexion sur le paquet
5.2.3 avant intégration (aucune API supposée) : `Calendar`, `CalendarEvent`, `CalDateTime`,
`CalendarSerializer`, `Calendar.AddTimeZone(string)` pour embarquer un bloc `VTIMEZONE` complet.

## Conséquences

- Le pliage de lignes et l'échappement RFC 5545 sont délégués à la bibliothèque : confirmé par
  test manuel contre le serveur PRONOTE réel (salles séparées par une virgule correctement
  échappées et repliées dans `LOCATION`/`DESCRIPTION`).
- `UMonsPlanning.Backend` dépend de `Ical.Net` (voir `Directory.Packages.props`).
- `docs/THIRD-PARTY-NOTICES.md` référence la licence MIT d'Ical.Net.
