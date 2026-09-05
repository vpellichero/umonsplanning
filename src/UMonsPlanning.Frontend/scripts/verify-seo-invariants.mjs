// Build-time regression guard (wired as part of "postbuild" in package.json, runs after every
// `ng build`): reads the actually prerendered HTML for every route and fails the build if the
// SEO baseline established across LOT 0-3 regresses — this is the single most likely regression
// of the whole SEO effort (a copy-pasted route `data` block silently reusing another route's
// title/description, a missing canonical, a duplicated <h1>).
import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

const DIST_DIR = new URL('../dist/UMonsPlanning.Frontend/', import.meta.url);
const distPath = (name) => fileURLToPath(new URL(name, DIST_DIR));

function routeToIndexPath(route) {
  return route === '/' ? 'browser/index.html' : `browser${route}/index.html`;
}

function attrValue(tag, attrName) {
  const match = tag.match(new RegExp(`${attrName}="([^"]*)"`));
  return match?.[1];
}

function findTags(html, tagPattern) {
  return [...html.matchAll(tagPattern)].map((match) => match[0]);
}

function extractTitle(html) {
  return html.match(/<title>([^<]*)<\/title>/)?.[1];
}

function extractDescription(html) {
  const metaTag = findTags(html, /<meta\b[^>]*>/g).find((tag) => attrValue(tag, 'name') === 'description');
  return metaTag ? attrValue(metaTag, 'content') : undefined;
}

function extractCanonical(html) {
  const linkTag = findTags(html, /<link\b[^>]*>/g).find((tag) => attrValue(tag, 'rel') === 'canonical');
  return linkTag ? attrValue(linkTag, 'href') : undefined;
}

function countH1(html) {
  return findTags(html, /<h1\b[^>]*>/g).length;
}

async function readOrigin() {
  const html = await readFile(distPath('browser/index.html'), 'utf-8');
  const canonical = extractCanonical(html);
  const match = canonical?.match(/^(https?:\/\/[^/]+)/);
  if (!match) {
    throw new Error('Could not derive the expected origin from the home page canonical link.');
  }
  return match[1];
}

async function readRoutes() {
  const manifest = JSON.parse(await readFile(distPath('prerendered-routes.json'), 'utf-8'));
  return Object.keys(manifest.routes);
}

const origin = await readOrigin();
const routes = await readRoutes();

const errors = [];
const titlesSeen = new Map();
const descriptionsSeen = new Map();

for (const route of routes) {
  const html = await readFile(distPath(routeToIndexPath(route)), 'utf-8');
  const title = extractTitle(html);
  const description = extractDescription(html);
  const canonical = extractCanonical(html);
  const h1Count = countH1(html);
  const expectedCanonical = route === '/' ? `${origin}/` : `${origin}${route}`;

  if (!title) {
    errors.push(`${route}: missing <title>.`);
  } else if (titlesSeen.has(title)) {
    errors.push(`${route}: <title> "${title}" is not unique (already used by ${titlesSeen.get(title)}).`);
  } else {
    titlesSeen.set(title, route);
  }

  if (!description) {
    errors.push(`${route}: missing meta description.`);
  } else if (descriptionsSeen.has(description)) {
    errors.push(
      `${route}: meta description is not unique (already used by ${descriptionsSeen.get(description)}).`,
    );
  } else {
    descriptionsSeen.set(description, route);
  }

  if (canonical !== expectedCanonical) {
    errors.push(`${route}: canonical is "${canonical}", expected self-referencing "${expectedCanonical}".`);
  }

  if (h1Count !== 1) {
    errors.push(`${route}: expected exactly one <h1>, found ${h1Count}.`);
  }
}

if (errors.length > 0) {
  console.error('SEO invariants violated:\n' + errors.map((error) => `  - ${error}`).join('\n'));
  process.exit(1);
}

console.log(`SEO invariants OK for ${routes.length} prerendered routes.`);
