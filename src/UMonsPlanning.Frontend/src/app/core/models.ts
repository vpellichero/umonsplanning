/** A field of study (dropdown #1) or a sub-choice (dropdown #2), as returned by the API. */
export interface Resource {
  readonly id: string;
  readonly label: string;
}

/** A course event decoded from the .ics file, for the day-by-day preview list. */
export interface PreviewEvent {
  readonly uid: string;
  readonly summary: string;
  readonly location: string;
  readonly description: string;
  readonly status: string | null;
  readonly start: Date;
  readonly end: Date;
}
