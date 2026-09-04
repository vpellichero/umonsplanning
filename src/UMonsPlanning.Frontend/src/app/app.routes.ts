import { Routes } from '@angular/router';
import { environment } from '../environments/environment';
import {
  buildBreadcrumbJsonLd,
  buildFaqJsonLd,
  buildHowToJsonLd,
} from './core/structured-data-builders';
import { HelpPage } from './features/help/help-page';
import { HOME_FAQ } from './features/home/home-faq';
import { HomePage } from './features/home/home-page';
import { APPLE_CALENDAR_CONTENT } from './features/guide/content/apple-calendar';
import { GOOGLE_CALENDAR_CONTENT } from './features/guide/content/google-calendar';
import { HYPERPLANNING_UMONS_CONTENT } from './features/guide/content/hyperplanning-umons';
import { OUTLOOK_CONTENT } from './features/guide/content/outlook';
import { PROTON_CALENDAR_CONTENT } from './features/guide/content/proton-calendar';
import { THUNDERBIRD_CONTENT } from './features/guide/content/thunderbird';
import type { GuideContent } from './features/guide/guide-content';
import { GuidePage } from './features/guide/guide-page';
import { NotFoundPage } from './features/not-found/not-found-page';

/** Route config + `data` (description, JSON-LD) for one destination page, from its content object -
 * the HowTo steps and breadcrumb below are built from the exact same data driving the visible page
 * (GuidePage), so the structured data can never drift from what's actually on the page. */
function guideRoute(content: GuideContent) {
  return {
    path: content.slug,
    component: GuidePage,
    // No " — UMonsPlanning" suffix here: content.h1 is already the exact, budget-conscious (<=60
    // char) title from the SEO brief for this specific page - appending the site name would blow
    // past that budget.
    title: content.h1,
    data: {
      guide: content,
      description: content.description,
      jsonLd: [
        buildHowToJsonLd({
          name: content.h1,
          description: content.description,
          steps: content.steps.map((step) => ({ name: step.title, text: step.body })),
        }),
        buildBreadcrumbJsonLd([
          { name: 'Accueil', url: `${environment.baseUrl}/` },
          { name: content.breadcrumbLabel, url: `${environment.baseUrl}/${content.slug}` },
        ]),
      ],
    },
  };
}

export const routes: Routes = [
  {
    path: '',
    component: HomePage,
    title: 'UMonsPlanning — Votre horaire UMONS dans votre calendrier',
    data: {
      description:
        "Générez un lien à ajouter une fois dans Google Calendar, Outlook ou Apple Calendar pour garder votre horaire de cours UMONS toujours à jour.",
      jsonLd: [
        {
          '@context': 'https://schema.org',
          '@type': 'WebSite',
          name: 'UMonsPlanning',
          url: `${environment.baseUrl}/`,
          inLanguage: 'fr-BE',
        },
        {
          '@context': 'https://schema.org',
          '@type': 'Person',
          name: 'Vincent Pellichero',
          sameAs: ['https://github.com/vpellichero'],
        },
        buildFaqJsonLd(HOME_FAQ.map((entry) => ({ question: entry.question, answer: entry.answer }))),
      ],
    },
  },
  {
    path: 'aide',
    component: HelpPage,
    title: 'Aide — UMonsPlanning',
    data: {
      description:
        "Comment ajouter votre lien de calendrier UMonsPlanning dans Google Calendar, Outlook, Apple Calendar, Thunderbird ou Proton Calendar.",
      jsonLd: [
        buildBreadcrumbJsonLd([
          { name: 'Accueil', url: `${environment.baseUrl}/` },
          { name: 'Aide', url: `${environment.baseUrl}/aide` },
        ]),
      ],
    },
  },
  guideRoute(GOOGLE_CALENDAR_CONTENT),
  guideRoute(OUTLOOK_CONTENT),
  guideRoute(APPLE_CALENDAR_CONTENT),
  guideRoute(THUNDERBIRD_CONTENT),
  guideRoute(PROTON_CALENDAR_CONTENT),
  guideRoute(HYPERPLANNING_UMONS_CONTENT),
  {
    path: '404',
    component: NotFoundPage,
    title: 'Page introuvable — UMonsPlanning',
    data: {
      description: 'Cette page n\'existe pas ou plus.',
      noIndex: true,
    },
  },
  {
    path: '**',
    component: NotFoundPage,
    title: 'Page introuvable — UMonsPlanning',
    data: {
      description: 'Cette page n\'existe pas ou plus.',
      noIndex: true,
    },
  },
];
