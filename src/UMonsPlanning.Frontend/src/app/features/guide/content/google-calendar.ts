import type { GuideContent } from '../guide-content';

export const GOOGLE_CALENDAR_CONTENT: GuideContent = {
  slug: 'horaire-umons-google-calendar',
  breadcrumbLabel: 'Google Calendar',
  h1: 'Ajouter son horaire UMONS dans Google Calendar',
  description:
    "Comment ajouter votre horaire UMONS à Google Calendar via un lien qui se met à jour tout seul, sur ordinateur et sur mobile.",
  intro: [
    "Plutôt que de réimporter votre horaire à la main à chaque changement, UMonsPlanning génère un lien que Google Calendar interroge tout seul : ajoutez-le une fois, il reste à jour ensuite sans rien refaire.",
    "Techniquement, ce lien pointe vers un fichier iCalendar (.ics) régénéré à la demande à partir de votre horaire UMONS actuel. Google Calendar ne le télécharge pas une fois pour toutes : il revient le consulter régulièrement, ce qui est précisément ce qui permet à votre agenda de rester synchronisé sans aucune action de votre part.",
  ],
  steps: [
    {
      title: 'Générez votre lien',
      body: "Depuis la page d'accueil d'UMonsPlanning, choisissez votre formation (et votre section si nécessaire), puis copiez le lien généré.",
    },
    {
      title: 'Ouvrez la version web de Google Calendar',
      body: 'Sur un ordinateur, ou le navigateur de votre téléphone, rendez-vous sur calendar.google.com — pas l\'application mobile (voir le piège ci-dessous).',
    },
    {
      title: 'Ajoutez un agenda « à partir de l\'URL »',
      body: 'Dans le menu de gauche, à côté de « Autres agendas », cliquez sur le « + » puis choisissez « À partir de l\'URL ».',
    },
    {
      title: 'Collez le lien',
      body: 'Collez le lien copié à l\'étape 1, puis cliquez sur « Ajouter un agenda ».',
    },
    {
      title: 'Patientez quelques minutes',
      body: "Votre horaire apparaît peu après sur la version web. Sur l'application mobile, un réglage supplémentaire est parfois nécessaire — voir le piège ci-dessous.",
    },
  ],
  pitfallsTitle: 'Pièges à connaître',
  pitfalls: [
    {
      title: "L'application mobile ne propose pas cette option",
      body: "Google Calendar sur iOS et Android n'a pas d'option « Ajouter par URL » dans son interface. Il faut obligatoirement passer par la version web au moins une fois (voir le piège suivant pour le faire apparaître ensuite sur mobile).",
    },
    {
      title: "Le calendrier n'apparaît pas automatiquement sur l'application mobile",
      body: 'Une fois ajouté depuis la version web, l\'agenda ne s\'affiche pas toujours tout de suite sur mobile. Ouvrez l\'application Google Calendar sur votre téléphone, allez dans ses paramètres (appuyez sur son nom dans la liste des agendas) et cochez la case « Synchroniser » — il apparaît alors dans votre liste de calendriers.',
    },
    {
      title: "La mise à jour n'est pas instantanée",
      body: "Google choisit seul la fréquence à laquelle il revient consulter le lien (aucun réglage possible de notre côté) — comptez généralement de quelques heures à une journée après un changement d'horaire.",
    },
    {
      title: "Si l'agenda n'apparaît pas",
      body: "Vérifiez d'abord que le lien a été copié en entier (sans espace avant ou après). Si le problème persiste, supprimez l'agenda depuis « Paramètres → Autres agendas » puis recommencez l'étape 3 — cela suffit dans la grande majorité des cas.",
    },
  ],
  lastUpdatedDisplay: '5 septembre 2026',
  lastUpdatedIso: '2026-09-05',
};
