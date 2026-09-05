import type { GuideContent } from '../guide-content';

export const APPLE_CALENDAR_CONTENT: GuideContent = {
  slug: 'horaire-umons-apple-calendar',
  breadcrumbLabel: 'Apple Calendar',
  h1: 'Horaire UMONS sur iPhone et Apple Calendar',
  description:
    "Comment ajouter votre horaire UMONS dans Calendrier sur iPhone, iPad et Mac via un lien qui se met à jour tout seul.",
  intro: [
    "UMonsPlanning génère un lien à ajouter une seule fois dans Calendrier (iPhone, iPad ou Mac) — votre horaire de cours s'y met ensuite à jour tout seul, sans rien réimporter.",
    "Ce lien pointe vers un fichier iCalendar (.ics) régénéré à la demande à partir de votre horaire UMONS actuel. L'app Calendrier ne le télécharge pas une seule fois : c'est un abonnement, qu'elle revient consulter régulièrement — exactement le mécanisme qu'Apple appelle « calendrier par abonnement ».",
  ],
  steps: [
    {
      title: 'Générez votre lien',
      body: "Depuis la page d'accueil d'UMonsPlanning, choisissez votre formation (et votre section si nécessaire), puis copiez le lien généré.",
    },
    {
      title: 'Sur iPhone ou iPad',
      body: "Ouvrez l'app Calendrier → bouton « Calendriers » en bas → « Ajouter un calendrier » → « Ajouter un calendrier par abonnement » → collez le lien → « Rechercher » → « Ajouter ».",
    },
    {
      title: 'Sur Mac',
      body: "Ouvrez l'app Calendrier → menu Fichier → « Nouvel abonnement au calendrier… » → collez le lien → « S'abonner ».",
    },
    {
      title: 'Synchronisation automatique',
      body: "Une fois abonné via votre identifiant Apple (iCloud), l'horaire apparaît sur tous vos appareils connectés au même compte.",
    },
  ],
  pitfallsTitle: 'Pièges à connaître',
  pitfalls: [
    {
      title: 'Sur Mac, vérifiez la fréquence d\'actualisation',
      body: "Une fenêtre de réglages s'affiche juste après « S'abonner » : selon les versions de macOS, la fréquence peut être réglée par défaut sur « Manuellement » — dans ce cas l'agenda ne se met jamais à jour tout seul. Choisissez plutôt « Toutes les heures » ou « Toutes les 15 minutes ».",
    },
    {
      title: 'Sur iPhone/iPad, la fréquence n\'est pas réglable',
      body: "iOS gère seul la fréquence de vérification du lien, sans option de réglage manuel — comptez généralement de quelques heures à une journée après un changement d'horaire.",
    },
    {
      title: "Si l'agenda n'apparaît pas",
      body: "Vérifiez que le lien a été copié en entier, sans espace avant ou après. Si besoin, retirez l'abonnement (Réglages → Calendrier → Comptes) et recommencez — cela résout la plupart des cas où rien ne s'affiche.",
    },
  ],
  officialLink: {
    label: 'Use iCloud calendar subscriptions (Apple)',
    url: 'https://support.apple.com/en-us/102301',
  },
  lastUpdatedDisplay: '5 septembre 2026',
  lastUpdatedIso: '2026-09-05',
};
