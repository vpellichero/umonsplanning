import { describe, expect, it } from 'vitest';
import type { PreviewEvent } from '../../core/models';
import { groupEventsByDay } from './schedule-days';

function event(overrides: Partial<PreviewEvent> & { start: Date; end: Date }): PreviewEvent {
  return {
    uid: 'uid',
    summary: 'Cours',
    location: '',
    description: '',
    status: 'CONFIRMED',
    ...overrides,
  };
}

describe('groupEventsByDay', () => {
  it('groups events by calendar day', () => {
    const days = groupEventsByDay([
      event({ start: new Date(2026, 8, 21, 9, 15), end: new Date(2026, 8, 21, 10, 15) }),
      event({ start: new Date(2026, 8, 22, 13, 30), end: new Date(2026, 8, 22, 15, 30) }),
    ]);

    expect(days).toHaveLength(2);
    expect(days[0].label).toBe('Lundi 21/9');
    expect(days[1].label).toBe('Mardi 22/9');
  });

  it('orders days chronologically regardless of input order or month boundary', () => {
    const days = groupEventsByDay([
      event({ start: new Date(2026, 9, 1, 9, 0), end: new Date(2026, 9, 1, 10, 0) }),
      event({ start: new Date(2026, 8, 30, 9, 0), end: new Date(2026, 8, 30, 10, 0) }),
    ]);

    expect(days.map((d) => d.label)).toEqual(['Mercredi 30/9', 'Jeudi 1/10']);
  });

  it('orders events within a day chronologically regardless of input order', () => {
    const days = groupEventsByDay([
      event({ uid: 'afternoon', start: new Date(2026, 8, 21, 13, 30), end: new Date(2026, 8, 21, 15, 30) }),
      event({ uid: 'morning', start: new Date(2026, 8, 21, 9, 15), end: new Date(2026, 8, 21, 10, 15) }),
    ]);

    expect(days[0].events.map((e) => e.uid)).toEqual(['morning', 'afternoon']);
  });

  it('returns an empty list for no events', () => {
    expect(groupEventsByDay([])).toEqual([]);
  });
});
