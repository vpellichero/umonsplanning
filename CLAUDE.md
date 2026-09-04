# CLAUDE.md

Manuel opératoire des agents IA travaillant sur ce dépôt.
À lire **intégralement** avant la première modification, puis à relire section par section avant d'intervenir sur un domaine non encore abordé dans la session.

---

## 1. Identité du projet

| Champ | Valeur |
|---|---|
| Nom | `UMonsPlanning` |
| Objectif (1–2 phrases) | Générer une URL de calendrier (`.ics`) toujours à jour à partir des horaires PRONOTE de l'UMONS, à souscrire dans une application de calendrier (Google Calendar, Outlook, Apple Calendar). |
| Client / propriétaire | Projet personnel de Vincent Pellichero — **non commercial**, sans obligation contractuelle envers l'UMONS ni envers un tiers. |
| Type | Application métier : une façade REST + une SPA (page d'accueil + page d'aide). Pas de compte utilisateur, pas de persistance en base. |
| Environnements | `URL_DEV` : `http://localhost:4200` (frontend, `ng serve`) + `http://localhost:5199` (backend, `dotnet run`) · `URL_STAGING` : <https://test.umonsplanning.pellichero.be> (branche `develop`) · `URL_PROD` : <https://umonsplanning.pellichero.be> (branche `main`) |
| Hébergement | Mutualisé Windows (type Plesk/IIS) — voir `docs/adr/0004-mutualized-hosting-topology.md` pour les conséquences d'architecture |
| Langues / marchés | fr-BE uniquement (public UMONS francophone). Aucune infrastructure de localisation introduite pour ce seul marché — voir §8. |

Dépôt destiné à être publié sur un GitHub public. Licence : **PolyForm Noncommercial 1.0.0** +
clause d'attribution ajoutée en cas de modification, voir `LICENSE.md` à la racine — usage libre,
interdiction d'usage commercial ou d'en tirer profit, citation du dépôt d'origine obligatoire en
cas de republication modifiée. Cela ne dispense pas de vérifier la licence de chaque dépendance
avant de l'intégrer (§ ci-dessous, détail dans `docs/ai/dependances.md`) : la politique de licences
compatibles reste la même qu'un projet fermé, par prudence.

---

## 2. Modules actifs

<!-- @docs/ai/backend-dotnet.md      Conventions C# / ASP.NET Core -->
<!-- @docs/ai/angular.md             Front Angular v21 -->
<!-- @docs/ai/frontend-ui.md         Mobile-first, Tailwind, accessibilité WCAG 2.2 AA -->
<!-- @docs/ai/performance.md         Core Web Vitals, budgets (profil B, voir §12) -->
<!-- @docs/ai/dependances.md         Audit de l'existant, licences, seuils de maintenance -->
<!-- @docs/ai/securite-rgpd.md       OWASP, secrets, en-têtes — RGPD très allégé, voir §12 -->
<!-- @docs/ai/tests-git.md           Stratégie de tests, conventions Git, ADR -->

Non utilisés, supprimés du dépôt : `orchardcore.md` (pas de CMS), `graphql-hotchocolate.md`
(façade REST simple, aucun besoin de graphe de données interrogeable), `sakai.md` (pas de kit
back-office Sakai-ng), `seo.md` (outil utilitaire sans enjeu de référencement public).

Autres documents du projet :
<!-- @docs/adr/ — une décision par fichier, voir §11 -->

---

## 3. Langue

- **Échanges avec le mainteneur : en français.**
- **Le code et ses commentaires sont en anglais**, sans exception : identifiants, types, membres,
  paramètres, variables, commentaires XML et inline, messages de log et d'exception, noms de
  tests, noms de migrations. Ce projet cible une université belge francophone (Wallonie) : rien
  n'impose l'anglais au-delà du code lui-même.
