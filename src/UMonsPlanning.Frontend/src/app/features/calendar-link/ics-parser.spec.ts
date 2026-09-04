import { describe, expect, it } from 'vitest';
import { parseIcsToEvents } from './ics-parser';

const SAMPLE_ICS = [
  'BEGIN:VCALENDAR',
  'PRODID:-//UMonsPlanning//Horaires UMONS//FR',
  'VERSION:2.0',
  'X-WR-CALNAME:.BAB3 - Traduction et interprétation',
  'BEGIN:VTIMEZONE',
  'TZID:Europe/Brussels',
  'BEGIN:STANDARD',
  'DTSTART:20241027T030000',
  'RRULE:FREQ=YEARLY;BYDAY=-1SU;BYMONTH=10',
  'TZNAME:CET',
  'TZOFFSETFROM:+0200',
  'TZOFFSETTO:+0100',
  'END:STANDARD',
  'BEGIN:DAYLIGHT',
  'DTSTART:20250330T020000',
  'RRULE:FREQ=YEARLY;BYDAY=-1SU;BYMONTH=3',
  'TZNAME:CEST',
  'TZOFFSETFROM:+0100',
  'TZOFFSETTO:+0200',
  'END:DAYLIGHT',
  'END:VTIMEZONE',
  'BEGIN:VEVENT',
  'UID:9f2c41ab7d0e5533@umonsplanning',
  'SUMMARY:T-ALLE-401 - Langue ALLE',
  'DESCRIPTION:Groupes : D3',
  'LOCATION:NiDeVinci.313\\, NiDeVinci.314',
  'DTSTART;TZID=Europe/Brussels:20260921T091500',
  'DTEND;TZID=Europe/Brussels:20260921T101500',
  'DTSTAMP:20260902T170731Z',
  'STATUS:CONFIRMED',
  'END:VEVENT',
  'BEGIN:VEVENT',
  'UID:47a8021c5a0f9250@umonsplanning',
  'SUMMARY:M-DOYM-051 - Statistiques I',
  'DTSTART;TZID=Europe/Brussels:20260922T133000',
  'DTEND;TZID=Europe/Brussels:20260922T153000',
  'DTSTAMP:20260902T170731Z',
  'STATUS:CANCELLED',
  'END:VEVENT',
  'END:VCALENDAR',
].join('\r\n');

describe('parseIcsToEvents', () => {
  it('decodes every VEVENT into a PreviewEvent', () => {
    const events = parseIcsToEvents(SAMPLE_ICS);

    expect(events).toHaveLength(2);
  });

  it('reads summary, location, description and unfolds/unescapes values', () => {
    const [first] = parseIcsToEvents(SAMPLE_ICS);

    expect(first.summary).toBe('T-ALLE-401 - Langue ALLE');
    expect(first.location).toBe('NiDeVinci.313, NiDeVinci.314');
    expect(first.description).toBe('Groupes : D3');
    expect(first.status).toBe('CONFIRMED');
  });

  it('converts DTSTART/DTEND to the correct local wall-clock time', () => {
    const [first] = parseIcsToEvents(SAMPLE_ICS);

    expect(first.start.getHours()).toBe(9);
    expect(first.start.getMinutes()).toBe(15);
    expect(first.end.getHours()).toBe(10);
    expect(first.end.getMinutes()).toBe(15);
  });

  it('reports a cancelled course as such', () => {
    const [, second] = parseIcsToEvents(SAMPLE_ICS);

    expect(second.status).toBe('CANCELLED');
  });

  it('returns an empty array for a calendar without events', () => {
    const events = parseIcsToEvents(
      'BEGIN:VCALENDAR\r\nPRODID:-//UMonsPlanning//FR\r\nVERSION:2.0\r\nEND:VCALENDAR',
    );

    expect(events).toEqual([]);
  });
});
