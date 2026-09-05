import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Routes, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { SeoMetaService } from './seo-meta.service';

@Component({ selector: 'app-stub-a', template: 'A' })
class StubPageA {}

@Component({ selector: 'app-stub-b', template: 'B' })
class StubPageB {}

const routes: Routes = [
  {
    path: '',
    component: StubPageA,
    title: 'Home title',
    data: { description: 'Home description', jsonLd: [{ '@type': 'WebSite' }] },
  },
  {
    path: 'other',
    component: StubPageB,
    title: 'Other title',
    data: { description: 'Other description', noIndex: true, jsonLd: [{ '@type': 'HowTo' }] },
  },
];

describe('SeoMetaService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter(routes)],
      teardown: { destroyAfterEach: true },
    });
  });

  it('should set the description, canonical link and Open Graph tags for the active route', async () => {
    const service = TestBed.inject(SeoMetaService);
    service.start();
    const harness = await RouterTestingHarness.create('/');
    harness.detectChanges();

    expect(document.title).toBe('Home title');
    expect(document.querySelector('meta[name="description"]')?.getAttribute('content')).toBe('Home description');
    expect(document.querySelector('link[rel="canonical"]')?.getAttribute('href')).toBe(
      'https://umonsplanning.pellichero.be/',
    );
    expect(document.querySelector('meta[property="og:title"]')?.getAttribute('content')).toBe('Home title');
    expect(document.querySelector('meta[name="robots"]')).toBeNull();

    const script = document.querySelector('script[data-route-json-ld]');
    expect(script?.getAttribute('type')).toBe('application/ld+json');
    expect(JSON.parse(script?.textContent ?? 'null')).toEqual({ '@type': 'WebSite' });
  });

  it('should update the canonical link, noindex and structured data when navigating to a different route', async () => {
    const service = TestBed.inject(SeoMetaService);
    service.start();
    const harness = await RouterTestingHarness.create('/');
    harness.detectChanges();

    await harness.navigateByUrl('/other');
    harness.detectChanges();

    expect(document.querySelector('link[rel="canonical"]')?.getAttribute('href')).toBe(
      'https://umonsplanning.pellichero.be/other',
    );
    expect(document.querySelector('meta[name="robots"]')?.getAttribute('content')).toBe('noindex');

    const scripts = document.querySelectorAll('script[data-route-json-ld]');
    expect(scripts.length).toBe(1);
    expect(JSON.parse(scripts[0].textContent ?? 'null')).toEqual({ '@type': 'HowTo' });
  });
});
