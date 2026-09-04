import { NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { environment } from '../environments/environment';
import { LegalDisclaimerDialog } from './features/legal-disclaimer/legal-disclaimer-dialog';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, NgOptimizedImage, LegalDisclaimerDialog],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  protected readonly isTestEnvironment = environment.isTestEnvironment;

  protected readonly footerGuideLinks = [
    { label: 'Google Calendar', path: '/horaire-umons-google-calendar' },
    { label: 'Outlook', path: '/horaire-umons-outlook' },
    { label: 'Apple Calendar', path: '/horaire-umons-apple-calendar' },
    { label: 'Thunderbird', path: '/horaire-umons-thunderbird' },
    { label: 'Proton Calendar', path: '/horaire-umons-proton-calendar' },
    { label: 'Hyperplanning UMONS', path: '/hyperplanning-umons' },
  ];
}
