import { Routes } from '@angular/router';
import { HelpPage } from './features/help/help-page';
import { HomePage } from './features/home/home-page';
import { NotFoundPage } from './features/not-found/not-found-page';

export const routes: Routes = [
  {
    path: '',
    component: HomePage,
    title: 'UMonsPlanning — Votre horaire UMONS dans votre calendrier',
    data: {
      description:
        "Générez un lien à ajouter une fois dans Google Calendar, Outlook ou Apple Calendar pour garder votre horaire de cours UMONS toujours à jour.",
    },
  },
  {
    path: 'aide',
    component: HelpPage,
    title: 'Aide — UMonsPlanning',
    data: {
      description:
        "Comment ajouter votre lien de calendrier UMonsPlanning dans Google Calendar, Outlook, Thunderbird ou Proton Calendar.",
    },
  },
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
