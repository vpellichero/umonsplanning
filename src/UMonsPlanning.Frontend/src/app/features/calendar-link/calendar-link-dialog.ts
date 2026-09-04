import { HttpClient } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  afterNextRender,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { CatalogService } from '../../core/catalog.service';
import type { PreviewEvent } from '../../core/models';
import { parseIcsToEvents } from './ics-parser';
import { SchedulePreviewDialog } from './schedule-preview-dialog';

@Component({
  selector: 'app-calendar-link-dialog',
  imports: [SchedulePreviewDialog],
  templateUrl: './calendar-link-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalendarLinkDialog {
  protected readonly catalog = inject(CatalogService);
  private readonly http = inject(HttpClient);

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');
  private readonly preview = viewChild.required(SchedulePreviewDialog);

  private readonly origin = signal('');

  protected readonly formationId = signal<string | null>(null);
  protected readonly sectionId = signal<string | null>(null);
  protected readonly startDate = signal<string>('');
  protected readonly endDate = signal<string>('');

  /** false = one event per course (default), true = one event per day. */
  protected readonly perDayLayout = signal(false);

  private readonly layoutHelpButton = viewChild<ElementRef<HTMLButtonElement>>('layoutHelpButton');

  /**
   * Explicitly Angular-controlled tooltip state (rather than the native Popover API, which only
   * triggers on click and gave no reliable hover behavior on desktop) — shown on hover/focus for
   * desktop and toggled on click/tap for mobile. Positioned as `fixed` from the button's
   * bounding rect rather than `absolute` in normal flow: the dialog's form wrapper scrolls
   * (`overflow-auto`), which clipped/hid an in-flow tooltip instead of floating it above the modal.
   */
  protected readonly layoutHelpVisible = signal(false);
  protected readonly layoutHelpPosition = signal<{ top: number; left: number } | null>(null);

  protected readonly testing = signal(false);
  protected readonly testError = signal<string | null>(null);
  protected readonly linkCopied = signal(false);

  protected readonly previewWeekLabel = signal('');
  protected readonly previewEvents = signal<readonly PreviewEvent[]>([]);

  protected readonly generatedUrl = computed(() => {
    const origin = this.origin();
    const params = this.buildBaseParams();
    if (!origin || !params) {
      return '';
    }

    const start = this.startDate();
    if (start) {
      params.set('start', start);
    }

    const end = this.endDate();
    if (end) {
      params.set('end', end);
    }

    return `${origin}/api/schedule.ics?${params.toString()}`;
  });

  constructor() {
    afterNextRender(() => this.origin.set(window.location.origin));
  }

  open(): void {
    this.dialog().nativeElement.showModal();
  }

  close(): void {
    this.dialog().nativeElement.close();
  }

  onFormationChange(formationId: string): void {
    this.formationId.set(formationId || null);
    this.sectionId.set(null);
    this.catalog.selectFormation(formationId || null);
  }

  /** Opens the native date picker — the calendar button only ever calls this. */
  openDatePicker(input: HTMLInputElement): void {
    input.showPicker?.();
    input.focus();
  }

  showLayoutHelp(): void {
    const button = this.layoutHelpButton()?.nativeElement;
    if (!button) {
      return;
    }

    const rect = button.getBoundingClientRect();
    const tooltipWidth = 256;
    this.layoutHelpPosition.set({
      top: rect.bottom + 8,
      left: Math.max(8, Math.min(rect.left, window.innerWidth - tooltipWidth - 8)),
    });
    this.layoutHelpVisible.set(true);
  }

  hideLayoutHelp(): void {
    this.layoutHelpVisible.set(false);
  }

  toggleLayoutHelp(): void {
    if (this.layoutHelpVisible()) {
      this.hideLayoutHelp();
    } else {
      this.showLayoutHelp();
    }
  }

  async copyLink(): Promise<void> {
    const url = this.generatedUrl();
    if (!url) {
      return;
    }

    await navigator.clipboard.writeText(url);
    this.linkCopied.set(true);
    setTimeout(() => this.linkCopied.set(false), 2000);
  }

  async testCalendar(): Promise<void> {
    const origin = this.origin();
    const params = this.buildBaseParams();
    if (!origin || !params) {
      return;
    }

    this.testing.set(true);
    this.testError.set(null);

    try {
      // Built independently from generatedUrl(): the backend rejects combining "week" with
      // "start"/"end" (only one selection mode at a time), while generatedUrl() carries
      // start/end as soon as a period is set.
      const week = await this.resolveTestWeek();
      params.set('week', String(week));

      const icsText = await firstValueFrom(
        this.http.get(`${origin}/api/schedule.ics?${params.toString()}`, { responseType: 'text' }),
      );

      this.previewEvents.set(parseIcsToEvents(icsText));
      this.previewWeekLabel.set(`semaine ${week}`);
      this.preview().open();
    } catch {
      this.testError.set(
        "Impossible de récupérer ce calendrier pour le moment. Réessayez dans un instant.",
      );
    } finally {
      this.testing.set(false);
    }
  }

  private buildBaseParams(): URLSearchParams | null {
    const formationId = this.formationId();
    if (!formationId) {
      return null;
    }

    const params = new URLSearchParams({ formation: formationId });
    const sectionId = this.sectionId();
    if (sectionId) {
      params.set('section', sectionId);
    }

    if (this.perDayLayout()) {
      params.set('layout', 'PerDay');
    }

    return params;
  }

  /**
   * Week to preview: the first week of the chosen period if a period date (start and/or end) is
   * set, otherwise the current week. Start alone starts there ; end alone starts from the first
   * available week (same logic as the backend, see `ScheduleEndpoints.ResolveWeeksAsync`).
   */
  private async resolveTestWeek(): Promise<number> {
    const start = this.startDate();
    if (start) {
      return this.weekNumberFor(start);
    }

    if (this.endDate()) {
      return 1;
    }

    const today = new Date().toISOString().slice(0, 10);
    return this.weekNumberFor(today);
  }

  private async weekNumberFor(date: string): Promise<number> {
    const { week } = await firstValueFrom(
      this.http.get<{ date: string; week: number }>(`/api/weeks/by-date/${date}`),
    );
    return week;
  }
}
