import { ChangeDetectionStrategy, Component, ElementRef, computed, input, signal, viewChild } from '@angular/core';
import type { PreviewEvent } from '../../core/models';
import { groupEventsByDay } from './schedule-days';

@Component({
  selector: 'app-schedule-preview-dialog',
  templateUrl: './schedule-preview-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SchedulePreviewDialog {
  readonly weekLabel = input<string>('');
  readonly events = input<readonly PreviewEvent[]>([]);

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  readonly days = computed(() => groupEventsByDay(this.events()));

  /**
   * Explicitly Angular-controlled accordion state (rather than native `<details>`/`<summary>`,
   * which some users reported as unresponsive) — a plain toggle button + `@if` is guaranteed to
   * work regardless of any browser/hydration quirk around native disclosure widgets.
   */
  protected readonly expandedUids = signal<ReadonlySet<string>>(new Set());

  open(): void {
    this.expandedUids.set(new Set());
    this.dialog().nativeElement.showModal();
  }

  close(): void {
    this.dialog().nativeElement.close();
  }

  protected toggleExpanded(uid: string): void {
    const next = new Set(this.expandedUids());
    if (next.has(uid)) {
      next.delete(uid);
    } else {
      next.add(uid);
    }
    this.expandedUids.set(next);
  }
}
