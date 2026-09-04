# 0004 — Topologie d'hébergement mutualisé (IIS/Plesk), même origine, cache à la demande

- Statut : accepté
- Date : 2026-09-02

## Contexte

Projet personnel, hébergé sur un mutualisé Windows/IIS (type Plesk/LWS). Contraintes qui en
découlent, tranchées explicitement avec le mainteneur :

- **Pas de processus d'arrière-plan long-running fiable** : le pool d'application IIS recycle
  périodiquement et peut être arrêté après inactivité. Un `BackgroundService`/timer ne peut pas
  être le seul mécanisme de rafraîchissement.
- **Pas de Node.js** : un serveur SSR Angular (`server.ts`) ne peut pas tourner en continu.
- Le frontend doit générer une URL de calendrier qui utilise **le domaine actuel de la page**,
  sans configuration différente par environnement (localhost / test / prod).

## Décisions

### 1. Cache des dropdowns : rafraîchi à la demande, pas par minuterie

`FormationCatalogCache` (`UMonsPlanning.Backend`) vérifie l'âge du fichier de cache **à chaque
requête** (comparaison du mois calendaire, fuseau Europe/Bruxelles) plutôt que via un
`BackgroundService`. Le premier appel du mois qui trouve le cache périmé déclenche le
rafraîchissement (verrou par ressource, double vérification après acquisition) ; les hébergements
qui recyclent le pool d'application avant qu'un mois ne s'écoule ne perdent donc jamais le
rafraîchissement — contrairement à un timer qui ne se déclencherait jamais si le processus ne
tourne pas au bon moment.

### 2. Frontend : prérendu (SSG) plutôt que SSR live

Angular est construit avec le prérendu au build (`@angular/ssr` en mode statique), pas un serveur
Node en production : la page unique du site n'a pas de contenu dépendant de l'utilisateur au
premier rendu (le contenu dynamique — dropdowns, lien généré — est peuplé côté client après
hydratation). Le résultat est un ensemble de fichiers statiques servable par IIS sans runtime Node.
Voir `docs/ai/angular.md` §6 et le README du frontend pour le détail du build.

### 3. Même origine via sous-chemin, pas de sous-domaine

Le backend est publié comme application IIS sous `/api` du même site que le frontend statique,
plutôt que sur un sous-domaine séparé. Le frontend appelle des chemins relatifs (`/api/...`) et
construit l'URL de calendrier avec `window.location.origin` : aucune configuration d'URL d'API par
environnement, aucun CORS nécessaire en production. En développement (`ng serve` sur un port
différent de `dotnet run`), le proxy de développement Angular (`proxy.conf.json`) redirige `/api`
vers le backend local ; le `AllowAnyOrigin` CORS du backend couvre le cas où ce proxy n'est pas
utilisé.

## Conséquences

- Pas de `IHostedService`/`BackgroundService` dans `UMonsPlanning.Backend`.
- Le dossier de cache (`App_Data/catalog-cache` par défaut) doit être accessible en écriture par
  le pool d'application — à vérifier à la mise en production (piège connu, voir CLAUDE.md §12).
- Un changement de sujet de page nécessitant un vrai SSR par requête (données personnalisées au
  premier rendu) remettrait en cause la décision 2 et demanderait un hébergement avec Node.
