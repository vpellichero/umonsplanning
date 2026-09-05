import { buildBreadcrumbJsonLd, buildFaqJsonLd, buildHowToJsonLd } from './structured-data-builders';

describe('structured-data-builders', () => {
  it('should build a BreadcrumbList with 1-based positions', () => {
    const schema = buildBreadcrumbJsonLd([
      { name: 'Accueil', url: 'https://example.test/' },
      { name: 'Aide', url: 'https://example.test/aide' },
    ]) as { '@type': string; itemListElement: { position: number; name: string }[] };

    expect(schema['@type']).toBe('BreadcrumbList');
    expect(schema.itemListElement).toEqual([
      { '@type': 'ListItem', position: 1, name: 'Accueil', item: 'https://example.test/' },
      { '@type': 'ListItem', position: 2, name: 'Aide', item: 'https://example.test/aide' },
    ]);
  });

  it('should build a HowTo with one HowToStep per step', () => {
    const schema = buildHowToJsonLd({
      name: 'Test',
      description: 'Description',
      steps: [{ name: 'Étape 1', text: 'Faites ceci.' }],
    }) as { '@type': string; step: { '@type': string; name: string; text: string }[] };

    expect(schema['@type']).toBe('HowTo');
    expect(schema.step).toEqual([{ '@type': 'HowToStep', name: 'Étape 1', text: 'Faites ceci.' }]);
  });

  it('should build a FAQPage with one Question per entry', () => {
    const schema = buildFaqJsonLd([{ question: 'Pourquoi ?', answer: 'Parce que.' }]) as {
      '@type': string;
      mainEntity: { name: string; acceptedAnswer: { text: string } }[];
    };

    expect(schema['@type']).toBe('FAQPage');
    expect(schema.mainEntity[0].name).toBe('Pourquoi ?');
    expect(schema.mainEntity[0].acceptedAnswer.text).toBe('Parce que.');
  });
});