- **Toute la documentation reste en français** : `README.md`, `docs/pronote-protocol.md`,
  `docs/adr/*.md`, les modules `docs/ai/*.md`, `CLAUDE.md` lui-même. Décision explicite du
  mainteneur, revenant sur une tentative précédente de tout passer en anglais — ne pas la
  réappliquer sans qu'il le redemande explicitement.
- Exception : les fichiers dont le contenu n'est pas de la prose libre mais un format externe figé
  — `LICENSE.md` (texte légal officiel de la PolyForm Noncommercial License, en anglais dans sa
  version de référence ; le traduire risquerait d'en altérer la portée juridique) — restent dans
  leur langue d'origine.
- Messages de commit, noms de branches, titres et descriptions de PR : en anglais (convention Git
  usuelle), voir `docs/ai/tests-git.md` §2.
- **Contenu produit pour l'utilisateur final du calendrier** (libellés PRONOTE, description des
  événements `.ics` — « Groupes : », « Statut : ») : reste en français, à l'identique des données
  sources — cohérent avec le marché fr-BE exclusif (§1). Les messages d'erreur de l'API
  (`ProblemDetails`, validation FluentValidation) restent en anglais comme le reste du code : ce
  sont des diagnostics destinés au développeur/au frontend, pas du contenu affiché tel quel à
  l'utilisateur.
- Les termes métier PRONOTE (« formation », « section », « semaine », « BAB3 », etc.) sont
  documentés au fil du code plutôt que dans un glossaire séparé — le domaine est petit et stable.

---

## 4. Stack technique

**Backend**
- .NET **10** / C# 14 — `LangVersion`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors` définis dans `Directory.Build.props`.
- ASP.NET Core — Minimal API.
- Accès données : **aucun** — façade sans état au-dessus de PRONOTE, avec un cache fichier pour les listes de dropdown (voir §12). Aucune base de données.
- Socle C# imposé, non réauditable : **FluentValidation** (validation), **Mapperly** (mapping — non référencé actuellement, voir §12), **xUnit v3 + AwesomeAssertions + Bogus + Moq** (tests). HotChocolate/GraphQL non utilisé (pas de graphe de données à interroger).
- Génération iCalendar : **Ical.Net** (`docs/adr/0001-ical-net-for-ics-generation.md`).
- Documentation API : `Microsoft.AspNetCore.OpenApi` (natif) + **Scalar.AspNetCore** (`docs/adr/0006-native-openapi-and-scalar.md`), servie sur `/scalar`.

