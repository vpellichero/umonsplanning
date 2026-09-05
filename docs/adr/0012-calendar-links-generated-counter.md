# 0012 — Compteur de calendriers générés (page d'accueil)

- Statut : accepté
- Date : 2026-09-04

## Contexte

Le mainteneur a demandé un compteur défilant sur la page d'accueil, affichant le nombre de liens
de calendrier générés — sans compter les tests (bouton « Tester » de la modale de génération) ni
les rafraîchissements quotidiens que Google Calendar/Outlook/Apple Calendar effectuent tout seuls
sur une URL déjà souscrite.

Deux mécanismes ont été envisagés :

- **Déduplication par contenu d'URL sur `GET /api/schedule.ics`** : compter la première fois qu'une
  combinaison `formation+section+layout+title+start+end` est demandée sans `week`/`date` (signal
  déjà utilisé par le endpoint pour distinguer un aperçu d'une souscription, voir sa
  `WithDescription`). Sous-compte fortement : deux personnes qui choisissent la même formation/
  section sans filtre de dates — le cas le plus fréquent — génèrent la même URL et ne comptent que
  pour une seule.
- **Évènement de génération découplé, déclenché par le frontend** : un nouveau endpoint, appelé
  uniquement au clic sur « Copier le lien » (jamais par « Tester », jamais par un rafraîchissement
  d'agenda puisque ceux-ci ne parlent qu'à `/api/schedule.ics`). Compte chaque génération réelle,
  y compris deux personnes ayant fait le même choix de formation/section.

Le mainteneur a choisi la seconde option. Il a aussi tranché explicitement qu'aucun garde-fou
anti-inflation (ex. flag `localStorage` pour ne compter qu'une fois par navigateur) n'était
nécessaire pour un premier jet : un compteur brut, incrémenté à chaque clic, suffit pour un chiffre
« vitrine ».

## Décision

**Backend** : `POST /api/stats/calendar-links` incrémente un compteur, `GET /api/stats/calendar-links`
le lit ; les deux renvoient `{ count }` (`CalendarLinkStatsDto`). `CalendarLinkCounter`
(`UMonsPlanning.Backend/Stats/`) persiste ce compteur dans un fichier
(`App_Data/stats/calendar-links.json` par défaut, configurable via `StatsOptions`), même mécanisme
que `FormationCatalogCache` (docs/adr/0004) : verrou (`SemaphoreSlim`) autour d'un cycle
lecture-incrément-écriture atomique (fichier temporaire puis `File.Move`) — pas de base de données
(CLAUDE.md §4). Aucune protection anti-abus dédiée au-delà du rate limiter global déjà en place sur
tout le backend (120 req/min/IP) : décision assumée ci-dessus, à revoir si un abus réel est observé.

**Frontend** : `StatsService` (`core/stats.service.ts`) expose `calendarLinksGenerated` (un
`httpResource`, actif uniquement côté navigateur — même garde que `CatalogService`, docs/adr/0004
décision 2) et `recordCalendarLinkGenerated()`, appelé en fire-and-forget par
`CalendarLinkDialog.copyLink()` après que la copie presse-papiers a réussi (jamais par
`testCalendar()`). `CalendarLinkCounter` (`features/home/`) anime l'affichage (montée progressive
sur 1,2s, easing cubique, `requestAnimationFrame`) à chaque changement de la valeur lue —
`toLocaleString('fr-BE')` pour le séparateur de milliers (CLAUDE.md §8), sans dépendre du
`LOCALE_ID` Angular (non configuré dans ce projet).

## Conséquences

- Le compteur peut être inférieur au nombre réel de personnes ayant souscrit un calendrier : un
  clic sur « Copier le lien » sans qu'il soit effectivement collé dans une application d'agenda
  compte quand même. Accepté comme approximation raisonnable pour un chiffre vitrine.
- Un navigateur qui régénère plusieurs liens (formations différentes, ou un même lien recopié
  plusieurs fois) incrémente autant de fois — décision assumée ci-dessus.
- `App_Data/stats/` suit la même règle que `App_Data/catalog-cache/` : non commité (`.gitignore`),
  doit être accessible en écriture par le pool d'application IIS en production (piège déjà
  documenté CLAUDE.md §12 pour `catalog-cache`, s'applique identiquement ici).
