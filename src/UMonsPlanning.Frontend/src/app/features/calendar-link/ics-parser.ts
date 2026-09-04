import ICAL from 'ical.js';
import type { PreviewEvent } from '../../core/models';

/**
 * Decodes a .ics file (as returned by `/api/schedule.ics`) into events usable for the day-by-day
 * preview list. The backend never emits recurrence (`RRULE`): each occurrence is already its own
 * distinct `VEVENT`, so no recurrence expansion is needed here.
 */
export function parseIcsToEvents(icsText: string): PreviewEvent[] {
  const jcalData = ICAL.parse(icsText);
  const calendar = new ICAL.Component(jcalData);

  return calendar.getAllSubcomponents('vevent').map((component) => {
    const event = new ICAL.Event(component);
    const status = component.getFirstPropertyValue('status');

    return {
      uid: event.uid,
      summary: event.summary ?? '',
      location: event.location ?? '',
      description: event.description ?? '',
      status: typeof status === 'string' ? status : null,
      start: event.startDate.toJSDate(),
      end: event.endDate.toJSDate(),
    } satisfies PreviewEvent;
  });
}
