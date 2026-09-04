import { HttpClient } from '@angular/common/http';
import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { httpResource } from '@angular/common/http';

/** Response shape of GET/POST /api/stats/calendar-links. */
interface CalendarLinkStats {
  readonly count: number;
}

/**
 * Vanity counter of calendar links generated, shown on the home page (see docs/adr/0012). Only
 * fetched in the browser: at prerender time (build) no backend is reachable, same reasoning as
 * CatalogService.
 */
@Injectable({ providedIn: 'root' })
export class StatsService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly http = inject(HttpClient);

  readonly calendarLinksGenerated = httpResource<CalendarLinkStats>(
    () => (this.isBrowser ? '/api/stats/calendar-links' : undefined),
    { defaultValue: { count: 0 } },
  );

  /**
   * Records that a calendar link was generated. Fire-and-forget: the copy-to-clipboard action it
   * follows has already succeeded, so a failure here must never surface to the user.
   */
  recordCalendarLinkGenerated(): void {
    if (!this.isBrowser) {
      return;
    }

    this.http.post('/api/stats/calendar-links', null).subscribe({ error: () => {} });
  }
}
