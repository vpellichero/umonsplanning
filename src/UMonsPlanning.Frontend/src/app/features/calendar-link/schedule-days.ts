import type { PreviewEvent } from '../../core/models';

const WEEKDAY_LABELS = ['Lundi', 'Mardi', 'Mercredi', 'Jeudi', 'Vendredi', 'Samedi', 'Dimanche'];

export interface ScheduleDay {
  readonly date: Date;
  readonly label: string;
  readonly events: readonly PreviewEvent[];
}

/**
 * Groups decoded .ics events by calendar day, each day listing its courses in chronological
 * order — for a stacked list display rather than a time grid.
 */
export function groupEventsByDay(events: readonly PreviewEvent[]): readonly ScheduleDay[] {
  const eventsByDate = new Map<string, PreviewEvent[]>();

  for (const event of events) {
    const key = dateKey(event.start);
    const bucket = eventsByDate.get(key);
    if (bucket) {
      bucket.push(event);
    } else {
      eventsByDate.set(key, [event]);
    }
  }

  return [...eventsByDate.values()]
    .map((dayEvents): ScheduleDay => {
      const sortedEvents = [...dayEvents].sort((a, b) => a.start.getTime() - b.start.getTime());
      const date = sortedEvents[0].start;
      return { date, label: formatDayLabel(date), events: sortedEvents };
    })
    .sort((a, b) => a.date.getTime() - b.date.getTime());
}

function dateKey(date: Date): string {
  return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;
}

function formatDayLabel(date: Date): string {
  return `${WEEKDAY_LABELS[(date.getDay() + 6) % 7]} ${date.getDate()}/${date.getMonth() + 1}`;
}
