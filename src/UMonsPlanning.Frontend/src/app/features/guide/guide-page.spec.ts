import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import type { GuideContent } from './guide-content';
import { GuidePage } from './guide-page';

const SAMPLE_GUIDE: GuideContent = {
  slug: 'sample-guide',
  breadcrumbLabel: 'Sample',
  h1: 'Sample guide title',
  description: 'Sample description.',
  intro: ['Sample intro paragraph.'],
  steps: [
    { title: 'First step', body: 'Do the first thing.' },
    { title: 'Second step', body: 'Do the second thing.' },
  ],
  pitfallsTitle: 'Sample pitfalls',
  pitfalls: [{ title: 'Watch out', body: 'This can trip you up.' }],
  lastUpdatedDisplay: '5 septembre 2026',
  lastUpdatedIso: '2026-09-05',
};

describe('GuidePage', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuidePage],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { data: { guide: SAMPLE_GUIDE } } } },
      ],
    }).compileComponents();
  });

  it('should render the guide title, numbered steps and pitfalls', () => {
    const fixture = TestBed.createComponent(GuidePage);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('h1')?.textContent).toBe('Sample guide title');
    expect(compiled.querySelectorAll('#guide-steps > li').length).toBe(2);
    expect(compiled.textContent).toContain('This can trip you up.');
    expect(compiled.textContent).toContain('5 septembre 2026');
  });

  it('should not render a related-guides section when the guide has none', () => {
    const fixture = TestBed.createComponent(GuidePage);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).not.toContain('Guides par application');
  });
});
