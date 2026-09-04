import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { httpResource } from '@angular/common/http';
import type { Resource } from './models';

/**
 * Lists for the two dropdowns, populated from the backend API (itself backed by a file cache
 * server-side — see CLAUDE.md §12). The second dropdown only loads once the first one is set:
 * `httpResource` makes no request while its URL is `undefined`.
 *
 * Both resources only fire in the browser: at prerender time (build) no backend is reachable,
 * and these lists are only ever needed once the user opens the dialog anyway — always after
 * hydration, so always client-side.
 */
@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly selectedFormationId = signal<string | null>(null);

  readonly formations = httpResource<Resource[]>(
    () => (this.isBrowser ? '/api/formations' : undefined),
    { defaultValue: [] },
  );

  readonly sections = httpResource<Resource[]>(
    () => {
      const formationId = this.selectedFormationId();
      return this.isBrowser && formationId
        ? `/api/formations/${encodeURIComponent(formationId)}/sections`
        : undefined;
    },
    { defaultValue: [] },
  );

  selectFormation(formationId: string | null): void {
    this.selectedFormationId.set(formationId);
  }
}
