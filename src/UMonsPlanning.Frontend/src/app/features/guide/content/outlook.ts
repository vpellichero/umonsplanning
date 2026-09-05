import type { GuideContent } from '../guide-content';

export const OUTLOOK_CONTENT: GuideContent = {
  slug: 'horaire-umons-outlook',
  breadcrumbLabel: 'Outlook',
  h1: 'Horaire UMONS dans Outlook : mode d\'emploi',
  description:
    "Comment ajouter votre horaire UMONS dans Outlook (web et bureau) via un lien qui se met à jour tout seul.",
  intro: [
    "UMonsPlanning génère un lien que vous ajoutez une seule fois dans Outlook — votre horaire de cours s'y met ensuite à jour tout seul.",
    "Ce lien pointe vers un fichier iCalendar (.ics) régénéré à la demande à partir de votre horaire UMONS actuel. Outlook ne le télécharge pas une seule fois : il revient le consulter régulièrement, ce qui permet à l'agenda de refléter vos changements d'horaire sans aucune manipulation de votre part.",
  ],
  steps: [
    {
      title: 'Générez votre lien',
      body: "Depuis la page d'accueil d'UMonsPlanning, choisissez votre formation (et votre section si nécessaire), puis copiez le lien généré.",
    },
    {
      title: 'Sur Outlook sur le web',
      body: "Sur outlook.com ou votre compte Microsoft 365 : Calendrier → Ajouter un calendrier → « S'abonner à partir du web ».",
    },
    {
      title: 'Collez le lien',
      body: 'Collez le lien copié à l\'étape 1, donnez un nom à l\'agenda (par exemple « Horaire UMONS »), puis validez.',
    },
    {
      title: 'Sur Outlook de bureau (application classique, Windows)',
      body: 'Ouvrez le volet Calendrier → « À partir d\'Internet » → collez le même lien.',
    },
    {
      title: 'Sur l\'application mobile Outlook',
      body: 'Aucune action supplémentaire nécessaire : l\'agenda ajouté à l\'étape 2 ou 3 apparaît automatiquement dans l\'application mobile, dès lors qu\'elle est connectée au même compte Microsoft.',
    },
  ],
  pitfallsTitle: 'Pièges à connaître',
  pitfalls: [
    {
      title: 'La fréquence de rafraîchissement ne se règle pas',
      body: "C'est Outlook qui décide seul de la fréquence à laquelle il revient consulter le lien — aucun réglage possible de notre côté.",
    },
    {
      title: 'Un seul abonnement suffit généralement',
      body: "S'abonner une fois via votre compte Microsoft (web ou bureau) fait apparaître l'agenda sur tous les appareils connectés à ce même compte, mobile compris.",
    },
    {
      title: "Si l'agenda n'apparaît pas",
      body: 'Vérifiez que le lien a été copié en entier, sans espace avant ou après. Si besoin, supprimez l\'agenda importé et recommencez l\'abonnement — cela résout la plupart des cas où rien ne s\'affiche.',
    },
    {
      title: 'Confidentialité',
      body: "Le lien ne contient que la formation et la section choisies (déjà publiques sur l'espace invité PRONOTE) : aucune donnée personnelle n'y transite, et Microsoft ne voit rien de plus qu'un fichier iCalendar classique.",
    },
  ],
  lastUpdatedDisplay: '5 septembre 2026',
  lastUpdatedIso: '2026-09-05',
};