**Frontend**
- Angular **21** (version exacte épinglée dans `src/UMonsPlanning.Frontend/package.json`).
- Tailwind CSS 4.x — solution de style par défaut.
- Kit UI : **aucun** — composants sur mesure (`<select>`/`<dialog>` natifs stylés Tailwind), voir `docs/adr/0007-primeng-ui-kit.md` (PrimeNG écarté après découverte d'une licence à clé sur son paquet de thème).
- Rendu : **prérendu statique (SSG)** au build, pas de serveur Node en production (`docs/adr/0004-mutualized-hosting-topology.md`).
- Parsing ICS côté client : **ical.js** (`docs/adr/0005-icaljs-mpl-license-exception.md`).
- Outillage : Angular CLI (esbuild) + npm.

**Outils**
- Gestionnaires de paquets : NuGet (Central Package Management, `Directory.Packages.props`) + npm (lockfile commité).
- Conteneurs : aucun en production (hébergement mutualisé) ; usage local libre s'il simplifie le poste de développement.
- CI/CD : GitHub Actions (`.github/workflows/ci-cd.yml`) — build/tests sur chaque branche et PR,
  déploiement FTPS automatique (`develop` → test, `main` → production). Voir
  `docs/adr/0010-cicd-github-actions-ftps-deploy.md`.
- IDE : au choix du mainteneur (Visual Studio, VS Code, Rider).

---

## 5. Structure du dépôt

```text
.github/
  workflows/ci-cd.yml               Build/tests à chaque branche, déploiement FTPS (ADR 0010)
UMonsPlanning.slnx
global.json                        Épingle le SDK .NET et le mode `dotnet test` (Microsoft.Testing.Platform)
Directory.Build.props              TargetFramework net10.0, Nullable, TreatWarningsAsErrors, etc.
Directory.Packages.props           Central Package Management — toutes les versions NuGet épinglées ici
THIRD-PARTY-NOTICES.md
docs/
  ai/                              Modules d'instructions importés par ce fichier
  adr/                             Décisions d'architecture (§11)
src/
  UMonsPlanning.Pronote/           Bibliothèque : client HttpClient + session PRONOTE + mapping
    Protocol/                      DTO du protocole PRONOTE, chiffrement du numéro d'ordre
    Internal/                      Session, mapping grille → modèle stable, slugs
    Models/                        DTO publics (ResourceDto, ScheduleDto, CourseDto, ...)
    IPronoteClient.cs              Abstraction (testabilité) de PronoteClient
    PronoteClient.cs               API publique de la bibliothèque
  UMonsPlanning.Backend/           API web ASP.NET Core (Minimal API + OpenAPI/Scalar)
    Catalog/                       Cache fichier des dropdowns (formations/sections), rafraîchi au mois
    Calendar/                      Construction du fichier .ics (Ical.Net)
    Contracts/                     DTO de requête + validateurs FluentValidation
    Validation/                    Filtre de validation centralisé (IEndpointFilter)
    Endpoints/                     Extensions IEndpointRouteBuilder par domaine fonctionnel
  UMonsPlanning.Frontend/          Workspace Angular (SPA, prérendu, Tailwind)
    src/app/app.routes.ts          Deux routes statiques : `/` et `/aide`
    src/app/features/home/         Page d'accueil (dropdowns, lien, aperçu)
    src/app/features/help/         Page d'aide (liens par application de calendrier)
    src/app/features/calendar-link/ Modales de génération de lien et d'aperçu du calendrier
tests/
  UMonsPlanning.Pronote.Tests/
  UMonsPlanning.Backend.Tests/
tools/
  UMonsPlanning.Cli/               Vérification en ligne de commande de la bibliothèque Pronote
postman/                           Collection Postman de l'API *source* PRONOTE (rétro-ingénierie)
```

**Règles de couches**
- `UMonsPlanning.Pronote` ne référence aucun paquet exposant un détail d'ASP.NET Core : c'est une bibliothèque autonome, utilisable par le backend comme par `UMonsPlanning.Cli`.
- `UMonsPlanning.Backend` dépend de `UMonsPlanning.Pronote` (jamais l'inverse).
- Le frontend ne connaît que l'API HTTP du backend (`/api/...`), jamais le protocole PRONOTE.

---

## 6. Commandes

```bash
# Backend
dotnet restore
dotnet build UMonsPlanning.slnx --nologo
dotnet run --project src/UMonsPlanning.Backend        # http://localhost:5199/scalar
dotnet test --solution UMonsPlanning.slnx              # mode Microsoft.Testing.Platform (global.json)
dotnet format --verify-no-changes

# Frontend (depuis src/UMonsPlanning.Frontend/)
npm install
npm start             # ng serve, proxy vers le backend local (proxy.conf.json)
npm run build         # ng build — sortie statique prérendue
npm test              # Vitest (choix par défaut d'Angular CLI 21)
```

- Compiler et exécuter les tests **avant** de déclarer une tâche terminée. Un code qui ne compile pas n'est pas un livrable.
- `dotnet test` utilise le mode natif Microsoft.Testing.Platform (`global.json`) : invoquer avec `--project <csproj>` ou `--solution <slnx>`, pas un chemin positionnel (voir la doc officielle citée dans l'historique du projet si un projet de test ne démarre pas).
- Secrets et configuration d'environnement : `.env.example` est la référence unique et le seul fichier commité ; `.env.development` via **DotNetEnv** en local ; en déploiement mutualisé, les valeurs sont injectées dans le `web.config` (détail dans `securite-rgpd.md` §3). Ce projet n'a **actuellement aucun secret** (l'API PRONOTE source ne demande ni clé ni identifiant) : `.env.example` reste vide ou absent tant qu'aucune variable sensible n'apparaît.
- Aucune commande destructrice (`git reset --hard`, `git push --force`, suppression de base) sans accord explicite.
- Ne jamais committer ni pousser sans qu'on le demande.

---

## 7. Principes d'ingénierie (non négociables)

- **SOLID**, DRY, KISS, YAGNI. Composition plutôt qu'héritage. Programmer contre des abstractions possédées par le consommateur.
- **Les design patterns sont un vocabulaire, pas un objectif.** Un pattern se justifie s'il supprime une complexité réelle. Exemple appliqué dans ce projet : Mapperly est dans le socle imposé mais n'est **pas** référencé — il n'existe aucune traduction structurelle DTO-à-DTO dans le code (le backend consomme directement les DTO de `UMonsPlanning.Pronote`) ; l'inventer pour « utiliser la bibliothèque » serait le pattern qui ne fait que relayer des appels que ce principe interdit.
- **Petites unités.** Une méthode fait une chose. Une classe a une seule raison de changer.
- **Échouer tôt et explicitement.** Validation à la frontière, guard clauses en tête, aucun `catch` silencieux.
- **Immuabilité par défaut** : `readonly`, `record`, propriétés `init`, `IReadOnlyList<T>` en surface publique.
- **Injection de dépendances partout.** `IPronoteClient` existe spécifiquement pour que le backend dépende d'une abstraction plutôt que de la classe scellée `PronoteClient`.
- **Aucun code mort, aucun code commenté, aucun `TODO` dans un livrable.** Ce qui est volontairement hors périmètre se dit dans la réponse, pas dans le code.

Détail des conventions par technologie : voir les modules activés en §2.

---

## 8. Données, localisation, temps

- Pas de migration ni de base de données dans ce projet.
- Localisation : voir §3 — pas de mécanisme d'i18n, marché fr-BE unique et documenté comme tel.
- Dates, nombres formatés par la culture `fr-BE` côté Angular ; le backend produit des formats invariants (JSON, iCalendar) — voir `docs/adr/0002-no-invariant-globalization.md` pour la raison précise de **ne pas** activer `InvariantGlobalization` malgré tout (`Slug.From` dépend d'un `Normalize` correct).
- Stockage en UTC (`DtStamp` des événements ICS, horodatage du cache), affichage en **Europe/Brussels** (créneaux de cours, `DTSTART`/`DTEND` avec `VTIMEZONE`). `TimeProvider` injecté partout où l'heure courante compte (`FormationCatalogCache`, génération ICS) — jamais `DateTime.Now`/`DateTime.UtcNow` en dur dans ce genre de code.

---

## 9. Ne jamais deviner : lire les sources

Quand une API, une signature, un espace de noms ou un comportement de framework est incertain, la règle est de **lire la source de vérité**, pas de supposer.

Sources de vérité, par ordre de priorité :
1. Le code existant de ce dépôt.
2. Le paquet NuGet/npm réellement restauré (`~/.nuget/packages/<id>/<version>/lib/...` ; une sonde de réflexion jetable est plus fiable qu'un souvenir de l'API pour une bibliothèque peu familière — c'est ainsi qu'ont été vérifiés `Ical.Net` avant `docs/adr/0001` et la route par défaut de `Scalar.AspNetCore` avant `docs/adr/0006`).
3. La documentation officielle de la version épinglée en §4.

Si l'API n'existe pas dans la version utilisée, le dire et proposer des alternatives. **Ne jamais fabriquer une API.**

---

## 10. Méthode de travail

**Avant d'écrire**
1. Lire le code existant du domaine concerné et en reprendre les patterns.
2. Vérifier les signatures dans les sources locales (§9).
3. Si la demande est ambiguë, touche à l'architecture, ou admet plusieurs conceptions raisonnables : **poser la question avant d'implémenter**.
4. Au-delà d'un changement trivial : annoncer le plan et obtenir l'accord.

**Pendant**
- Code complet et prêt pour la production. Aucun `// TODO: implement`, aucun `NotImplementedException`, aucun stub renvoyant des données factices.
- Ne changer que ce que la tâche exige.
- Un problème découvert hors périmètre se signale, il ne se corrige pas en silence.

**Après**
- Compiler, exécuter les tests, passer les linters et formateurs.
- Reprendre la Definition of Done (module actif concerné) et indiquer honnêtement ce qui est satisfait, ce qui ne l'est pas, et pourquoi.
- Signaler toute hypothèse, supposition ou point non vérifié.

**Jamais**
- Inventer une API, une clé de configuration, un paquet NuGet/npm, une option de CLI ou un lien de documentation.
- Affirmer qu'une chose est testée, mesurée ou vérifiée quand elle ne l'est pas.
- Désactiver une règle, un avertissement, un test ou un contrôle de type pour faire passer un build.
- Mentionner l'assistance IA dans un message de commit ou une description de PR.

---

## 11. Definition of Done

- [ ] Compile sans avertissement ; formateur propre (`dotnet format --verify-no-changes`).
- [ ] Tests écrits et passants (`dotnet test --solution UMonsPlanning.slnx`) ; test de non-régression pour toute correction de bug.
- [ ] Code en anglais, conforme aux conventions, sans code mort ni TODO.
- [ ] Aucune nouvelle dépendance sans l'audit `dependances.md` et un accord explicite — un ADR dans `docs/adr/` pour toute dépendance ajoutée/retirée/remplacée, tout choix de cache/hébergement, ou tout écart assumé à ce fichier.
- [ ] Aucun secret, identifiant ou donnée personnelle commité ou journalisé.
- [ ] `THIRD-PARTY-NOTICES.md` à jour si une dépendance a changé.
- [ ] Documentation / ADR mise à jour si pertinent.

Chaque module activé en §2 ajoute sa propre checklist (accessibilité, performance, sécurité…), tempérée par les allègements explicites de §12.

---

## 12. Règles spécifiques au projet

- **API en lecture seule, sans authentification.** Toutes les données exposées par `/api/*`
  (formations, sections, horaires) sont déjà publiques sur l'espace invité PRONOTE de l'UMONS :
  aucune donnée personnelle n'est traitée, aucun compte utilisateur n'existe. `docs/ai/securite-rgpd.md`
  §2 (authentification), §7 (RGPD : registre, bandeau de consentement, mentions légales) **ne
  s'applique pas** — il n'y a ni compte, ni cookie non essentiel, ni traceur. Les sections OWASP
  restantes (validation d'entrée, en-têtes de transport, gestion des secrets, journalisation)
  s'appliquent normalement.
- **Cache des dropdowns : rafraîchi à la demande, pas par minuterie.** Voir
  `docs/adr/0004-mutualized-hosting-topology.md`. `FormationCatalogCache` stocke ses fichiers dans
  `CatalogOptions.CacheDirectory` (`App_Data/catalog-cache` par défaut, non commité).
- **Pas de `IHostedService`/`BackgroundService`** dans `UMonsPlanning.Backend` (hébergement mutualisé,
  processus non long-running garanti).
- **Frontend en prérendu (SSG), pas de serveur Node en production.** `window`/`document` uniquement
  derrière une garde (`afterNextRender`, `isPlatformBrowser`) — toute la logique de génération de
  lien et d'appel `/api/schedule.ics` est de toute façon déclenchée par une interaction utilisateur,
  donc intrinsèquement post-hydratation.
- **Le backend sert aussi le frontend** (fichiers statiques + fallback SPA dans le même processus
  ASP.NET Core, voir `docs/adr/0009-backend-serves-frontend.md`, qui amende
  `docs/adr/0004-mutualized-hosting-topology.md` décision 3) : une seule application IIS pour tout
  le site, `/api` comme routage interne plutôt que comme application IIS séparée. Conséquence :
  `app_offline.htm`/`_app_offline.htm` (bascule faite par le pipeline de déploiement, voir
  `docs/adr/0010-cicd-github-actions-ftps-deploy.md`) coupe tout le site avec un seul fichier,
  reconnu nativement par ANCM à la racine de l'application. Même origine via sous-chemin `/api`,
  pas de sous-domaine séparé, pour que `window.location.origin` suffise à construire l'URL de
  calendrier dans tous les environnements — aucune politique CORS n'est nécessaire.
- **Résilience réseau vers PRONOTE : non implémentée pour l'instant.** `PronoteClient` gère déjà le
  renouvellement de session sur expiration, mais aucune politique de retry/circuit breaker
  (`Microsoft.Extensions.Http.Resilience`) n'a été ajoutée — le volume de requêtes est faible et
  PRONOTE est le seul dépendant externe. À ajouter si des échecs transitoires sont observés en
  production ; ne pas l'ajouter préventivement sans un incident réel (YAGNI).
- **Performance frontend : profil B** (`docs/ai/performance.md` §2, application métier / SPA
  Angular) — bundle initial ≤ 250 Ko gzip, budgets `angular.json` en échec de build au dépassement.
- **Identité visuelle** : logo et icône dans `src/UMonsPlanning.Frontend/public/` (`logo.webp`,
  `logo-horizontal.webp`, `icon.webp`), générés par un outil de génération d'image IA — voir la
  note du `README.md` racine. Pipeline de favicons complet généré depuis `icon.webp` (§6 de
  `performance.md`) via `src/UMonsPlanning.Frontend/scripts/generate-icons.mjs` — à relancer si le
  logo change (voir le README du frontend pour la commande). Pas de `favicon.svg` : la source est
  une image matricielle (WebP), pas un vecteur.
- **Numéro de session PRONOTE, année académique** : `Pronote:BaseUrl` contient l'année dans le nom
  d'hôte (`hplanning2026.umons.ac.be`) et devra être mis à jour à chaque rentrée académique — déjà
  externalisé en configuration, pas de code à changer.

### Contraintes et pièges connus
- Le dossier de cache du backend (`App_Data/catalog-cache`) doit être accessible en écriture par le
  pool d'application IIS en production — à vérifier explicitement à la mise en service, ce type
  d'hébergement refuse parfois l'écriture hors de certains dossiers.
- Une session PRONOTE expire vite et son compteur d'ordre est un compteur partagé côté serveur :
  toute modification du protocole (`UMonsPlanning.Pronote/Protocol`, `Internal/PronoteSession.cs`)
  doit être revérifiée avec `dotnet run --project tools/UMonsPlanning.Cli` contre le serveur réel,
  pas seulement avec les fixtures de test.
- `Slug.From` doit rester correct sous compilation .NET normale (ICU chargée) : voir
  `docs/adr/0002-no-invariant-globalization.md` avant de jamais reconsidérer `InvariantGlobalization`.
- `AllowedHosts` (`appsettings.json`) liste les domaines réels de production : un domaine
  supplémentaire (nouveau sous-domaine, renommage) doit y être ajouté, sinon IIS/Kestrel répond
  400 « Invalid Hostname » à toute requête légitime sur ce domaine.

### Hors périmètre
- Authentification, comptes utilisateurs, favoris multi-appareils synchronisés (aucun besoin
  identifié : l'URL de calendrier générée est elle-même le seul « état » à conserver, côté
  utilisateur, dans son application de calendrier).
- Tests end-to-end (Playwright) : non écrits dans cette session, faute de parcours à risque
  justifiant l'investissement pour un projet personnel à un seul contributeur ; à réévaluer si des
  régressions utilisateur apparaissent.
