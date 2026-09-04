// One-off maintenance script: regenerates every derived icon/favicon/OG-image asset from the
// source logo files in `public/`. Not a build-time step and not wired into `npm run build` — the
// outputs are committed like any other static asset, this script only exists so they can be
// reproduced if the source logos ever change (see docs/ai/performance.md §6).
//
// sharp/png-to-ico are intentionally NOT project dependencies (see CLAUDE.md §11 on the
// dependency-audit requirement for every new package) — install them ad hoc before running this:
//   npm install --no-save sharp png-to-ico
//   node scripts/generate-icons.mjs
import { readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import sharp from 'sharp';
import pngToIco from 'png-to-ico';

const PUBLIC_DIR = new URL('../public/', import.meta.url);
const publicPath = (name) => fileURLToPath(new URL(name, PUBLIC_DIR));

const BRAND_600 = '#b30f3a';

async function toPngBuffer(sourcePath, size) {
  return sharp(sourcePath).resize(size, size, { fit: 'cover' }).png().toBuffer();
}

async function generateFavicon() {
  const buffers = await Promise.all([16, 32, 48].map((size) => toPngBuffer(publicPath('icon.webp'), size)));
  const ico = await pngToIco(buffers);
  await writeFile(publicPath('favicon.ico'), ico);
}

async function generateAppleTouchIcon() {
  const buffer = await toPngBuffer(publicPath('icon.webp'), 180);
  await writeFile(publicPath('apple-touch-icon.png'), buffer);
}

async function generatePwaIcons() {
  for (const size of [192, 512]) {
    const buffer = await toPngBuffer(publicPath('icon.webp'), size);
    await writeFile(publicPath(`icon-${size}.png`), buffer);
  }
}

/** Maskable icon: the source artwork is composited onto a plain background with a 20% safe-zone
 * margin on every side, since OS launcher masks (circle, squircle...) crop right up to the edge. */
async function generateMaskableIcon() {
  const canvasSize = 512;
  const contentSize = Math.round(canvasSize * 0.6);
  const content = await sharp(publicPath('icon.webp')).resize(contentSize, contentSize, { fit: 'cover' }).toBuffer();

  const buffer = await sharp({
    create: { width: canvasSize, height: canvasSize, channels: 3, background: '#ffffff' },
  })
    .composite([{ input: content, gravity: 'center' }])
    .png()
    .toBuffer();

  await writeFile(publicPath('icon-maskable-512.png'), buffer);
}

async function generateManifest() {
  const manifest = {
    name: 'UMonsPlanning',
    short_name: 'UMonsPlanning',
    description: "Génère un lien de calendrier toujours à jour à partir de l'horaire PRONOTE de l'UMONS.",
    start_url: '/',
    display: 'standalone',
    background_color: '#ffffff',
    theme_color: BRAND_600,
    icons: [
      { src: '/icon-192.png', sizes: '192x192', type: 'image/png', purpose: 'any' },
      { src: '/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
      { src: '/icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
    ],
  };
  await writeFile(publicPath('site.webmanifest'), `${JSON.stringify(manifest, null, 2)}\n`);
}

/**
 * Display-sized derivatives for the two logos actually rendered in the app UI. `logo.webp` and
 * `logo-horizontal.webp` stay at their original resolution — they're also the source for the OG
 * image below, which needs more pixels than a small header/hero logo does. Sized at 2x the CSS
 * display box (docs/ai/performance.md §3 "vérifier le rendu à haute densité (2x)") since neither
 * usage varies by breakpoint — a single right-sized file, no `srcset` needed.
 */
async function generateDisplayLogos() {
  // Header logo: displayed at 220x32 (app.html) -> 2x = 440 wide.
  const header = await sharp(publicPath('logo-horizontal.webp')).resize({ width: 440 }).webp({ quality: 80 }).toBuffer();
  await writeFile(publicPath('logo-horizontal-header.webp'), header);
  const headerMeta = await sharp(header).metadata();
  console.log(`logo-horizontal-header.webp: ${headerMeta.width}x${headerMeta.height}`);

  // Home hero logo: displayed at 200x100 (home-page.html) -> 2x = 400 wide.
  const hero = await sharp(publicPath('logo.webp')).resize({ width: 400 }).webp({ quality: 80 }).toBuffer();
  await writeFile(publicPath('logo-hero.webp'), hero);
  const heroMeta = await sharp(hero).metadata();
  console.log(`logo-hero.webp: ${heroMeta.width}x${heroMeta.height}`);
}

/** Open Graph preview image (1200x630, PNG for maximum crawler compatibility): the horizontal
 * logo centered on a plain white canvas with generous padding. */
async function generateOgImage() {
  const canvasWidth = 1200;
  const canvasHeight = 630;
  const logo = await sharp(publicPath('logo-horizontal.webp'))
    .resize({ width: Math.round(canvasWidth * 0.7), fit: 'inside' })
    .toBuffer();

  const buffer = await sharp({
    create: { width: canvasWidth, height: canvasHeight, channels: 3, background: '#ffffff' },
  })
    .composite([{ input: logo, gravity: 'center' }])
    .png()
    .toBuffer();

  await writeFile(publicPath('og-image.png'), buffer);
}

/** Recompresses the source logos as lossy WebP q80: they were exported lossless (100-470 KB for
 * icons displayed at a few dozen pixels), which docs/ai/performance.md §3 forbids. */
async function recompressSourceLogos() {
  for (const name of ['logo.webp', 'logo-horizontal.webp', 'icon.webp']) {
    const original = await readFile(publicPath(name));
    const recompressed = await sharp(original).webp({ quality: 80 }).toBuffer();
    await writeFile(publicPath(name), recompressed);
  }
}

await recompressSourceLogos();
await generateFavicon();
await generateAppleTouchIcon();
await generatePwaIcons();
await generateMaskableIcon();
await generateManifest();
await generateDisplayLogos();
await generateOgImage();

console.log('Icons, favicons, manifest, display logos and OG image regenerated in public/.');
