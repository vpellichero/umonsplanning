# Module — Performance web : Core Web Vitals, images, polices, favicons

Les budgets et seuils de ce module font partie de la Definition of Done. Un dépassement est un défaut, pas un compromis acceptable.

## 1. Objectifs Core Web Vitals

Mesurés au **75e percentile, en données terrain, profil mobile** :

| Métrique | Seuil | Commentaire |
|---|---|---|
| LCP | ≤ 2,5 s (viser 2,0 s sur un site vitrine) | Élément LCP identifié et optimisé explicitement |
| INP | ≤ 200 ms | Généralement dégradé par les scripts tiers |
| CLS | ≤ 0,1 | Presque toujours dû à des images ou polices sans réservation d'espace |
| TTFB | ≤ 800 ms | Cache serveur, requêtes SQL, démarrage à froid |
| FCP | ≤ 1,8 s | |

## 2. Budgets de poids — deux profils

Choisir le profil applicable et le déclarer dans `CLAUDE.md` §12. Les budgets sont appliqués par l'outillage (budgets `angular.json`, `size-limit`, `bundlesize`) et **font échouer le build**, pas seulement avertir.

### Profil A — Site public / vitrine / e-commerce (rendu serveur)

| Poste | Budget (gzip/brotli) |
|---|---|
| JS initial | **≤ 100 Ko** (idéal < 50 Ko) |
| CSS initial | **≤ 30 Ko** |
| Police(s) au chargement initial | ≤ 60 Ko (2 fichiers WOFF2 maximum) |
| Poids total du premier écran | ≤ 1 Mo, images comprises |
| Requêtes au premier rendu | ≤ 30 |
| Scripts tiers | ≤ 2, chacun justifié et différé |

### Profil B — Application métier / back-office (SPA Angular)

| Poste | Budget (gzip/brotli) |
|---|---|
| Bundle initial (main + polyfills + styles) | **≤ 250 Ko** (≈ 700 Ko brut) |
| Chunk paresseux par fonctionnalité | ≤ 100 Ko |
| CSS global | ≤ 50 Ko |
| Styles d'un composant unique | ≤ 6 Ko brut |
| Poids total du premier écran | ≤ 1,5 Mo |

Configuration Angular correspondante (profil B) :

```json
"budgets": [
  { "type": "initial", "maximumWarning": "600kb", "maximumError": "800kb" },
  { "type": "anyComponentStyle", "maximumWarning": "4kb", "maximumError": "6kb" },
  { "type": "bundle", "name": "lazy", "maximumError": "300kb" }
]
```

Tout dépassement se traite en réduisant le code ou la dépendance responsable, **jamais** en relevant le budget sans accord explicite.

## 3. Images — pipeline obligatoire

Toute image publiée respecte l'ensemble de ces règles :

- **Format** : WebP en sortie par défaut, AVIF en plus lorsque l'outillage le permet, format d'origine en repli via `<picture>`. SVG pour les icônes et logos, minifié (SVGO). Jamais de PNG pour une photographie, jamais de JPEG pour un aplat.
- **`srcset` + `sizes`** pour chaque breakpoint utile. Jamais un seul fichier surdimensionné servi à tous les écrans. Non applicable pour l'instant : la page unique actuelle ne contient aucune image (texte, dropdowns, modales). Largeurs de référence à définir si des images sont ajoutées.
- **Dimensions explicites** : attributs `width` et `height` (ou `aspect-ratio` en CSS) sur **chaque** image. C'est la première cause de CLS.
- **Chargement différé** : `loading="lazy"` + `decoding="async"` partout…
- **…sauf les images « hero » et tout élément visible au premier écran** : `loading="eager"`, `fetchpriority="high"`, et `<link rel="preload" as="image" imagesrcset="..." imagesizes="...">` dans le `<head>`. Une image hero en `lazy` est un défaut LCP caractérisé.
- **Compression** : non applicable pour l'instant (aucune image dans la page actuelle) ; WebP q80 par défaut le jour où une image est ajoutée. Métadonnées EXIF supprimées dans tous les cas.
- **Aucune image décorative lourde** en CSS `background-image` sur le premier écran : elle n'est ni préchargeable ni prioritisable proprement.
- Angular : utiliser `NgOptimizedImage` (`ngSrc`, `priority`, `width`/`height` obligatoires) — il applique une partie de ces règles et avertit sur les erreurs classiques.
- OrchardCore : passer par le pipeline média et les profils d'image (redimensionnement à la volée + cache), pas par des variantes pré-générées commitées dans le dépôt.
- Vérifier le rendu sur écran à haute densité (2x) sans servir du 2x à tout le monde.

## 4. Polices

