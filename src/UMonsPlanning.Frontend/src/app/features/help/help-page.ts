import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Breadcrumb } from '../../core/breadcrumb/breadcrumb';

interface HelpLink {
  readonly name: string;
  readonly instructions: string;
  readonly url: string;
  readonly guidePath: string;
}

@Component({
  selector: 'app-help-page',
  imports: [RouterLink, Breadcrumb],
  templateUrl: './help-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HelpPage {
  protected readonly breadcrumb = [{ label: 'Accueil', link: '/' }, { label: 'Aide' }];

  protected readonly links: readonly HelpLink[] = [
    {
      name: 'Google Calendar (Gmail)',
      instructions:
        "Sur ordinateur uniquement (l'application mobile ne le permet pas) : « Autres agendas » → « À partir de l'URL », puis collez le lien.",
      url: 'https://support.google.com/calendar/answer/37100',
      guidePath: '/horaire-umons-google-calendar',
    },
    {
      name: 'Outlook.com et Outlook (application de bureau)',
      instructions:
        'Dans Outlook sur le web : Calendrier → Ajouter un calendrier → « S\'abonner à partir du web ». Dans Outlook de bureau (classique) : Ouvrir le calendrier → « À partir d\'Internet ».',
      url: 'https://support.microsoft.com/en-us/office/import-or-subscribe-to-a-calendar-in-outlook-com-or-outlook-on-the-web-cff1429c-5af6-41ec-a5b4-74f2c278e98c',
      guidePath: '/horaire-umons-outlook',
    },
    {
      name: 'Apple Calendar (iPhone, iPad, Mac)',
      instructions:
        'Sur iPhone/iPad : Calendrier → Calendriers → Ajouter un calendrier par abonnement, puis collez le lien. Sur Mac : Calendrier → Fichier → Nouvel abonnement au calendrier.',
      url: 'https://support.apple.com/en-us/102301',
      guidePath: '/horaire-umons-apple-calendar',
    },
    {
      name: 'Thunderbird',
      instructions:
        'Menu Fichier → Nouveau → Calendrier… → « Sur le réseau », puis collez le lien dans le champ Emplacement.',
      url: 'https://support.mozilla.org/en-US/kb/creating-new-calendars',
      guidePath: '/horaire-umons-thunderbird',
    },
    {
      name: 'Proton Calendar',
      instructions:
        'Paramètres → Calendriers → Autres calendriers → « Ajouter un calendrier depuis une URL », puis collez le lien.',
      url: 'https://proton.me/support/subscribe-to-external-calendar',
      guidePath: '/horaire-umons-proton-calendar',
    },
  ];
}
