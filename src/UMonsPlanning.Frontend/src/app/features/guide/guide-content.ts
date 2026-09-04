/** Shared shape for the six "how to add your UMONS schedule to X" destination pages
 * (src/app/features/guide/content/) — one GuidePage component renders any of them. */
export interface GuideStep {
  readonly title: string;
  readonly body: string;
}

export interface GuideRelatedLink {
  readonly label: string;
  readonly path: string;
}

export interface GuideContent {
  readonly slug: string;
  readonly breadcrumbLabel: string;
  readonly h1: string;
  /** Meta description (~150-160 chars) - distinct from `intro`, which can be longer/more conversational. */
  readonly description: string;
  readonly intro: readonly string[];
  readonly steps: readonly GuideStep[];
  readonly pitfallsTitle: string;
  readonly pitfalls: readonly GuideStep[];
  /** Only populated by the hub page (hyperplanning-umons) — links to the per-app guides. */
  readonly relatedGuides?: readonly GuideRelatedLink[];
  /** Official documentation link, when one is directly relevant (verified before use — never fabricated, see CLAUDE.md §9). */
  readonly officialLink?: { readonly label: string; readonly url: string };
  readonly lastUpdatedDisplay: string;
  readonly lastUpdatedIso: string;
}
