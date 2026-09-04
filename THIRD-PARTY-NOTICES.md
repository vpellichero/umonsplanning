# Notices tierces

Ce fichier liste les dépendances tierces d'UMonsPlanning, leur version et leur licence,
conformément à `docs/ai/dependances.md` §3. Toutes les licences ci-dessous permettent un usage
propriétaire sans obligation de divulgation, à l'exception d'ical.js (voir l'exception explicite
consignée dans `docs/adr/0005-icaljs-mpl-license-exception.md`).

## Backend (`UMonsPlanning.Backend`, `UMonsPlanning.Pronote`)

| Paquet | Version | Licence |
|---|---|---|
| Microsoft.Extensions.Options | 10.0.11 | MIT |
| Microsoft.Extensions.Logging.Abstractions | 10.0.11 | MIT |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.11 | MIT |
| Microsoft.Extensions.Logging.Console | 10.0.11 | MIT |
| Microsoft.AspNetCore.OpenApi | 10.0.11 | MIT |
| Scalar.AspNetCore | 2.17.2 | MIT |
| FluentValidation | 12.1.1 | Apache-2.0 |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | Apache-2.0 |
| Ical.Net | 5.2.3 | MIT |
| NodaTime (transitive, via Ical.Net) | 3.2.2+ | Apache-2.0 |

## Projets de tests Backend et Pronote

| Paquet | Version | Licence |
|---|---|---|
| xunit.v3 | 4.0.0 | Apache-2.0 |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT |
| coverlet.collector | 6.0.4 | MIT |
| AwesomeAssertions | 9.6.0 | Apache-2.0 |
| Bogus | 35.6.5 | MIT |
| Moq | 4.20.72 | BSD-3-Clause |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.11 | MIT |

## Frontend (`UMonsPlanning.Frontend`)

| Paquet | Version | Licence |
|---|---|---|
| Angular (`@angular/*`) | 21.x | MIT |
| Tailwind CSS | 4.x | MIT |
| **ical.js** | 2.2.1 | **MPL-2.0 — exception explicite, voir ADR 0005** |

Pas de kit UI (`docs/adr/0007-primeng-ui-kit.md`) : PrimeNG a été écarté après découverte que son
paquet de thème officiel pour la v21 (`@primeuix/themes`) exige une clé de licence PrimeUI, même
sur le palier gratuit — incompatible avec la politique de licences de ce dépôt.

À mettre à jour à chaque ajout, remplacement ou suppression de dépendance
(`docs/ai/dependances.md` §6). Versions épinglées de façon exacte ; pas de plage flottante.
