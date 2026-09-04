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

**Synchronisation FTPS : `lftp`, pas une action tierce** — premier choix testé, écarté après
échec réel. `SamKirkland/FTP-Deploy-Action` (épinglée au commit SHA du tag `v4.4.0`) a été essayée
en premier : elle plante systématiquement dès la première opération sur ce serveur mutualisé
(`Error: Client is closed because read ECONNRESET (data socket)`, échec immédiat à la création du
premier dossier, avant tout transfert). `lftp`, lui, avait déjà négocié avec succès le canal de
données du même serveur pour les vérifications `ls` de la bascule de maintenance (paragraphe
suivant) — preuve concrète que le client Node de cette action gère mal une particularité TLS/canal
de données de cet hébergeur, que `lftp` (bien plus ancien, bien plus éprouvé en interopérabilité
FTP/FTPS) gère correctement. `lftp` est donc utilisé pour **toute** l'interaction FTP de ce
workflow — la bascule de maintenance ET la synchronisation elle-même
(`mirror --reverse --delete --no-perms --exclude-glob app_offline.htm publish/ /`), avec
`mirror:parallel-transfer-count 1` fixé explicitement (pas de suppositions sur le nombre de
connexions simultanées qu'un hébergement mutualisé tolère). `--no-perms` a lui aussi été ajouté
après un échec réel : le premier essai transférait tous les fichiers avec succès puis échouait à la
toute fin sur `chmod: Operation not supported: MFF and SITE CHMOD are not supported by this site`
— attendu sur un serveur FTP Windows/IIS, qui n'a pas de bits de permission Unix à faire
correspondre. Installé à la volée sur le runner
(`apt-get install lftp`), pas une dépendance persistante du projet — un outil ponctuel comme
`sharp`/`png-to-ico` pour les favicons (`src/UMonsPlanning.Frontend/README.md`). Les actions
officielles GitHub (`actions/checkout`, `actions/setup-dotnet`, `actions/setup-node`) restent
épinglées à leur tag de version majeure, pratique courante pour du premier parti.

**Bascule de maintenance (`app_offline.htm`)** : le script vérifie d'abord si `app_offline.htm`
existe déjà côté serveur (dans ce cas, ne pas y toucher : une maintenance déjà active manuellement
ne doit pas être levée par un déploiement automatique) ; sinon il renomme `_app_offline.htm` →
`app_offline.htm` avant l'envoi et le renomme en sens inverse une fois terminé.
`app_offline.htm` est explicitement exclu (`--exclude-glob`) de la synchronisation `mirror` : par
défaut, `--delete` supprime du serveur tout fichier absent du dossier publié localement — sans
cette exclusion, la synchronisation aurait elle-même supprimé `app_offline.htm` en cours de
déploiement, mettant fin à la maintenance avant la fin de l'envoi.

Second cas limite constaté au tout premier déploiement réel : sur un serveur encore vide,
`_app_offline.htm` n'existe pas encore côté FTP (rien n'y a jamais été envoyé), donc le renommer
échoue (`550`). La bascule vérifie maintenant l'existence de `_app_offline.htm` avant de tenter le
renommage ; s'il est absent (premier déploiement, ou fichier supprimé manuellement), le job continue
sans bascule ni restauration — la synchronisation qui suit le dépose de toute façon pour la
prochaine fois.

**Vérification du certificat TLS assouplie, constatée en production, pas préventive** : le premier
déploiement réel a échoué avec `Certificate verification: certificate common name doesn't match
requested host name` — le certificat TLS du serveur mutualisé ne correspond pas au nom d'hôte
`ftp.pellichero.be` (cas courant sur de l'hébergement mutualisé, où le certificat est souvent
partagé entre plusieurs domaines clients). `set ssl:verify-certificate no` (lftp) désactive la
vérification du nom d'hôte tout en gardant le chiffrement TLS actif — protège contre l'écoute
passive, pas contre une usurpation active de serveur. Accepté comme contrainte réelle de cet
hébergement plutôt que contourné en silence ; à retirer si l'hébergeur régularise un jour son
certificat.

**CI** : un seul workflow (`.github/workflows/ci-cd.yml`). `build-backend` et `build-frontend`
tournent sur tout `push` (n'importe quelle branche) et toute `pull_request` vers `main`/`develop` —
c'est ce qui satisfait « compiler à chaque branche ». `deploy-test`/`deploy-prod` ne tournent que
sur un `push` (pas une PR) respectivement sur `develop`/`main`, après succès des deux jobs de
build. Chaque job a son propre groupe de concurrence plutôt qu'un seul au niveau du workflow —
constaté en conditions réelles : un groupe unique au niveau du workflow (`cancel-in-progress:
true`) a annulé un run `deploy-prod` en cours **pendant qu'il avait déjà commencé son
déploiement**, un vrai risque de laisser le site bloqué en maintenance. `build-backend`/
`build-frontend` gardent `cancel-in-progress: true` (rapides, sans effet de bord) ; `deploy-test`/
`deploy-prod` ont chacun un groupe dédié sans annulation — un nouveau push se met en file derrière
un déploiement en cours plutôt que de l'interrompre. `timeout-minutes` explicite sur chaque job
(10 min pour les builds, 15 pour les déploiements) pour ne jamais dépendre du délai par défaut de
GitHub Actions (6 h) si quelque chose venait à bloquer.

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
