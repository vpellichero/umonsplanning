/** Pure JSON-LD (schema.org) builders — no DOM access, so the shape of each schema can be unit
 * tested in isolation from how/when it gets injected into the page (see SeoMetaService). */

export interface BreadcrumbItem {
  readonly name: string;
  readonly url: string;
}

export function buildBreadcrumbJsonLd(items: readonly BreadcrumbItem[]): object {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: items.map((item, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: item.name,
      item: item.url,
    })),
  };
}

export interface HowToStepInput {
  readonly name: string;
  readonly text: string;
}

export function buildHowToJsonLd(options: {
  readonly name: string;
  readonly description: string;
  readonly steps: readonly HowToStepInput[];
}): object {
  return {
    '@context': 'https://schema.org',
    '@type': 'HowTo',
    name: options.name,
    description: options.description,
    step: options.steps.map((step) => ({
      '@type': 'HowToStep',
      name: step.name,
      text: step.text,
    })),
  };
}

export interface FaqEntryInput {
  readonly question: string;
  readonly answer: string;
}

export function buildFaqJsonLd(entries: readonly FaqEntryInput[]): object {
  return {
    '@context': 'https://schema.org',
    '@type': 'FAQPage',
    mainEntity: entries.map((entry) => ({
      '@type': 'Question',
      name: entry.question,
      acceptedAnswer: {
        '@type': 'Answer',
        text: entry.answer,
      },
    })),
  };
}
