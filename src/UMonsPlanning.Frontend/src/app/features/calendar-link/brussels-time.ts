const PARTS_FORMATTER = new Intl.DateTimeFormat('en-US', {
  timeZone: 'Europe/Brussels',
  year: 'numeric',
  month: 'numeric',
  day: 'numeric',
  hour: 'numeric',
  minute: 'numeric',
  hourCycle: 'h23',
});

export interface BrusselsDateParts {
  readonly year: number;
  readonly month: number;
  readonly day: number;
  readonly hour: number;
  readonly minute: number;
  /** Monday = 0 .. Sunday = 6. */
  readonly weekday: number;
}

/**
 * Decomposes a `Date` into its Europe/Brussels wall-clock components. Every course happens in
 * Brussels regardless of the browser's own local timezone, so the plain `Date.prototype.getHours`
 * family (which reads the *runtime's* local timezone) is never the right tool here — it silently
 * shows the wrong time to anyone whose device isn't set to Europe/Brussels, and was caught by CI
 * running in UTC.
 */
export function toBrusselsParts(date: Date): BrusselsDateParts {
  const parts = PARTS_FORMATTER.formatToParts(date);
  const part = (type: string): number => Number(parts.find((p) => p.type === type)?.value);

  const year = part('year');
  const month = part('month');
  const day = part('day');
  // Day-of-week only depends on the calendar date, so treating the Brussels-local y/m/d as if it
  // were UTC is a correct, well-known way to derive it without a timezone-aware date library.
  const weekday = (new Date(Date.UTC(year, month - 1, day)).getUTCDay() + 6) % 7;

  return { year, month, day, hour: part('hour'), minute: part('minute'), weekday };
}

/** Formats a `Date` as its Brussels wall-clock time, e.g. "9h15". */
export function formatBrusselsTime(date: Date): string {
  const { hour, minute } = toBrusselsParts(date);
  return `${hour}h${minute.toString().padStart(2, '0')}`;
}
