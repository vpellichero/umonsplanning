import type { GuideContent } from '../guide-content';

export const HYPERPLANNING_UMONS_CONTENT: GuideContent = {
  slug: 'hyperplanning-umons',
  breadcrumbLabel: 'Hyperplanning UMONS',
  h1: 'Hyperplanning UMONS : récupérer son horaire 2026-2027',
  description:
    "Hyperplanning UMONS : ce que c'est, et comment récupérer votre horaire 2026-2027 dans votre propre application de calendrier.",
  intro: [
    "Hyperplanning est le portail sur lequel l'UMONS publie les horaires de cours ; PRONOTE est le logiciel qui le fait fonctionner. Pour consulter votre semaine, il faut normalement s'y connecter à chaque fois — pas d'application dédiée, pas de synchronisation avec votre propre agenda.",
    "UMonsPlanning ne remplace pas ce portail : il lit les mêmes données, publiques sur l'espace invité (aucun identifiant n'est demandé côté PRONOTE pour cet espace), et génère un lien à ajouter une seule fois dans votre propre application de calendrier — elle se synchronise ensuite automatiquement, sans revisiter le portail à chaque fois que vous voulez connaître votre prochain cours.",
    "Ce lien pointe vers un fichier iCalendar (.ics) régénéré à la demande à partir de votre horaire UMONS actuel, au format que toutes les applications de calendrier courantes savent lire (Google Calendar, Outlook, Apple Calendar, Thunderbird, Proton Calendar…).",
  ],
  steps: [
    {
      title: 'Choisissez votre formation',
      body: 'Et votre section si nécessaire, depuis la page d\'accueil.',
    },
    {
      title: 'Générez votre lien',
      body: 'Un lien unique est créé pour votre choix — testez-le si vous voulez voir à quoi ressemble votre semaine avant de l\'ajouter.',
    },
    {
      title: 'Ajoutez-le dans votre application de calendrier',
      body: 'Une seule fois : elle revient ensuite consulter le lien toute seule. Un guide dédié existe pour chaque application courante (liens ci-dessous).',
    },
  ],
  pitfallsTitle: 'Précisions',
  pitfalls: [
    {
      title: "Ce n'est pas un outil officiel",
      body: "UMonsPlanning est un projet personnel, non affilié à l'UMONS. En cas de doute sur votre horaire, l'espace Hyperplanning officiel de l'UMONS reste la référence.",
    },
    {
      title: 'Mêmes données, présentées différemment',
      body: "Le contenu provient de l'espace invité PRONOTE, déjà accessible publiquement sans identifiant — UMonsPlanning ne fait que le rendre consultable depuis votre calendrier habituel.",
    },
    {
      title: 'Aucune donnée personnelle collectée',
      body: "Pas de compte, pas de cookie : le formulaire de génération tourne entièrement dans votre navigateur, et le lien généré ne contient que la formation et la section choisies (déjà publiques). Seul un compteur global et anonyme du nombre de liens générés est conservé.",
    },
  ],
  relatedGuides: [
    { label: 'Google Calendar', path: '/horaire-umons-google-calendar' },
    { label: 'Outlook', path: '/horaire-umons-outlook' },
    { label: 'Apple Calendar (iPhone, iPad, Mac)', path: '/horaire-umons-apple-calendar' },
    { label: 'Thunderbird', path: '/horaire-umons-thunderbird' },
    { label: 'Proton Calendar', path: '/horaire-umons-proton-calendar' },
  ],
  lastUpdatedDisplay: '5 septembre 2026',
  lastUpdatedIso: '2026-09-05',
};
