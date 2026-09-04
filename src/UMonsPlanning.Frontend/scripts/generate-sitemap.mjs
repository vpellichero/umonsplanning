// Build-time step (wired as "postbuild" in package.json, runs after every `npm run build`,
// including with extra `--configuration=...` flags): writes dist/browser/sitemap.xml from the
// routes Angular actually prerendered (dist/prerendered-routes.json), so the sitemap can never
// list a route that doesn't exist or isn't a real 200 in the build that produced it. The origin is
// read back from the already-generated index.html's canonical link rather than duplicated from
// environment.ts, so the sitemap stays correct for whichever configuration (production vs
// production,staging) was actually built.
import { readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

const DIST_DIR = new URL('../dist/UMonsPlanning.Frontend/', import.meta.url);
const distPath = (name) => fileURLToPath(new URL(name, DIST_DIR));

const NOINDEX_ROUTES = new Set(['/404']);

async function readOrigin() {
  const html = await readFile(distPath('browser/index.html'), 'utf-8');
  const match = html.match(/<link rel="canonical" href="(https?:\/\/[^/"]+)/);
  if (!match) {
    throw new Error('Could not find a canonical link in browser/index.html to derive the sitemap origin.');
  }
  return match[1];
}

async function readPrerenderedRoutes() {
  const manifest = JSON.parse(await readFile(distPath('prerendered-routes.json'), 'utf-8'));
  return Object.keys(manifest.routes).filter((route) => !NOINDEX_ROUTES.has(route));
}

function buildSitemap(origin, routes) {
  const lastmod = new Date().toISOString().slice(0, 10);
  const urls = routes
    .map((route) => {
      const loc = route === '/' ? `${origin}/` : `${origin}${route}`;
      return `  <url>\n    <loc>${loc}</loc>\n    <lastmod>${lastmod}</lastmod>\n    <changefreq>monthly</changefreq>\n  </url>`;
    })
    .join('\n');
  return `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls}\n</urlset>\n`;
}

const origin = await readOrigin();
const routes = await readPrerenderedRoutes();
await writeFile(distPath('browser/sitemap.xml'), buildSitemap(origin, routes));
