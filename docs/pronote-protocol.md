# Le protocole PRONOTE, rétro-ingénieré

`UMonsPlanning.Pronote` dialogue avec l'espace invité de **PRONOTE Campus** de l'Université de Mons
(`https://hplanning2026.umons.ac.be/invite`) — le même espace que la page web publique « Horaires
de cours », sans connexion requise. PRONOTE ne publie ni API ni documentation pour cet espace :
tout ce qui suit a été retrouvé en observant le trafic réseau du client JavaScript officiel et en
le reproduisant à l'identique. Ce document explique le protocole pour que le comportement du
backend reste compréhensible s'il faut le déboguer un jour, ou si PRONOTE change quelque chose.

## 1. Deux URL, et c'est tout

### 1.1 Ouverture de session

```
GET https://hplanning2026.umons.ac.be/invite?fd=1
```

> **`?fd=1` est obligatoire pour tout client non navigateur.** PRONOTE inspecte l'User-Agent
> **côté serveur** et renvoie sinon une page « La consultation de cette page avec le navigateur
> utilisé n'est pas garantie » — qui ne contient aucun numéro de session. `fd=1` est le paramètre
> derrière le lien « Accéder tout de même à… » de cette page.

La page renvoyée contient, en fin de document :

```js
Start({"a":2,"b":0,"c":"DIPLOME.EDT","i":8420612})
```

- `a` = genre d'espace (2 = invité) — c'est le premier segment de l'URL des appels ;
- `c` = onglet de démarrage ;
- **`i` = numéro de session**, obligatoire pour tous les appels suivants.

Chaque `GET /invite` crée une **nouvelle** session.

### 1.2 Appels de fonctions

```
POST https://hplanning2026.umons.ac.be/appelfonction/{espace}/{session}/{no}
Content-Type: application/json

{
  "session": 8420612,
  "no": "<identique au segment d'URL>",
  "id": "FonctionEmploiDuTemps",
  "dataSec": {
    "Signature": { "Onglet": "...", "listeRecherche": [ ... ] },
    "data": { ... }
  }
}
```

Réponse :

```json
{ "id": "...", "session": 8420612, "no": "...", "dataSec": { "data": { ... } } }
```

**Le corps n'est pas chiffré** sur ce serveur (option « skipCryptage » active). Seul le **numéro
d'ordre** `no` l'est.

Le serveur est strict sur les types JSON : `session` doit être un **nombre**, pas une chaîne —
sinon il répond `{"Erreur":{"G":8,"Titre":"La page a expiré ! (1)"}}`. Même chose pour `G` dans
`listeRecherche`.

### 1.3 Chiffrement du numéro d'ordre

Reproduction exacte de `ObjetCryptageAES.encrypter` du client officiel (node-forge) :

```
clé = MD5(cleAES)      où cleAES = ""            → d41d8cd98f00b204e9800998ecf8427e
IV  = MD5(ivAES)       si un IV de session existe
IV  = 16 octets nuls   pour le tout premier appel
no  = HEX( AES-128-CBC/PKCS7( "<numéro d'ordre décimal>" ) )
```

Le numéro d'ordre est un **compteur partagé** : le client envoie `n`, le serveur répond avec
`n+1`, le client repart de `n+2`. Les appels doivent donc être strictement séquentiels
(`PronoteSession` sérialise tout, et `PronoteClient` protège l'ensemble avec un `SemaphoreSlim`).

`ivAES` est **choisi par le client** : 16 octets aléatoires transmis dans `FonctionParametres`. Le
client JS officiel ne chiffre l'IV en RSA que si le site est servi en HTTP simple ; en HTTPS (cas
de l'UMONS) il envoie **`Uuid` = base64 brut de l'IV**. Pas de RSA à réimplémenter.

### 1.4 Séquence minimale

| # | Ordre | `id` | Rôle |
|---|---|---|---|
| 1 | 1 | `FonctionParametres` | négocie l'IV (`Uuid`), renvoie les paramètres de la grille |
| 2 | 3 | `DemandeParametreUtilisateur` | **ouvre les droits** — sans lui les appels suivants renvoient « Vos droits sont insuffisants » |
| 3 | 5 | `FonctionRenvoyerListeDeRessource` | liste des choix d'études (menu déroulant 1) |
| 4 | 7 | `FonctionListeDeTDEtOptionDuDiplome` | sous-choix (menu déroulant 2) |
| 5 | 9 | `FonctionDomaineDePresence` | semaines où la ressource a cours |
| 6 | 11 | `FonctionEmploiDuTemps` | cours d'une semaine |

> **Piège principal** : les identifiants de ressource `N`
> (`"50#YswGA4QqqYRwjjZ8oWnfb7dCFku9oXxjtKfglvQGx-k"`) sont **régénérés à chaque session**. Ils ne
> peuvent pas servir de clé publique : la bibliothèque calcule donc des *slugs* stables dérivés du
> libellé (`bab3-traduction-et-interpretation`) et résout `slug → N` dans sa propre session.

### 1.5 Placement des cours

`FonctionEmploiDuTemps` renvoie des cours positionnés sur une grille de créneaux :

```json
{ "N": "10#…", "p": 205, "d": 8, "co": "#CD004C", "dom": "[2..7,9..15]",
  "listeC": [
    { "G": 0,  "C": { "L": "T-ESPA-400 - C&I ESPA" } },
    { "G": 3,  "C": [ { "L": "NiDeVinci.318" } ] },
    { "G": 14, "C": [ { "L": "<.BAB3 - …>S3Gr2" } ] },
    { "G": 5,  "C": { "str": "A" } },
    { "G": 7,  "C": { "L": "Cours" } } ] }
```

- `p` = **place absolue** dans la semaine → `jour = p / PlacesParJour`, `place = p % PlacesParJour`
- `d` = durée en nombre de places
- `PlacesParJour = 68`, `PlacesParHeure = 4` → 1 place = **15 minutes**
- la place 0 d'une journée correspond à **08h00** (calibré sur l'affichage réel du serveur ;
  PRONOTE ne publie pas cette valeur, elle est configurable via `Pronote:DayStart`)
