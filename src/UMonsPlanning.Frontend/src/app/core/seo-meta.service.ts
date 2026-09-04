import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta } from '@angular/platform-browser';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { environment } from '../../environments/environment';

interface SeoRouteData {
  readonly description?: string;
  readonly noIndex?: boolean;
}

/**
 * Keeps the canonical link and Open Graph/Twitter meta tags in sync with the active route.
 * Runs during prerendering too (the Router already fires NavigationEnd at that stage, which is
 * why the route `title` is already correct in the static output) — so these values are baked
 * into the HTML actually served, not only patched in after client-side hydration.
 */
@Injectable({ providedIn: 'root' })
export class SeoMetaService {
  private readonly document = inject(DOCUMENT);
  private readonly meta = inject(Meta);
  private readonly router = inject(Router);
  private readonly activatedRoute = inject(ActivatedRoute);

  start(): void {
    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
      this.applyMetaForActiveRoute();
    });
    this.applyMetaForActiveRoute();
  }

  private applyMetaForActiveRoute(): void {
    const leaf = this.leafRouteSnapshot();
    const data = leaf.data as SeoRouteData;
    const title = leaf.title ?? this.document.title;
    const description = data.description ?? '';
    const canonicalUrl = `${environment.baseUrl}${this.router.url}`;

    this.meta.updateTag({ name: 'description', content: description });
    this.meta.updateTag({ property: 'og:url', content: canonicalUrl });
    this.meta.updateTag({ property: 'og:title', content: title });
    this.meta.updateTag({ property: 'og:description', content: description });
    this.meta.updateTag({ name: 'twitter:title', content: title });
    this.meta.updateTag({ name: 'twitter:description', content: description });
    this.updateCanonicalLink(canonicalUrl);

    if (data.noIndex) {
      this.meta.updateTag({ name: 'robots', content: 'noindex' });
    } else {
      this.meta.removeTag('name="robots"');
    }
  }

  private updateCanonicalLink(href: string): void {
    let link = this.document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!link) {
      link = this.document.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.document.head.appendChild(link);
    }
    link.setAttribute('href', href);
  }

  private leafRouteSnapshot() {
    let route = this.activatedRoute;
    while (route.firstChild) {
      route = route.firstChild;
    }
    return route.snapshot;
  }
}
