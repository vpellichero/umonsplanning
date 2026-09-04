import type { GuideContent } from '../guide-content';

export const THUNDERBIRD_CONTENT: GuideContent = {
  slug: 'horaire-umons-thunderbird',
  breadcrumbLabel: 'Thunderbird',
  h1: 'Horaire UMONS dans Thunderbird',
  description:
    "Comment ajouter votre horaire UMONS dans Thunderbird via un lien qui se met à jour tout seul.",
  intro: [
    "UMonsPlanning génère un lien à ajouter une seule fois dans Thunderbird — votre horaire de cours s'y met ensuite à jour tout seul.",
    "Ce lien pointe vers un fichier iCalendar (.ics) régénéré à la demande à partir de votre horaire UMONS actuel. Thunderbird ne le télécharge pas une seule fois : c'est un calendrier « en réseau », qu'il revient consulter à intervalle régulier — vous pouvez d'ailleurs choisir vous-même cet intervalle (voir le piège ci-dessous).",
  ],
  steps: [
    {
      title: 'Générez votre lien',
      body: "Depuis la page d'accueil d'UMonsPlanning, choisissez votre formation (et votre section si nécessaire), puis copiez le lien généré.",
    },
    {
      title: 'Créez un nouveau calendrier',
      body: 'Menu Fichier → Nouveau → Calendrier…',
    },
    {
      title: 'Choisissez « Sur le réseau »',
      body: 'Sélectionnez « Sur le réseau », cliquez sur Suivant, puis laissez le format « iCalendar (ICS) ».',
    },
    {
      title: 'Collez le lien',
      body: "Collez le lien copié à l'étape 1 dans le champ « Emplacement », donnez un nom à l'agenda et terminez.",
    },
    {
      title: 'Retrouvez votre horaire',
      body: 'Le nouvel agenda apparaît dans la liste de gauche de Thunderbird, coché par défaut — décochez-le si vous voulez le masquer temporairement sans le supprimer.',
    },
  ],
  pitfallsTitle: 'Pièges à connaître',
  pitfalls: [
    {
      title: 'La fréquence de vérification se règle manuellement',
      body: "Par défaut, Thunderbird vérifie le lien assez peu souvent. Faites un clic droit sur l'agenda → Propriétés, et resserrez l'intervalle d'actualisation si vous voulez des mises à jour plus rapprochées.",
    },
    {
      title: "Si l'agenda n'apparaît pas",
      body: "Vérifiez que le lien a été collé en entier dans le champ Emplacement, sans espace avant ou après. Si besoin, supprimez l'agenda et recommencez la création — cela résout la plupart des cas où rien ne s'affiche.",
    },
    {
      title: 'Confidentialité',
      body: "Le lien ne contient que la formation et la section choisies (déjà publiques sur l'espace invité PRONOTE) : aucune donnée personnelle n'y transite, Thunderbird ne voit qu'un fichier iCalendar classique.",
    },
  ],
  lastUpdatedDisplay: '5 septembre 2026',
  lastUpdatedIso: '2026-09-05',
};
