# Module — Sécurité et RGPD

Base : OWASP Top 10. Aucun de ces points n'est optionnel, y compris sur un projet interne.

## 1. Entrées, sorties, injections

- Valider **et** normaliser toute entrée à la frontière (API, formulaire, webhook, fichier importé, paramètre d'URL). La validation client ne remplace jamais la validation serveur.
- Requêtes paramétrées exclusivement. Aucune concaténation SQL, aucune interpolation dans une requête, y compris pour un nom de colonne (utiliser une liste blanche).
- Encodage systématique en sortie ; HTML utilisateur assaini par une bibliothèque dédiée (côté OrchardCore : `IHtmlSanitizerService`), jamais par une expression régulière.
- Uploads : non applicable — aucun endpoint n'accepte de fichier envoyé par un client.
- Désérialisation : jamais de type polymorphe non contraint depuis une entrée externe.
- SSRF : toute URL fournie par l'utilisateur et appelée par le serveur passe par une liste blanche de domaines.

## 2. Authentification et autorisation

**Non applicable à `UMonsPlanning.Backend`** : aucun compte utilisateur, aucune authentification.
Toutes les routes `/api/*` sont en lecture seule et exposent des données déjà publiques sur
l'espace invité PRONOTE de l'UMONS (CLAUDE.md §12). Pas de mot de passe, pas de session applicative,
pas de contrôle d'accès par ressource à implémenter. Si une fonctionnalité nécessitant un compte
apparaît un jour (favoris synchronisés, etc.), revenir compléter cette section avant de l'écrire.

## 3. Secrets et configuration sensible

Aucun secret dans le dépôt : ni chaîne de connexion, ni clé d'API, ni certificat, ni mot de passe, ni jeton — pas dans `appsettings.*.json`, pas dans un test, pas dans un commentaire, pas dans l'historique Git. `dotnet user-secrets` n'est utilisé sur aucun projet.

Le mécanisme dépend du contexte, mais la **source de vérité est toujours `.env.example`**.

### `.env.example` — référence unique, versionnée

Seul fichier de configuration d'environnement commité. Il décrit **toutes** les variables attendues par l'application, avec pour chacune un commentaire d'usage, son caractère obligatoire ou optionnel, et une valeur factice ou vide — jamais une valeur réelle.

Toute variable lue par le code y est ajoutée **dans le même commit**. Une variable utilisée mais absente de `.env.example` est un défaut de livraison : c'est ce fichier qui indique quoi déclarer, aussi bien en local que dans les secrets de la CI.

### Développement et tests — DotNetEnv

- Fichier `.env.development`, local à la machine, **jamais commité**.
- Chargement via **DotNetEnv** au tout début de `Program.cs`, avant la construction de la configuration.
- Le chargement est **conditionnel à l'existence du fichier** : en production il n'y en a pas, et l'absence ne doit provoquer aucune erreur.
- Les variables d'environnement déjà définies par l'hôte l'emportent sur le fichier (ne pas activer l'écrasement).

`.gitignore` — la négation est indispensable, sinon `.env.example` est ignoré avec le reste :

```gitignore
.env
.env.*
!.env.example
```

### Déploiement — cible retenue : mutualisé Plesk/IIS

**A. Hébergement mutualisé Plesk/IIS — injection dans `web.config` par la CI/CD**

- **Aucun fichier `.env` n'est déposé sur le serveur.** Les valeurs proviennent des secrets GitHub et sont injectées par le pipeline dans le bloc `<environmentVariables>` du module ASP.NET Core :

  ```xml
  <aspNetCore processPath="dotnet" arguments=".\UMonsPlanning.Backend.dll" hostingModel="inprocess">
    <environmentVariables>
      <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      <environmentVariable name="ConnectionStrings__Default" value="__INJECTED__" />
    </environmentVariables>
  </aspNetCore>
  ```

- Le `web.config` **versionné** est un gabarit contenant uniquement des jetons de substitution, jamais de valeur réelle. La substitution a lieu à l'**étape de déploiement**, pas à l'étape de build : un artefact de build contenant des secrets serait conservé et téléchargeable dans l'historique des exécutions GitHub.
- Chaque secret est déclaré comme secret GitHub (jamais comme variable de dépôt en clair) et le pipeline ne les journalise pas — vérifier que le masquage fonctionne réellement sur les valeurs multi-lignes ou encodées.
- L'ensemble des variables de `.env.example` doit avoir un secret correspondant dans le dépôt GitHub. Un écart entre les deux se manifeste par un échec au démarrage après déploiement — d'où l'intérêt de la validation ci-dessous.
- Avantage de cette approche par rapport à un `.env` déposé : IIS refuse par défaut de servir les fichiers `.config`, alors qu'un `.env` à la racine du répertoire publié est servi en clair.
- Points de vigilance propres à cette cible : les sauvegardes Plesk et les copies `web.config.bak` laissées par un déploiement contiennent les secrets en clair ; une rotation de secret impose un redéploiement ; l'accès au panneau Plesk équivaut à l'accès aux secrets.

**B. Conteneur, VPS ou hébergement dédié**

