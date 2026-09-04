# Module — Tests, Git et traçabilité des décisions

## 1. Stratégie de tests

### Socle imposé (C#)

| Rôle | Bibliothèque | Notes |
|---|---|---|
| Framework de test | **xUnit** | v3 sur tout nouveau projet ; v2 conservé sur l'existant tant qu'aucune migration n'est décidée |
| Assertions | **AwesomeAssertions** | fork Apache-2.0 de FluentAssertions v7, API identique |
| Données de test | **Bogus** | génération de jeux de données réalistes |
| Doublures | **Moq** | |

Ces choix ne se réauditent pas. **Interdictions associées** :

- Ne jamais installer **FluentAssertions ≥ 8** : la licence est devenue commerciale (Xceed), incompatible avec la politique de `dependances.md`. Si le paquet apparaît dans une solution reprise, le signaler et proposer la substitution par AwesomeAssertions — l'API étant la même, la migration se limite au remplacement du paquet.
- Pas de second framework d'assertions ou de mock en parallèle (NSubstitute, FakeItEasy, Shouldly) dans une même solution.

Frontend : **Vitest** (choix par défaut d'Angular CLI 21, confirmé via `ng new --test-runner=vitest`).
Pas de Playwright dans cette première version : aucun parcours n'est encore jugé assez critique
pour justifier l'E2E sur un projet personnel à un seul contributeur (voir CLAUDE.md §12, « Hors
périmètre »).

### Usage du socle

**Assertions (AwesomeAssertions)**
- Style fluide **exclusivement** : `result.Should().Be(...)`. Ne pas mélanger avec `Assert.Equal` dans le même projet.
- Utiliser les assertions spécialisées plutôt qu'un booléen : `.Should().BeEquivalentTo(...)`, `.Should().ThrowAsync<T>().WithMessage(...)`, `.Should().ContainSingle(x => ...)`. `Should().BeTrue()` sur une expression complexe produit un message d'échec inexploitable.
- `because` renseigné quand l'intention n'est pas évidente à la lecture de l'assertion.
- `AssertionScope` pour regrouper plusieurs assertions liées et obtenir tous les échecs d'un coup, plutôt que le premier seulement.

**Données de test (Bogus)**
- Les `Faker<T>` sont regroupés dans des **builders réutilisables** (`CustomerFaker`, `OrderFaker`) dans un projet de test partagé, pas recopiés dans chaque fichier.
- **Graine fixée** (`Randomizer.Seed = new Random(<n>)`) pour que les échecs soient reproductibles. Un test qui ne passe qu'une fois sur dix est un test cassé, pas un test aléatoire utile.
- Bogus sert à remplir le bruit, pas la donnée signifiante : la valeur sur laquelle porte l'assertion est **toujours** fixée explicitement dans le test. Ne jamais asserter sur une valeur générée.
- Locale `fr_BE` pour les données de type adresse/nom quand le format compte.

**Doublures (Moq)**
- Ne simuler que ce qu'on possède : une dépendance externe (SDK, client HTTP tiers) est d'abord encapsulée derrière une interface du projet, puis c'est cette interface qui est simulée.
- Pas de mock sur un POCO, un DTO, un record ou un objet valeur : instancier le vrai objet, c'est plus court et plus fiable.
- `MockBehavior.Strict` par défaut sur les collaborateurs critiques : un appel non prévu doit échouer plutôt que renvoyer silencieusement `default`.
- `Verify` réservé aux interactions qui **sont** le comportement attendu (un e-mail est envoyé, un événement est publié). Vérifier chaque appel d'un mock revient à tester l'implémentation et rend le refactoring impossible.
- Un test qui exige plus de trois mocks signale généralement une classe qui en fait trop : le dire plutôt que d'empiler les `Setup`.
- Préférer une implémentation en mémoire (fake) à un mock pour les abstractions très sollicitées (dépôt, horloge, cache) — plus lisible et réutilisable.

- **Pyramide** : beaucoup de tests unitaires rapides, quelques tests d'intégration sur les chemins réels, peu de tests E2E sur les parcours critiques.
- Nommage : `MethodName_Scenario_ExpectedResult` (anglais). Structure Arrange / Act / Assert. **Un comportement par test.**
- Tester le comportement observable, pas l'implémentation : un refactoring interne ne doit pas casser les tests.
- Pas de logique conditionnelle ni de boucle dans un test ; utiliser des tests paramétrés (`[Theory]`).
- Pas de `Thread.Sleep` ni de dépendance à l'horloge réelle : injecter `TimeProvider`. Pas de test dépendant de l'ordre d'exécution ni du réseau.
- Couverture minimale sur le code neuf : pas d'objectif chiffré ; priorité au mapping PRONOTE → modèle stable (`ScheduleMapper`), à la génération ICS (`ScheduleIcsBuilder`) et à la validation (`ScheduleIcsQueryValidator`) — déjà couverts. La couverture est un indicateur, pas un but : 100 % de couverture sur des assertions faibles ne vaut rien.
- **Toute correction de bug est accompagnée d'un test de non-régression** qui échoue avant le correctif et passe après.

