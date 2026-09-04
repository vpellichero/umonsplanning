# Module — Dépendances : audit de l'existant, licences, maintenance

Règle générale : **ne pas réinventer, mais ne pas non plus intégrer n'importe quoi.**

## 1. Auditer avant de développer

Avant d'implémenter toute fonctionnalité non triviale — analyse/parsing, planification, génération PDF ou Office, graphiques, éditeur riche, traitement d'images, flux d'authentification, import/export, mapping objet, validation, géolocalisation, paiement, recherche, cache distribué, envoi d'e-mails, etc. — **auditer d'abord ce qui existe** (NuGet, npm, GitHub).

Livrer une synthèse de **2 à 3 candidats crédibles**, avec pour chacun :

- licence exacte (et licences transitives) ;
- date de la dernière version publiée et rythme des versions ;
- santé du dépôt : issues ouvertes/fermées, PR en attente, réactivité des mainteneurs, nombre de mainteneurs actifs ;
- volume de téléchargements et tendance ;
- compatibilité avec la version .NET / Angular épinglée ;
- poids et dépendances transitives (impact sur le bundle pour le front) ;
- existence de vulnérabilités connues ;
- coût réel d'une implémentation maison en regard.

## 2. Décision : maison ou bibliothèque — toujours demander

**Ne jamais trancher seul.** Présenter l'arbitrage puis poser explicitement la question :

> « Préférez-vous une implémentation from scratch, ou l'utilisation de <bibliothèque> ? »

Éléments d'arbitrage à exposer :

| Implémentation maison | Bibliothèque |
|---|---|
| Contrôle total, zéro dépendance, périmètre exactement adapté | Temps gagné, cas limites déjà traités, éprouvé en production |
| Coût de développement **et de maintenance** à long terme | Dépendance à un tiers, risque d'abandon, montées de version subies |
| Pas de risque de licence | Vérification de licence obligatoire |
| Pertinent si le besoin est simple, spécifique ou central au métier | Pertinent si le besoin est standard, complexe et bien résolu ailleurs |

Fournir une recommandation argumentée, mais la décision revient au mainteneur.

## 3. Licences — le code produit est propriétaire

**Les projets sont privés et fermés : aucune licence publique n'est publiée, et aucune dépendance ne peut imposer la divulgation du code.**

**Autorisé** (usage commercial propriétaire sans contrainte de divulgation) :
MIT · Apache-2.0 · BSD-2-Clause · BSD-3-Clause · ISC · MS-PL · Unlicense · CC0 · Zlib · BSL-1.0.

**Interdit** :
GPL (toutes versions) · AGPL · SSPL · CC BY-NC / CC BY-SA · BUSL / Elastic License / « source-available » à restriction commerciale · toute licence sans fichier de licence identifiable · code copié depuis un blog, Stack Overflow ou un dépôt sans licence explicite.

**Sur accord préalable uniquement** :
LGPL (liaison dynamique, bibliothèque non modifiée) · MPL-2.0 (les fichiers modifiés doivent être repartagés) · licences commerciales payantes (coût, portée et durée à valider avec le client).

Obligations pratiques :

- Vérifier aussi les licences **transitives**, pas seulement celle du paquet de premier niveau.
- Maintenir un fichier `THIRD-PARTY-NOTICES.md` listant chaque dépendance, sa version et sa licence, avec les mentions d'attribution requises (Apache-2.0 et BSD l'exigent).
- Une police, une icône, une image ou un template de démonstration est aussi soumis à licence. Vérifier avant intégration.
- En cas de doute sur une licence : **s'arrêter et demander**, ne pas intégrer « en attendant ».

## 4. Socle imposé — audit déjà tranché

Les bibliothèques suivantes sont retenues pour l'ensemble des projets. Elles ne se réauditent pas, ne se remplacent pas et ne se doublent pas d'un équivalent concurrent dans une même solution :

| Besoin | Bibliothèque | Licence |
|---|---|---|
| Validation | FluentValidation | Apache-2.0 |
| Mapping | Mapperly (`Riok.Mapperly`) | Apache-2.0 |
| GraphQL | HotChocolate | MIT |
| Test | xUnit | Apache-2.0 |
| Assertions | AwesomeAssertions | Apache-2.0 |
| Données de test | Bogus | MIT |
| Doublures | Moq | BSD-3-Clause |

Point de vigilance : **FluentAssertions ≥ 8 est interdit** (licence commerciale Xceed). AwesomeAssertions en est le fork libre à API identique. Si le paquet apparaît dans une solution reprise, le signaler et proposer la substitution.

L'obligation d'audit de la §1 reste entière pour **tout autre** besoin.

## 5. Seuils de maintenance

Une dépendance est rejetée si l'un de ces critères est vrai :

- **Aucune version publiée depuis plus de 18 mois** (sauf bibliothèque manifestement terminée, très stable et largement utilisée — à justifier explicitement).
- Mainteneur unique et inactif, ou dépôt archivé.
- Vulnérabilité connue non corrigée.
- Pas de compatibilité annoncée avec la version majeure de .NET / Angular utilisée.
- Dépendances transitives disproportionnées par rapport au service rendu.
- Aucune documentation ni test dans le dépôt.

## 6. Hygiène des dépendances

- **Une dépendance ne s'ajoute jamais « au passage »** dans un changement plus large. C'est une décision à part entière, consignée dans un ADR (`docs/adr/NNNN-*.md`).
- Versions épinglées (`Directory.Packages.props` pour NuGet, lockfile commité pour npm/pnpm). Pas de plage de versions flottante.
- Analyse de vulnérabilités en CI : `dotnet list package --vulnerable --include-transitive`, `pnpm audit`, Dependabot ou équivalent.
- Montées de version traitées séparément des fonctionnalités, avec les tests comme filet.
- Supprimer les dépendances devenues inutiles ; ne pas laisser de paquet orphelin dans les manifestes.
- Préférer une petite dépendance ciblée à un framework entier importé pour une seule fonction, mais préférer une dépendance solide à trois micro-paquets peu maintenus.

## Checklist du module

- [ ] Audit de l'existant réalisé avant tout développement non trivial.
- [ ] 2–3 candidats présentés avec licence, maintenance, poids et compatibilité.
- [ ] Question « from scratch ou bibliothèque ? » explicitement posée.
- [ ] Licence compatible avec un usage propriétaire fermé, transitives comprises.
- [ ] Dernière version publiée il y a moins de 18 mois.
- [ ] Aucune vulnérabilité connue.
- [ ] Version épinglée, lockfile commité, ADR rédigé, `THIRD-PARTY-NOTICES.md` mis à jour.
