# 0018 — LOT 4 : nettoyage et garde-fou SEO

- Statut : accepté
- Date : 2026-09-05

## Contexte

Dernier lot du brief SEO d'origine, purement accessibilité/propreté — aucune nouvelle page ni
donnée structurée. Quatre points restaient ouverts après le LOT 3 (`docs/adr/0017`) :

1. `<meta name="keywords">` dans `index.html`, obsolète (les moteurs de recherche l'ignorent depuis
   longtemps) et redondant avec le contenu visible désormais riche (accueil, 6 pages de
   destination).
2. Contraste du pied de page : `<footer>` posait `text-slate-500` sur un fond `bg-slate-50`
   (`body`, `styles.css`), soit un ratio d'environ 4,55:1 — au-dessus du seuil AA (4,5:1) mais sans
   marge, et sensible au moindre changement de teinte.
3. `site.webmanifest` incomplet : ni `lang`, ni `id`, ni `scope`, ni `categories`.
4. Aucun garde-fou automatisé contre la régression la plus probable de tout cet effort SEO : un
   `data` de route copié-collé qui réutiliserait le titre/la description d'une autre route, un
   canonical qui ne s'auto-référence plus, ou un `<h1>` dupliqué/absent sur une page prérendue.

Un cinquième point du brief d'origine — bandeau de consentement de cookies à retirer ou remplacer
par une mesure respectueuse de la vie privée — ne s'applique pas : recherche faite dans
`src/app` (`consentement`, `cookie`) et aucun composant de bandeau de consentement n'existe dans ce
dépôt. Les seules occurrences du mot « cookie » sont des mentions explicatives dans le contenu
(`home-faq.ts`, `hyperplanning-umons.ts`) qui affirment l'absence de cookie non essentiel — cohérent
avec `CLAUDE.md` §12 (pas de compte, pas de cookie non essentiel, pas de traceur).

## Décision

**Meta keywords retiré** de `src/index.html`, sans remplacement.

**Contraste du pied de page corrigé par une seule classe** : `text-slate-500` → `text-slate-600`
sur le `<footer>` lui-même (`app.html`), qui cascade à la fois sur le texte et sur les liens
(seul le `hover:text-brand-600` restait déjà différent). Ratio `text-slate-600` sur `bg-slate-50` :
environ 7,25:1 — marge confortable au-dessus du seuil AA. Les autres usages de `text-slate-500`
dans le reste de l'application (fil d'ariane, date de mise à jour d'une page de destination, texte
de grande taille du compteur d'accueil) n'ont pas été touchés : ils n'étaient pas identifiés comme
un point du brief, et pour le compteur, `text-xl` place le texte dans la catégorie « texte large »
du WCAG (seuil 3:1, déjà respecté par `text-slate-500`).

**`site.webmanifest` complété** : `lang: "fr-BE"` (cohérent avec `index.html`), `id: "/"` et
`scope: "/"` (application installable limitée au même périmètre que `start_url`, pas de sous-arbre
séparé), `categories: ["education", "productivity", "utilities"]` (liste non normative du
[W3C Manifest wiki](https://github.com/w3c/manifest/wiki/Categories), utilisée telle quelle par les
navigateurs/stores qui l'exploitent).

**Garde-fou automatisé plutôt qu'un test Vitest** : la vérification porte sur le HTML réellement
prérendu (`dist/.../browser/<route>/index.html`), qui n'existe qu'après `ng build` — un test Vitest
ne peut pas dépendre de l'ordre d'exécution avec le build. Choix cohérent avec le mécanisme déjà en
place pour `sitemap.xml` (LOT 0) : un script Node exécuté en `postbuild`
(`scripts/verify-seo-invariants.mjs`), qui lit `prerendered-routes.json` pour connaître la liste des
routes (même source que `generate-sitemap.mjs`, aucune duplication de la liste des routes), puis
pour chaque route : extrait `<title>`, `<meta name="description">`, `<link rel="canonical">` et
compte les `<h1>` par une extraction de balises tolérante à l'ordre des attributs (nécessaire, les
balises générées par `Meta.updateTag`/le HTML statique n'ordonnent pas forcément les attributs de la
même façon). Échoue le build (`process.exit(1)`) si un titre ou une description n'est pas unique à
travers les routes, si un canonical ne s'auto-référence pas (`origin` dérivé du canonical de la
page d'accueil, comme `generate-sitemap.mjs`), ou si une route n'a pas exactement un `<h1>`.
`/404` est inclus dans la vérification (il a bien son propre titre/description) mais reste hors de
`sitemap.xml` (déjà exclu par `NOINDEX_ROUTES`, LOT 0) ; la route `**` (fallback client uniquement,
jamais prérendue en fichier statique propre) n'apparaît pas dans `prerendered-routes.json` et n'est
donc pas concernée par la vérification, malgré un `data` identique à `/404` dans `app.routes.ts` —
sans conflit puisqu'aucune des deux n'est un doublon de fichier réel.

## Conséquences

- Aucune nouvelle dépendance.
- `package.json` : `postbuild` devient
  `node scripts/generate-sitemap.mjs && node scripts/verify-seo-invariants.mjs` — toute
  configuration de build (`production`, `production,staging`, `development,staging`) bénéficie
  du garde-fou, comme c'était déjà le cas pour le sitemap.
- Le brief SEO d'origine (LOT 0 à 4) est désormais entièrement traité. Hors périmètre défini par le
  brief lui-même (§6/§7, hors de la responsabilité du code) : Search Console, Bing Webmaster Tools,
  backlinks, vérification des 382 formations/sections à la rentrée 2026-2027, migration de
  sous-domaine, pages par formation.
