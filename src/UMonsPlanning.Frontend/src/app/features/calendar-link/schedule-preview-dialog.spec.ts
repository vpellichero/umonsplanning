import { TestBed } from '@angular/core/testing';
import type { PreviewEvent } from '../../core/models';
import { SchedulePreviewDialog } from './schedule-preview-dialog';

function event(overrides: Partial<PreviewEvent> = {}): PreviewEvent {
  return {
    uid: 'course-1',
    summary: 'Langue ALLE',
    location: 'NiDeVinci.313',
    description: 'Groupes : D3',
    status: 'CONFIRMED',
    start: new Date(2026, 8, 21, 9, 15),
    end: new Date(2026, 8, 21, 10, 15),
    ...overrides,
  };
}

describe('SchedulePreviewDialog', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [SchedulePreviewDialog] }).compileComponents();
  });

  it('starts with every course collapsed', () => {
    const fixture = TestBed.createComponent(SchedulePreviewDialog);
    fixture.componentRef.setInput('events', [event()]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('#event-description-course-1')).toBeNull();
    expect(compiled.querySelector('button[aria-expanded="false"]')).not.toBeNull();
  });

  it('expands the course description when its header button is clicked', () => {
    const fixture = TestBed.createComponent(SchedulePreviewDialog);
    fixture.componentRef.setInput('events', [event()]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const toggle = compiled.querySelector<HTMLButtonElement>('button[aria-controls="event-description-course-1"]')!;
    toggle.click();
    fixture.detectChanges();

    expect(compiled.querySelector('#event-description-course-1')?.textContent).toContain('Groupes : D3');
    expect(toggle.getAttribute('aria-expanded')).toBe('true');
  });

  it('collapses the description again on a second click', () => {
    const fixture = TestBed.createComponent(SchedulePreviewDialog);
    fixture.componentRef.setInput('events', [event()]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const toggle = compiled.querySelector<HTMLButtonElement>('button[aria-controls="event-description-course-1"]')!;
    toggle.click();
    fixture.detectChanges();
    toggle.click();
    fixture.detectChanges();

    expect(compiled.querySelector('#event-description-course-1')).toBeNull();
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
  });

  it('tracks each course independently', () => {
    const fixture = TestBed.createComponent(SchedulePreviewDialog);
    fixture.componentRef.setInput('events', [
      event({ uid: 'course-1' }),
      event({ uid: 'course-2', start: new Date(2026, 8, 22, 13, 30), end: new Date(2026, 8, 22, 15, 30) }),
    ]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('button[aria-controls="event-description-course-1"]')!.click();
    fixture.detectChanges();

    expect(compiled.querySelector('#event-description-course-1')).not.toBeNull();
    expect(compiled.querySelector('#event-description-course-2')).toBeNull();
  });
});
