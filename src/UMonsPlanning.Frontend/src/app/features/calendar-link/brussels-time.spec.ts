import { describe, expect, it } from 'vitest';
import { formatBrusselsTime, toBrusselsParts } from './brussels-time';

describe('toBrusselsParts', () => {
  it('resolves a summer (CEST, UTC+2) instant to the correct Brussels wall-clock time', () => {
    // 2026-09-21T09:15:00+02:00
    const parts = toBrusselsParts(new Date('2026-09-21T07:15:00.000Z'));

    expect(parts).toEqual({ year: 2026, month: 9, day: 21, hour: 9, minute: 15, weekday: 0 });
  });

  it('resolves a winter (CET, UTC+1) instant to the correct Brussels wall-clock time', () => {
    // 2026-01-12T09:15:00+01:00
    const parts = toBrusselsParts(new Date('2026-01-12T08:15:00.000Z'));

    expect(parts).toEqual({ year: 2026, month: 1, day: 12, hour: 9, minute: 15, weekday: 0 });
  });

  it('shifts to the next Brussels calendar day when the UTC instant is still the previous day', () => {
    // 2026-09-21T23:30:00+02:00 -> already 2026-09-21T21:30:00Z, but 2026-09-22T00:30:00+02:00
    // (a course ending just past midnight) is 2026-09-21T22:30:00Z.
    const parts = toBrusselsParts(new Date('2026-09-21T22:30:00.000Z'));

    expect(parts).toMatchObject({ year: 2026, month: 9, day: 22, hour: 0, minute: 30 });
  });
});

describe('formatBrusselsTime', () => {
  it('pads single-digit minutes', () => {
    expect(formatBrusselsTime(new Date('2026-09-21T07:05:00.000Z'))).toBe('9h05');
  });

  it('does not pad the hour', () => {
    expect(formatBrusselsTime(new Date('2026-09-21T07:15:00.000Z'))).toBe('9h15');
  });
});
