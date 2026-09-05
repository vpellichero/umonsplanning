import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { StatsService } from '../../core/stats.service';

/**
 * Vanity counter of calendar links generated, animated (count-up) whenever the value it reads
 * from {@link StatsService} changes. Stays at 0 during prerender (SSG) and until the browser's
 * first fetch resolves — see `StatsService.calendarLinksGenerated`.
 */
@Component({
  selector: 'app-calendar-link-counter',
  templateUrl: './calendar-link-counter.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarLinkCounter {
  private readonly stats = inject(StatsService);

  protected readonly displayedCount = signal(0);

  constructor() {
    effect(() => this.animateTo(this.stats.calendarLinksGenerated.value().count));
  }

  private animateTo(target: number): void {
    const start = this.displayedCount();
    if (target === start) {
      return;
    }

    const durationMs = 1200;
    const startTime = performance.now();

    const step = (now: number): void => {
      const progress = Math.min(1, (now - startTime) / durationMs);
      const eased = 1 - (1 - progress) ** 3;
      this.displayedCount.set(Math.round(start + (target - start) * eased));

      if (progress < 1) {
        requestAnimationFrame(step);
      }
    };

    requestAnimationFrame(step);
  }
}