### Intégration
- `WebApplicationFactory<Program>` pour les tests ASP.NET Core (voir `ProgramTests` dans
  `UMonsPlanning.Backend.Tests`). Pas de base de données dans ce projet : rien à provisionner.
- Tester les frontières réelles atteignables sans réseau externe : sérialisation, routage,
  démarrage du graphe de dépendances (`ValidateOnStart`). **Ne jamais** faire dépendre un test
  automatisé du serveur PRONOTE réel (interdiction générale ci-dessous, « aucun test... dépendant
  du réseau ») — la vérification contre le serveur réel se fait manuellement via
  `tools/UMonsPlanning.Cli` après une modification du protocole (CLAUDE.md §12).

### Front et E2E
- Composants : tests de rendu et d'interaction sur les composants porteurs de logique ; pas de test trivial sur un composant purement présentationnel.
- E2E Playwright : non mis en place (voir remarque plus haut). Parcours qui le justifierait le cas échéant : génération du lien de calendrier, prévisualisation via « Tester votre calendrier ».
- Contrôle d'accessibilité automatisé (`axe-core`) intégré aux tests E2E sur les pages clés.
- Sélecteurs stables (`data-testid` ou rôles accessibles), jamais de sélecteur CSS structurel fragile.

### Interdits
- Affaiblir, ignorer (`[Skip]`, `.skip`) ou supprimer un test pour rendre un build vert. Corriger le code, ou expliquer pourquoi le test est faux.
- Annoncer un code « testé » sans avoir exécuté les tests.

## 2. Git

- Deux branches longues : `main` (production, `umonsplanning.pellichero.be`) et `develop`
  (test/intégration, `test.umonsplanning.pellichero.be`) — voir
  `docs/adr/0010-cicd-github-actions-ftps-deploy.md`. Toutes deux protégées sur GitHub (PR + revue
  obligatoires, statuts CI à jour, pas de force-push ni de suppression).
- Branches de travail : `feature/<slug>`, `fix/<slug>`, `chore/<slug>`, ouvertes depuis `develop`
  et fusionnées dans `develop` par PR — pas de système de ticket sur ce projet personnel. `develop`
  se fusionne dans `main` par PR pour chaque mise en production.
- **Conventional Commits en anglais**, à l'impératif : `feat:`, `fix:`, `refactor:`, `perf:`, `docs:`, `test:`, `build:`, `ci:`, `chore:`. Portée optionnelle : `feat(catalog): add product filtering`.
- Un commit = un changement logique cohérent. Pas de `wip`, pas de `fix stuff`, pas de commit fourre-tout mêlant fonctionnalité, reformatage et montée de version.
- Les changements de rupture sont signalés (`!` ou `BREAKING CHANGE:`) et documentés.
- **Ne jamais mentionner l'assistance IA** dans un message de commit, une description de PR ou un commentaire de code.
- Ne jamais réécrire l'historique d'une branche partagée ; pas de `push --force` (au mieux `--force-with-lease`, sur une branche personnelle, après accord).
- Fichiers générés, artefacts de build, secrets et dossiers de publication exclus par `.gitignore`.

## 3. Pull requests

- Description : le **quoi** et surtout le **pourquoi**, les changements de rupture, les étapes manuelles de déploiement, les captures avant/après pour l'UI, les chiffres avant/après pour la performance.
- Auto-relecture avant demande de relecture : diff relu ligne à ligne, code de débogage retiré.
- CI verte obligatoire : build, tests, linters, analyse de vulnérabilités.
- Dépôt public depuis `docs/adr/0010-cicd-github-actions-ftps-deploy.md` : la protection de branche
  GitHub impose une revue approuvée sur toute PR vers `main`/`develop` — ce n'est plus une
  recommandation. Garder les PR sous ~400 lignes modifiées, sinon découper.

## 4. Décisions d'architecture (ADR)

Un ADR est rédigé dans `docs/adr/NNNN-titre-court.md` pour :

- l'ajout, le remplacement ou la suppression d'une dépendance ;
- un choix de persistance, de cache, d'authentification ou d'hébergement ;
- l'introduction d'une nouvelle couche, d'un nouveau module ou d'un découpage ;
- tout écart assumé par rapport aux règles de `CLAUDE.md`.

Format : contexte · options envisagées · décision · conséquences (y compris négatives) · date · statut (proposé / accepté / remplacé).

## Checklist du module

- [ ] Tests écrits, exécutés et passants ; test de non-régression pour tout bug corrigé.
- [ ] Socle respecté : xUnit + AwesomeAssertions + Bogus + Moq, sans framework concurrent ni FluentAssertions ≥ 8.
- [ ] Assertions fluides uniquement, graine Bogus fixée, aucune assertion sur une valeur générée.
- [ ] Aucun test ignoré ou affaibli pour faire passer le build.
- [ ] Tests d'intégration sur base jetable, migrations testées.
- [ ] Parcours critiques couverts en E2E, contrôle axe inclus.
- [ ] Commits conventionnels en anglais, un changement logique par commit.
- [ ] Aucune mention d'assistance IA dans l'historique.
- [ ] PR décrivant le pourquoi, CI verte.
- [ ] ADR rédigé pour toute décision structurante ou tout écart aux règles.
