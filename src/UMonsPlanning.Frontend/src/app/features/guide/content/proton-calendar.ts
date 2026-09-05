import type { GuideContent } from '../guide-content';

export const PROTON_CALENDAR_CONTENT: GuideContent = {
  slug: 'horaire-umons-proton-calendar',
  breadcrumbLabel: 'Proton Calendar',
  h1: 'Horaire UMONS dans Proton Calendar',
  description:
    "Comment ajouter votre horaire UMONS dans Proton Calendar via un lien qui se met à jour tout seul.",
  intro: [
    "UMonsPlanning génère un lien à ajouter une seule fois dans Proton Calendar — votre horaire de cours s'y met ensuite à jour tout seul.",
    "Ce lien pointe vers un fichier iCalendar (.ics) régénéré à la demande à partir de votre horaire UMONS actuel. Proton Calendar ne le télécharge pas une seule fois : il revient le consulter régulièrement, ce qui permet à l'agenda de refléter vos changements d'horaire sans aucune manipulation de votre part.",
  ],
  steps: [
    {
      title: 'Générez votre lien',
      body: "Depuis la page d'accueil d'UMonsPlanning, choisissez votre formation (et votre section si nécessaire), puis copiez le lien généré.",
    },
    {
      title: 'Ouvrez les paramètres de Proton Calendar',
      body: 'Sur la version web, allez dans Paramètres → Calendriers → Autres calendriers.',
    },
    {
      title: 'Ajoutez un calendrier depuis une URL',
      body: '« Ajouter un calendrier depuis une URL », collez le lien copié à l\'étape 1, puis validez.',
    },
    {
      title: 'Retrouvez votre horaire',
      body: 'Le calendrier ajouté apparaît dans la liste « Autres calendriers », avec une couleur que vous pouvez personnaliser pour le distinguer de vos autres agendas.',
    },
  ],
  pitfallsTitle: 'Pièges à connaître',
  pitfalls: [
    {
      title: "L'application mobile ne propose pas cette option",
      body: "Comme Google Calendar, l'application mobile Proton Calendar ne permet pas d'ajouter un calendrier par URL directement. Abonnez-vous une fois depuis la version web — la synchronisation suit ensuite automatiquement sur mobile.",
    },
    {
      title: "Si l'agenda n'apparaît pas",
      body: "Vérifiez que le lien a été copié en entier, sans espace avant ou après. Si besoin, retirez le calendrier ajouté et recommencez l'étape 3 — cela résout la plupart des cas où rien ne s'affiche.",
    },
    {
      title: 'Confidentialité',
      body: "Le lien ne contient que la formation et la section choisies (déjà publiques sur l'espace invité PRONOTE) : aucune donnée personnelle n'y transite, Proton ne voit qu'un fichier iCalendar classique — cohérent avec l'approche de Proton en matière de vie privée.",
    },
  ],
  lastUpdatedDisplay: '5 septembre 2026',
  lastUpdatedIso: '2026-09-05',
};