- Variables d'environnement fournies par l'orchestrateur ou le gestionnaire de services, ou fichier `.env` déposé **hors du répertoire publié**, avec permissions restreintes au compte applicatif.
- Si un `.env` se trouve malgré tout dans le répertoire web, ajouter une règle de refus explicite et vérifier par une requête réelle que `https://<domaine>/.env` renvoie 404 ou 403 :

  ```xml
  <security>
    <requestFiltering>
      <hiddenSegments><add segment=".env" /></hiddenSegments>
      <fileExtensions><add fileExtension=".env" allowed="false" /></fileExtensions>
    </requestFiltering>
  </security>
  ```

### Règles communes

- Nommage des clés en convention .NET, double tiret bas pour les sections imbriquées (`ConnectionStrings__Default`, `Smtp__Password`), lisible nativement par le fournisseur de configuration d'environnement et compatible avec le pattern Options.
- **Échec au démarrage** si une variable obligatoire manque ou est vide, via `ValidateDataAnnotations().ValidateOnStart()`. C'est le filet qui rattrape un secret oublié dans la CI, avant que l'erreur ne se manifeste sur une page en production.
- Ne jamais journaliser, sérialiser ni exposer les valeurs chargées, y compris dans un endpoint de diagnostic, une page d'erreur détaillée ou un message d'exception.
- Aucun secret dans un artefact de CI, une archive de déploiement ou une sauvegarde non chiffrée.

### En cas de fuite

Si un secret est découvert commité, présent dans l'historique Git, exposé dans un log de pipeline ou accessible publiquement : **s'arrêter, le signaler immédiatement**, le considérer comme compromis et procéder à sa rotation. Le supprimer du dépôt ne suffit pas — il reste dans l'historique et dans tous les clones.

## 4. Transport et en-têtes

- HTTPS partout, redirection HTTP → HTTPS, HSTS avec `preload` en production.
- Content-Security-Policy stricte, **sans `unsafe-inline`** (nonces ou hachages pour les scripts et styles inline). Signaler explicitement si une bibliothèque tierce impose d'assouplir la CSP.
- `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy` restrictive, `Cross-Origin-Opener-Policy`.
- CORS : liste explicite d'origines. Jamais `AllowAnyOrigin` combiné à `AllowCredentials`.
- Cookies : `Secure`, `HttpOnly`, `SameSite=Lax` ou `Strict` ; jamais de donnée sensible dans un cookie.
- Antiforgery sur toute requête modifiant l'état.
- Limitation de débit sur les endpoints publics : non implémentée pour l'instant (trafic personnel
  attendu très faible) — à ajouter (`Microsoft.AspNetCore.RateLimiting`, natif) si l'API devient
  plus visible, en particulier pour protéger la session PRONOTE partagée d'un usage excessif.
  Protection anti-bot : non applicable, aucun formulaire d'écriture public.

## 5. Dépendances et supply chain

- Analyse de vulnérabilités en CI (voir `dependances.md`). Aucun paquet vulnérable connu ne part en production.
- Scripts tiers chargés depuis un CDN : `integrity` + `crossorigin`, ou auto-hébergement (préférable, y compris pour le RGPD).
- Aucun paquet installé depuis une source non officielle.

## 6. Journalisation et gestion des erreurs

- Journaliser les événements de sécurité : échecs d'authentification, refus d'autorisation, modifications de privilèges, exports de données.
- **Jamais** de secret, jeton, mot de passe, numéro complet de carte ou donnée personnelle non nécessaire dans les logs.
- Aucune trace technique (stack trace, requête SQL, chemin serveur, version de framework) renvoyée au client. Pages d'erreur génériques en production.

## 7. RGPD (UE / Belgique)

**Très allégé pour ce projet** (CLAUDE.md §12) : `UMonsPlanning` ne collecte, ne stocke ni ne traite
aucune donnée personnelle de ses utilisateurs. Les seules données manipulées sont les horaires de
cours publics de l'UMONS (formation, salle, matière, groupe — pas de nom d'étudiant ni
d'enseignant sur l'espace invité utilisé). Aucun compte, aucun formulaire de collecte, aucun
analytique, aucun cookie non essentiel, aucun traceur tiers.

Ce qui reste applicable si la situation changeait (ajout d'un compte, d'un analytique, d'un
formulaire de contact) : minimisation, base légale documentée, registre des traitements, droits
des personnes, bandeau de consentement bloquant réellement les scripts avant accord — revenir
compléter cette section avant d'introduire l'une de ces fonctionnalités plutôt que de l'ajouter
« au passage ».

## Checklist du module

- [ ] Validation serveur systématique, requêtes paramétrées, sorties encodées.
- [ ] Autorisation vérifiée côté serveur, y compris au niveau de l'objet.
- [ ] Aucun secret dans le dépôt, les logs ou un artefact de CI ; `.env*` ignorés sauf `.env.example`.
- [ ] `.env.example` exhaustif, mis à jour dans le même commit que la variable ajoutée.
- [ ] Chaque variable de `.env.example` a son secret GitHub correspondant (cible mutualisée).
- [ ] `web.config` versionné sans valeur réelle ; substitution au déploiement, pas au build.
- [ ] Démarrage en échec si une variable obligatoire manque.
- [ ] En-têtes de sécurité et CSP en place, cookies correctement attribués.
- [ ] Antiforgery et limitation de débit sur les endpoints exposés.
- [ ] Aucune dépendance vulnérable connue.
- [ ] Erreurs génériques côté client, détails uniquement dans les logs.
- [ ] Aucun cookie ni script non essentiel avant consentement.
- [ ] Données collectées minimisées, durée de conservation définie, mentions légales à jour.
