# 0010 — CI/CD : GitHub Actions, modèle main/develop, déploiement FTPS

- Statut : accepté
- Date : 2026-09-04

## Contexte

Le projet passe d'un dépôt local à un dépôt GitHub public avec deux environnements réels :
`test.umonsplanning.pellichero.be` et `umonsplanning.pellichero.be`, tous deux sur le même
hébergement mutualisé Windows/IIS accessible uniquement par FTP+TLS (pas de SSH, pas de Web
Deploy). Le mainteneur a demandé une CI qui compile/teste à chaque branche, un déploiement
automatique par branche (`develop` → test, `main` → production), des PR protégées par revue
obligatoire sur ces deux branches, et que toute information de déploiement passe par des secrets
GitHub plutôt que des variables.

## Décision

**Plateforme** : GitHub Actions — déjà le système d'hébergement du dépôt, aucune dépendance
externe supplémentaire à mettre en place.

**Modèle de branches** : `main` (production) et `develop` (test/intégration) sont les deux
branches longues ; `feature/*`/`fix/*`/`chore/*` en partent et y retournent par PR (voir
`docs/ai/tests-git.md` §2, mis à jour). Les deux branches sont protégées : PR obligatoire, au
moins une revue approuvée, statuts CI (`build-backend`, `build-frontend`) obligatoires et à jour,
force-push et suppression interdits. Les administrateurs ne sont **pas** inclus dans cette
restriction — GitHub interdit à un auteur d'approuver sa propre PR, ce qui bloquerait le
mainteneur seul sur ses propres PR si l'inclusion était stricte.

**Environnements et secrets** : deux environnements GitHub, `test` et `production`, chacun avec sa
propre politique de branche de déploiement (`test` uniquement depuis `develop`, `production`
uniquement depuis `main`) — une garantie structurelle contre un déploiement croisé, indépendante du
code du workflow. Chaque environnement porte ses propres secrets `FTP_HOST`, `FTP_USERNAME`,
`FTP_PASSWORD` (jamais de variable de dépôt en clair, conformément à la demande explicite). Le
dossier distant étant `/` pour les deux comptes FTP, `server-dir: /` reste un littéral dans le
workflow plutôt qu'un secret dupliqué ; il n'y a rien à protéger dans cette valeur commune.

**Action de déploiement FTPS** : `SamKirkland/FTP-Deploy-Action`, épinglée au commit SHA du tag
`v4.4.0` (`110f9186c050f71550953127052e77650219c287`) plutôt qu'au tag flottant — pratique standard
pour une action tierce (risque de chaîne d'approvisionnement, un tag peut être redéplacé). Supporte
`protocol: ftps` (FTP+TLS explicite, ce qui était demandé) nativement. Les actions officielles
GitHub (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`) restent épinglées à leur
tag de version majeure, pratique courante pour du premier parti.

**Bascule de maintenance (`app_offline.htm`)** : `lftp`, installé à la volée sur le runner
(`apt-get install lftp`), pour les deux commandes de renommage FTP avant/après la synchronisation
principale — pas une dépendance persistante du projet, un outil ponctuel comme `sharp`/`png-to-ico`
pour les favicons (`src/UMonsPlanning.Frontend/README.md`). Le script vérifie d'abord si
`app_offline.htm` existe déjà côté serveur (dans ce cas, ne pas y toucher : une maintenance déjà
active manuellement ne doit pas être levée par un déploiement automatique) ; sinon il renomme
`_app_offline.htm` → `app_offline.htm` avant l'envoi et le renomme en sens inverse une fois
terminé. `app_offline.htm` est explicitement exclu de la liste `exclude` de l'action de
déploiement : par défaut, cette action **supprime** du serveur tout fichier absent du dossier
publié localement (comportement miroir) — sans cette exclusion, la synchronisation aurait
elle-même supprimé `app_offline.htm` en cours de déploiement, mettant fin à la maintenance avant la
fin de l'envoi.

**CI** : un seul workflow (`.github/workflows/ci-cd.yml`). `build-backend` et `build-frontend`
tournent sur tout `push` (n'importe quelle branche) et toute `pull_request` vers `main`/`develop` —
c'est ce qui satisfait « compiler à chaque branche ». `deploy-test`/`deploy-prod` ne tournent que
sur un `push` (pas une PR) respectivement sur `develop`/`main`, après succès des deux jobs de
build. Un groupe de concurrence (`ci-${{ github.ref }}`, `cancel-in-progress: true`) annule les
exécutions redondantes sur une même branche.

## Conséquences

- Un déploiement republie systématiquement tout (pas de déploiement incrémental) : simple et
  suffisant pour le volume de ce projet ; à revoir seulement si les temps de build deviennent
  gênants.
- Le compte GitHub du mainteneur doit disposer du scope OAuth `workflow` pour que `gh`/git puisse
  pousser des fichiers sous `.github/workflows/` — absent par défaut, à ajouter explicitement
  (`gh auth refresh -s workflow`).
- Aucun mot de passe FTP n'est connu ou fabriqué par l'assistant : les comptes FTP
  (`deploy-umonsplanning-test`, `deploy-umonsplanning`) et leurs mots de passe sont créés et
  déclarés comme secrets GitHub par le mainteneur lui-même.
- `docs/ai/tests-git.md` §2/§3 mis à jour : modèle de branches réel, et la règle « pas de revue de
  PR formelle » (vraie tant que le dépôt était privé à un seul contributeur) est remplacée par la
  protection de branche désormais active sur un dépôt public.
