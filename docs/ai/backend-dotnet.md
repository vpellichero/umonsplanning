# Module — Backend .NET / ASP.NET Core

Complète le socle `CLAUDE.md`. Actif dès qu'il y a du C# dans le projet.

## Style et nommage

- Le style du runtime .NET fait référence ; `.editorconfig` est autoritaire et ne doit jamais être assoupli pour faire passer du code.
- `PascalCase` pour les types et membres, `camelCase` pour les locales et paramètres, `_camelCase` pour les champs privés, interfaces préfixées `I`.
- Namespaces à portée de fichier, un seul type de premier niveau par fichier, nom de fichier = nom du type.
- `var` quand le type est évident à droite de l'affectation, type explicite sinon.
- Documentation XML (en anglais) sur toute API publique.
- Pas d'abréviations : `customerRepository`, pas `custRepo`. Les acronymes de plus de deux lettres se traitent comme des mots (`HtmlParser`, `IoTDevice`).

## Typage et nullabilité

- `<Nullable>enable</Nullable>` et `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- Aucun opérateur `!` (null-forgiving) sans commentaire justifiant l'invariant.
- Préférer les types valeur immuables (`readonly record struct`) pour les identifiants et valeurs du domaine plutôt que des `string`/`Guid` nus (primitive obsession).
- `required` sur les propriétés obligatoires plutôt qu'un constructeur à dix paramètres optionnels.

## Asynchronisme

- Async de bout en bout. Aucun `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, `async void` (hors gestionnaires d'événements).
- Propager systématiquement `CancellationToken` et l'honorer dans les boucles et appels d'E/S.
- Suffixe `Async` sur les méthodes asynchrones. `ValueTask` uniquement sur un chemin chaud mesuré.
- `ConfigureAwait(false)` dans le code de bibliothèque, inutile dans le code applicatif ASP.NET Core.
- Pas de `Task.Run` pour du travail d'E/S ; pas de fire-and-forget (utiliser un `IHostedService`, un `BackgroundService` ou un mécanisme de file).

## Injection de dépendances

- Enregistrer avec la durée de vie la plus étroite correcte. Ne jamais capturer un service scoped dans un singleton (activer `ValidateScopes` et `ValidateOnBuild`).
- Une extension `IServiceCollection` par module fonctionnel (`AddOrdering()`), pas trois cents lignes dans `Program.cs`.
- Les dépendances passent par le constructeur ; pas de `IServiceProvider` injecté en dur, sauf factory explicite et justifiée.

## Configuration

- Pattern Options : `IOptions<T>` (singleton), `IOptionsSnapshot<T>` (scoped, rechargeable), `IOptionsMonitor<T>` (singleton rechargeable).
- Validation au démarrage : `.ValidateDataAnnotations().ValidateOnStart()`. Une configuration invalide doit empêcher le démarrage, pas produire une `NullReferenceException` en production.
- Aucune chaîne magique pour les clés ; une constante `SectionName` sur la classe d'options.
- Rien de sensible dans `appsettings.json`. Les secrets passent par les variables d'environnement : **DotNetEnv** + `.env.development` en local, injection CI/CD dans le `web.config` en mutualisé. Jamais `dotnet user-secrets`. Détail complet dans `securite-rgpd.md` §3.
- Le chargement DotNetEnv est conditionnel à l'existence du fichier : en production il n'y en a pas, et l'absence ne doit provoquer aucune erreur.
- `appsettings.json` ne contient que la configuration non sensible et identique partout. Tout ce qui varie par environnement vient des variables d'environnement, avec `__` pour les sections imbriquées (`ConnectionStrings__Default`).
- Toute variable lue par le code doit exister dans `.env.example`, décrite, dans le même commit — c'est la référence qui sert aussi à déclarer les secrets de la CI.

## Journalisation et observabilité

- `ILogger<T>` avec des templates de message : `_logger.LogInformation("Order {OrderId} shipped", id)`. Jamais d'interpolation de chaîne.
- Utiliser `LoggerMessage` (source-generated) sur les chemins chauds.
- Niveaux : `Trace`/`Debug` pour le diagnostic local, `Information` pour les événements métier, `Warning` pour l'anormal récupérable, `Error` pour l'échec d'une opération, `Critical` pour l'indisponibilité.
- Jamais de secret, jeton, mot de passe ou donnée personnelle dans les logs.
- Corrélation : conserver `TraceId`/`ActivityId`. Endpoint de santé : `/api/health`.

## Gestion des erreurs

- Exceptions pour l'exceptionnel ; résultats typés (`Result<T>`) ou validation pour les cas métier attendus.
- Exceptions du domaine dédiées, jamais `throw new Exception("...")`.
- Un middleware d'exception global convertit en `ProblemDetails` (RFC 9457). Aucun détail interne (stack trace, SQL, chemin) exposé au client.
- Ne jamais avaler une exception sans la journaliser et sans raison documentée. `throw;` et non `throw ex;`.

## API HTTP

- Verbes et codes de statut corrects ; `201 Created` + `Location` ; `204` sur suppression ; `409` sur conflit ; `422` sur validation métier.
- Validation d'entrée systématique côté serveur (voir §Validation), même si le client valide déjà.
- DTO d'entrée et de sortie explicites : ne jamais exposer une entité de persistance ni un ContentItem brut.
- Versionnage de l'API : aucun (API interne, seul `UMonsPlanning.Frontend` la consomme)
- Pagination, tri et filtrage bornés côté serveur (limite maximale de page imposée).
- OpenAPI généré et à jour.

## Bibliothèques imposées

Ces choix sont tranchés pour tous les projets. Ils ne se réauditent pas et ne se remplacent pas sans décision explicite du mainteneur. Ne jamais introduire d'équivalent concurrent en parallèle (AutoMapper, DataAnnotations pour la validation métier, NSubstitute/FakeItEasy, etc.) : deux mécanismes pour le même besoin dans une même solution est un défaut.

| Besoin | Bibliothèque | Licence |
|---|---|---|
| Validation métier | **FluentValidation** | Apache-2.0 |
| Mapping objet-objet | **Mapperly** (`Riok.Mapperly`) | Apache-2.0 |
| API GraphQL | **HotChocolate** (voir `graphql-hotchocolate.md`) | MIT |
| Tests | **xUnit**, **AwesomeAssertions**, **Bogus**, **Moq** (voir `tests-git.md`) | Apache-2.0 / MIT / BSD-3 |

## Validation — FluentValidation

- Un validateur par DTO d'entrée ou par commande, dans le même dossier que le type validé, nommé `<Type>Validator`.
- Enregistrement par assembly (`AddValidatorsFromAssemblyContaining<T>()`), jamais un à un à la main.
- **Exécution centralisée**, pas dans chaque contrôleur : filtre MVC ou `IEndpointFilter` pour les Minimal API, transformant les échecs en `ValidationProblemDetails` (RFC 9457). Ne pas utiliser le paquet `FluentValidation.AspNetCore` (déprécié) : câbler l'intégration explicitement.
- Les messages d'erreur sont **localisables** : passer par `IStringLocalizer` ou les clés de ressources, jamais de littéral français dans un validateur.
- Règles asynchrones (`MustAsync`) réservées à ce qui exige réellement une E/S (unicité en base) ; elles reçoivent le `CancellationToken`.
- Séparer la validation **syntaxique** (format, longueur, obligatoire — validateur du DTO) de la validation **métier** (invariants du domaine — dans le domaine lui-même, sous forme de garde ou de règle). FluentValidation ne remplace pas les invariants du modèle.
- `RuleSet` pour les variantes création/modification plutôt que deux validateurs quasi identiques.
- Chaque validateur a ses tests unitaires (`TestValidate`), cas passant et cas d'échec par règle.

## Mapping — Mapperly

- **Mapperly uniquement** : générateur de source, mapping résolu à la compilation, aucune réflexion à l'exécution. Aucun mapper écrit à la main, aucun mapper par réflexion.
- Déclaration : `[Mapper] internal static partial class <Source>To<Target>Mapper` (ou classe partielle instanciable si des dépendances sont nécessaires).
- Les mappers vivent dans la couche **Application**, jamais dans le domaine ni dans les contrôleurs.
- Les membres non mappés doivent **casser le build** : configurer les diagnostics Mapperly en erreur (`RMG012` membre source non mappé, `RMG020` membre cible non mappé) plutôt que les ignorer silencieusement. Un membre volontairement ignoré est marqué explicitement (`[MapperIgnoreTarget]` / `[MapperIgnoreSource]`) — l'oubli devient ainsi impossible.
- Mapping direction unique par défaut ; ne pas générer d'aller-retour « au cas où ».
- Aucune logique métier dans un mapper : pas de calcul, pas d'appel de service, pas d'accès aux données. Un mapper transporte, il ne décide pas.
- Entités de persistance et DTO d'API restent des types distincts. Ne jamais exposer une entité directement sous prétexte que le mapping serait trivial.
- Vérifier le code généré (`obj/**/Mapperly/**`) au moins une fois lors de l'ajout d'un mapper non trivial : c'est du C# lisible, et c'est là que se voient les conversions implicites indésirables.

## Accès aux données

- EF Core : `AsNoTracking()` en lecture, projections `Select` plutôt que chargement complet, `Include` explicite, jamais de lazy loading. Traquer les N+1.
- Requêtes paramétrées uniquement ; aucune concaténation SQL.
- Pas d'accès `DbContext` depuis la couche présentation ; le `DbContext` est scoped et non thread-safe.
- Transactions explicites sur les opérations multi-agrégats ; idempotence sur les traitements rejouables.
- Index créés en même temps que les requêtes qui en dépendent.
- Pour OrchardCore/YesSql, voir `orchardcore.md` : les règles diffèrent.

## Performance

- Pas d'optimisation sans mesure. Toute affirmation de gain s'accompagne d'un chiffre.
- Cache : `OutputCache` en mémoire (schedule/calendar) + cache fichier maison pour les dropdowns (`FormationCatalogCache`, voir CLAUDE.md §12) avec durées explicites ; toujours prévoir l'invalidation.
- `IAsyncEnumerable<T>` ou pagination pour les grands ensembles ; jamais de `ToList()` sur une table entière.
- `HttpClient` : instancié directement par `PronoteSession` (pas `IHttpClientFactory` — une seule session vivante à la fois, pas de pool de connexions à gérer). Aucune politique de résilience (Polly / `Microsoft.Extensions.Http.Resilience`) pour l'instant : voir CLAUDE.md §12 pour la justification et la condition d'ajout.

## Checklist du module

- [ ] Zéro avertissement, `.editorconfig` respecté, `dotnet format` propre.
- [ ] Nullabilité correcte, aucun `!` injustifié.
- [ ] `CancellationToken` propagé sur toute la chaîne asynchrone.
- [ ] Options validées au démarrage.
- [ ] Logs structurés, sans donnée sensible.
- [ ] Erreurs converties en `ProblemDetails`, rien d'interne exposé.
- [ ] Validation via FluentValidation, exécutée par filtre centralisé, messages localisables, validateurs testés.
- [ ] Mapping via Mapperly, diagnostics de membres non mappés en erreur, aucune logique dans les mappers.
- [ ] Aucune bibliothèque concurrente introduite en parallèle du socle imposé.
- [ ] Requêtes de données sans N+1, avec index correspondants.
