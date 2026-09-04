# 0006 — OpenAPI natif (`Microsoft.AspNetCore.OpenApi`) + Scalar plutôt que Swashbuckle

- Statut : accepté
- Date : 2026-09-02

## Contexte

Le projet repris utilisait **Swashbuckle.AspNetCore** pour générer le document OpenAPI et servir
Swagger UI sur `/swagger`. ASP.NET Core fournit depuis .NET 9 une génération de document OpenAPI
intégrée au framework (`Microsoft.AspNetCore.OpenApi`, `AddOpenApi()`/`MapOpenApi()`), rendant
Swashbuckle redondant pour ce seul besoin (deux mécanismes de génération OpenAPI dans la même
solution est explicitement déconseillé par `docs/ai/backend-dotnet.md`).

## Options considérées pour l'interface de documentation

| Option | Licence | Dernière version | Poids |
|---|---|---|---|
| **Scalar.AspNetCore** | MIT | 2.17.2 (2026-08-28) | zéro dépendance transitive |
| Swashbuckle.AspNetCore (SwaggerUI) | MIT | — | duplique la génération OpenAPI déjà native en .NET 10 |
| Aucune UI (JSON brut) | — | — | perte de confort pour tester l'API manuellement |

## Décision

Génération via `Microsoft.AspNetCore.OpenApi` (natif, pas un paquet tiers audité) ; interface de
navigation via **Scalar.AspNetCore** (MIT), servie sur `/scalar` (route par défaut de la
bibliothèque, vérifiée par sonde avant intégration — voir `docs/ai/backend-dotnet.md` §9). `/`
redirige vers `/scalar`.

## Conséquences

- `Swashbuckle.AspNetCore` retiré du projet.
- Document OpenAPI JSON disponible sur `/openapi/v1.json` (route par défaut du générateur natif).
