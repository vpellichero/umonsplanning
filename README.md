# UMonsPlanning

[![CI/CD](https://github.com/vpellichero/umonsplanning/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/vpellichero/umonsplanning/actions/workflows/ci-cd.yml)

**Votre horaire de cours UMONS, toujours à jour dans votre propre agenda.**

UMonsPlanning génère un lien d'abonnement à ajouter **une seule fois** dans Google Calendar, Outlook, Apple Calendar, Thunderbird ou Proton Calendar. Votre agenda se met ensuite à jour tout seul en fonction de votre horaire PRONOTE/HyperPlanning, du coup, plus jamais besoin de réexporter quoi que ce soit.

**Version en ligne :** <https://umonsplanning.pellichero.be>

> **Projet personnel, non officiel.** Aucun lien avec l'Université de Mons ni avec
> PRONOTE/Index Education, qui ne le soutiennent pas. L'outil consulte les mêmes pages d'horaires
> publiques et sans connexion ("espace invité") que l'UMONS publie déjà — aucune donnée privée
> ou authentifiée n'est utilisée.

---

## Comment ça marche

1. **Choisissez votre formation** (et votre section/groupe si nécessaire) via deux listes
   déroulantes. Les valeurs de ces listes sont récupérées en direct depuis PRONOTE.
2. **Récupérez votre lien.** Une URL est générée pour ce choix de cours, éventuellement restreinte
   à une période précisée. Rien n'est stocké côté serveur par utilisateur : l'URL porte elle-même les
   information de filtre.
3. **Ajoutez-le une seule fois** dans la fonction "s'abonner par URL" de votre application de
   calendrier favorite (voir la page **Aide** de l'application pour la marche à suivre par application).
   Votre agenda ira ensuite chercher cette URL selon son propre rythme, donc les changements
   de votre horaire PRONOTE apparaissent automatiquement. Il est cependant impossible de savoir à quelle
   heure exactement les mises-à-jour s'effectuent, celles-ci dépendent de l'application que vous utilisez.
4. **Prévisualisez avant de vous engager** : le bouton "Tester votre calendrier" récupère le
   vrai fichier `.ics` et affiche la première semaine en liste jour par jour directement dans le navigateur.
   Cela vous permet de vérifier le bon fonctionnement du lien.

Deux formats de sortie `.ics` sont proposés, via un switch dans la fenêtre de génération du
lien :

- **Un événement par cours** (par défaut) — chaque cours est une entrée distincte de l'agenda.
- **Un événement par jour** — une seule entrée "journée entière" par jour, avec le détail de
  chaque cours (horaire, salle, matière, type de cours, code du cours) listé dans sa description.

## Architecture

| Composant | Stack | Rôle |
|---|---|---|
| `src/UMonsPlanning.Pronote` | .NET 10 | Client PRONOTE rétro-ingénieré (gestion de session, chiffrement AES du numéro d'ordre, mapping de l'horaire). Voir [`docs/pronote-protocol.md`](docs/pronote-protocol.md) pour le détail complet du protocole. |
| `src/UMonsPlanning.Backend` | ASP.NET Core Minimal API | Façade REST : fournit le contenu des listes déroulantes (cache fichier, rafraîchi mensuellement) et l'endpoint d'export `.ics` (généré avec [Ical.Net](https://github.com/rianjs/ical.net)). Sert aussi les fichiers statiques du frontend en production (une seule application). |
| `src/UMonsPlanning.Frontend` | Angular 21 | La SPA décrite ci-dessus : générateur de lien, aperçu du calendrier, page d'aide. Prérendue statiquement (aucun serveur Node requis en production) — le résultat est servi par le backend ci-dessus. |
| `tools/UMonsPlanning.Cli` | .NET 10 | Outil de vérification en ligne de commande du client PRONOTE, utilisé quand le protocole doit être revérifié contre le serveur réel. |

Chaque décision d'architecture plus complexe (choix de dépendance, contrainte d'hébergement,
exception de licence, particularité du protocole) est consignée au fil de l'eau dans
[`docs/adr/`](docs/adr/). L'ensemble des conventions d'architecture et de développement suivies dans ce dépôt vit dans
[`CLAUDE.md`](CLAUDE.md).

## Lancer le projet soi-même

Prérequis : [SDK .NET 10](https://dotnet.microsoft.com/download), [Node.js](https://nodejs.org/)
20+, npm.

```bash
# Backend — http://localhost:5199/scalar pour la documentation API interactive
dotnet restore
dotnet run --project src/UMonsPlanning.Backend

# Frontend — http://localhost:4200, redirige /api vers le backend ci-dessus
cd src/UMonsPlanning.Frontend
npm install
npm start
```

```bash
# Tests
dotnet test --solution UMonsPlanning.slnx    # backend (48 tests)
cd src/UMonsPlanning.Frontend && npm test     # frontend (15 tests)
```

Le backend ne nécessite aucune configuration ni aucun secret : l'espace invité PRONOTE auquel il
s'adresse n'en demande pas non plus. Voir [`CLAUDE.md`](CLAUDE.md) pour la référence complète des
commandes, les conventions de code, et le raisonnement derrière chaque décision structurante de ce
dépôt.

## API

| Méthode | Route | Description |
|---|---|---|
| `GET` | `/api/formations` | Liste des choix d'études (menu déroulant #1), en cache fichier, rafraîchie mensuellement |
| `GET` | `/api/formations/{formation}/sections` | Sous-choix (menu déroulant #2) d'une formation |
| `GET` | `/api/calendar` | Calendrier académique : numérotation des semaines PRONOTE |
| `GET` | `/api/weeks/by-date/{date}` | Traduit une date en numéro de semaine PRONOTE |
| `GET` | `/api/schedule?formation=&section=&week=` | Horaire d'une semaine, au format JSON |
| `GET` | `/api/schedule.ics?formation=&section=&week=\|date=\|start=\|end=&layout=` | L'export `.ics` — c'est l'URL à laquelle on s'abonne |

La documentation interactive complète (Scalar) est servie sur `/scalar` de n'importe quelle
instance en cours d'exécution.

## Licence

[PolyForm Noncommercial License 1.0.0](LICENSE.md), avec une obligation d'attribution ajoutée en
cas de modification. En résumé : usage, étude et modification libres pour tout usage non
commercial ; tout usage commercial de ce code — en tout ou en partie — est interdit ; toute
republication d'une version modifiée doit créditer ce dépôt et y renvoyer par un lien.

## Remerciements

Construit à partir d'une analyse du protocole PRONOTE menée initialement comme un exercice de
rétro-ingénierie assisté par IA (voir [`docs/pronote-protocol.md`](docs/pronote-protocol.md) pour
le détail exact de ce qui a été découvert et comment). Utilise [Ical.Net](https://github.com/rianjs/ical.net)
(MIT) pour la génération RFC 5545 et [ical.js](https://github.com/kewisch/ical.js) (MPL-2.0) pour
le décodage côté client — voir [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) pour la liste
complète.

## Assistance IA

Ce projet a été développé avec l'assistance d'un assistant de programmation IA (Claude), sous la
supervision et la relecture du mainteneur. Le logo, l'icône et les visuels de l'application
(`src/UMonsPlanning.Frontend/public/logo.webp`, `logo-horizontal.webp`, `icon.webp`) ont été
générés par le module de génération d'image de ChatGPT.
