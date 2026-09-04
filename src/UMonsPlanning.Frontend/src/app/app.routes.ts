import { Routes } from '@angular/router';
import { HelpPage } from './features/help/help-page';
import { HomePage } from './features/home/home-page';

export const routes: Routes = [
  { path: '', component: HomePage, title: 'UMonsPlanning — Votre horaire UMONS dans votre calendrier' },
  { path: 'aide', component: HelpPage, title: 'Aide — UMonsPlanning' },
];