- `dom` = semaines de récurrence du cours
- genres de `listeC` : `0` matière, `2` personnels, `3` salles, `5` statut, `7` catégorie,
  `14` groupes. Les genres inconnus sont conservés dans `Additional`.

Numérotation des semaines : `PremierLundi` (07/09/2026) = semaine **1** ; semaine *n* =
`PremierLundi + (n-1) × 7 jours`.

## 2. Limites connues

- `DayStart = 08:00` est calibré empiriquement (le serveur renvoie `listeHeures: []`).
- Les libellés sont la seule clé stable ; un renommage côté UMONS change le slug.
- L'espace invité ne publie pas les enseignants : `teachers` est vide sur ce serveur, mais le
  mapping le gère si le genre `2` apparaît.
- Le protocole est non documenté : à surveiller lors des montées de version de PRONOTE
  (`parametreGeneral.Version` est exposé dans `/api/calendar`).

## 3. Valider l'API source dans Postman

1. Importer `postman/UMONS-Pronote.postman_collection.json`.
2. Régler les variables de collection `formationLabel`, `sectionLabel` (peut rester vide) et
   `week`.
3. Lancer les 7 requêtes **dans l'ordre** (Collection Runner, ou une par une de haut en bas).

Les scripts se chargent du numéro de session, de l'IV, du numéro d'ordre et de la reprise des
identifiants `N`. La console Postman affiche à la dernière requête l'emploi du temps décodé :

```
lundi 09h15-10h15  T-ALLE-401 - Langue ALLE  [NiDeVinci.313]
lundi 19h00-20h00  T-ESPA-402 - TAV/Int ESPA  [NiB5.204]
…
```

> Une session PRONOTE expire vite et le compteur d'ordre est partagé : si une requête renvoie
> « La page a expiré ! », relancer depuis la requête 1.

### Dépannage

**`{"Erreur":{"G":8,"Titre":"La page a expiré ! (1)"}}`** — le couple session / numéro d'ordre est
rejeté. Deux causes :

- le champ `session` du corps part **en chaîne** au lieu d'un nombre. Dans Postman il faut écrire
  `"session": {{session}}` **sans guillemets** ; `"session": "{{session}}"` est refusé par le
  serveur. Idem pour `G` dans `listeRecherche`.
- le compteur `ordre` n'est pas synchronisé avec le serveur : relancer depuis la requête 1.

**« La variable `{{no}}` n'est pas définie »** — il n'y a rien à y mettre. `no` est le numéro
d'ordre chiffré : le script de pré-requête de chaque appel le recalcule juste avant l'envoi et
l'écrit dans la variable de collection. Tant qu'aucune requête n'a tourné, elle est vide, et
Postman l'affiche comme non résolue dans l'aperçu de l'URL — c'est normal.

Il en va de même pour `session`, `sessionIvHex`, `uuid`, `ordre`, `formationN/G/L` et
`targetN/G/L`. Les seules variables à régler soi-même sont `formationLabel`, `sectionLabel`
(facultative) et `week`.

Si après envoi elle est toujours vide, c'est que le script de pré-requête a échoué : ouvrir
*View → Show Postman Console*, l'erreur y est affichée. Chaque appel y journalise aussi la ligne
`ordre N -> no = <hex>`.

**Repère de contrôle** : à la requête 2, `no` vaut toujours `3fa959b13967e0ef176069e01e23c8d7`
(numéro d'ordre 1 chiffré avec un IV nul — cette valeur ne dépend ni de la session ni de la date).
Si la console affiche autre chose, le compteur `ordre` n'est pas reparti de 1 : relancer la
requête 1.

**Requête 1 — page « navigateur non supporté »** : le paramètre `?fd=1` manque. PRONOTE filtre
l'User-Agent côté serveur ; sans `fd=1`, un client non navigateur reçoit une page d'avertissement
de ~4 ko sans numéro de session.

**Requête 1 — « Numéro de session introuvable »** (la console affiche alors statut, taille et
début du corps reçu). Dans l'ordre :

1. `?fd=1` est-il bien présent sur l'URL ?
2. La variable `baseUrl` est-elle résolue ? La collection doit être sélectionnée pour que ses
   variables s'appliquent — un `{{baseUrl}}` non résolu donne un 404 ou une erreur DNS.
3. *Settings → General → Automatically follow redirects* doit être **ON**.
4. L'en-tête `Accept-Encoding: gzip, deflate` doit bien partir : selon les versions, Postman
   annonce `br` mais décode mal le Brotli, et le corps arrive illisible.

Avec `fd=1`, le serveur répond sans cookie ni session préalable : une page de 2,6 ko contenant
`Start({…,"i":<session>})`.