- Auto-hébergées, format **WOFF2** uniquement, sous-ensemble aux glyphes réellement utilisés.
- `font-display: swap`, `<link rel="preload" as="font" type="font/woff2" crossorigin>` pour la fonte principale seulement.
- Fallback système avec métriques ajustées (`size-adjust`, `ascent-override`) pour éviter le décalage à la substitution.
- Deux familles maximum, quatre graisses maximum. Pas de CDN de polices tiers (performance + RGPD).

## 5. JavaScript, CSS et réseau

- CSS critique du premier écran inline ; le reste chargé sans bloquer le rendu.
- JS non critique en `defer` / `type="module"` ; aucun script bloquant dans le `<head>`.
- Découpage en chunks par route ; élimination du code mort vérifiée (tree-shaking effectif, imports nommés).
- `content-visibility: auto` + `contain-intrinsic-size` sur les longues pages.
- `preconnect` / `dns-prefetch` uniquement pour les origines tierces réellement nécessaires (maximum 3).
- **Chaque script tiers doit être justifié** : ils sont la cause habituelle d'un mauvais INP. Chargement différé, après consentement, et jamais synchrone.
- Découper les tâches longues (> 50 ms) ; éviter le layout thrashing (lecture/écriture DOM alternées).
- Compression Brotli, HTTP/2 ou HTTP/3, `Cache-Control` immuable sur les assets versionnés par hash, ETag sur le reste.
- Espace réservé pour tout contenu injecté après coup (bandeaux, iframes, publicités, widgets) — rien ne s'insère au-dessus d'un contenu déjà affiché.

## 6. Favicons — génération automatique depuis le logo

Un logo existe (`src/UMonsPlanning.Frontend/public/icon.webp`, `logo.webp`, `logo-horizontal.webp`,
voir la note du `README.md` racine sur leur origine) : l'ensemble des favicons ci-dessous est
**généré automatiquement** depuis `icon.webp` par
`src/UMonsPlanning.Frontend/scripts/generate-icons.mjs` (`sharp` + `png-to-ico`, installés à la
demande via `npm install --no-save sharp png-to-ico` — volontairement absents de `package.json`,
voir le README du frontend), plutôt que fabriqués un par un à la main.

Sorties dans `src/UMonsPlanning.Frontend/public/` :

| Fichier | Détail |
|---|---|
| `favicon.ico` | multi-résolutions 16 / 32 / 48 |
| `apple-touch-icon.png` | 180 × 180 |
| `icon-192.png`, `icon-512.png` | manifeste PWA |
| `icon-maskable-512.png` | maskable, zone de sécurité ≥ 20 % |
| `site.webmanifest` | `name`, `short_name`, `icons`, `theme_color`, `background_color`, `display`, `start_url` |
| `og-image.png` | aperçu Open Graph / Twitter Card, 1200 × 630 |

Pas de `favicon.svg` : la source (`icon.webp`) est une image matricielle générée par IA, pas un
tracé vectoriel — un vecteur fabriqué à la main à partir d'un raster serait une fabrication, pas
une génération automatique.

Règles :

- Le script est à relancer manuellement si le logo change ; il n'est pas exécuté par `npm run build`.
- Balises correspondantes ajoutées dans `src/index.html` (favicons, `apple-touch-icon`, manifeste,
  `<meta name="theme-color">`, balises Open Graph/Twitter).
- Vérifier le rendu réel en onglet clair et sombre, et sur l'écran d'accueil mobile.

## 7. Mesure

- Mesurer **avant et après** toute intervention de performance, en profil mobile bridé, et rapporter les chiffres. Aucune amélioration ne s'annonce sans mesure.
- Laboratoire : Lighthouse (mobile), WebPageTest. Terrain : CrUX / RUM.
- Identifier explicitement l'élément LCP de chaque page clé et justifier son optimisation.
- Vérifier le CLS en interaction réelle (chargement lent, images tardives), pas seulement au chargement idéal.

## Checklist du module

- [ ] Profil de budget déclaré et respecté ; build en échec au dépassement.
- [ ] Élément LCP identifié, préchargé, `fetchpriority="high"`.
- [ ] Images : WebP/AVIF, `srcset` + `sizes`, `width`/`height`, lazy sauf hero.
- [ ] Polices auto-hébergées, WOFF2, sous-ensemblées, `font-display: swap`, préchargées.
- [ ] Aucun script bloquant ; scripts tiers justifiés et différés.
- [ ] Espace réservé pour tout contenu à chargement tardif.
- [ ] Favicons régénérés par script si le logo a changé, manifeste et balises à jour.
- [ ] LCP / INP / CLS mesurés et rapportés, avant/après.
