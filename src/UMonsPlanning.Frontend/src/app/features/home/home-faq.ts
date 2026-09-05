/** Single source for the home page's visible FAQ (`<dl>` in home-page.html) and its `FAQPage`
 * JSON-LD (app.routes.ts) — the same array drives both, so they can never drift apart. */
export interface FaqEntry {
  readonly question: string;
  readonly answer: string;
}

export const HOME_FAQ: readonly FaqEntry[] = [
  {
    question: 'Mon horaire se met-il à jour automatiquement ?',
    answer:
      "Oui. Une fois le lien ajouté dans votre application de calendrier, elle revient interroger l'URL toute seule à intervalles réguliers (l'intervalle exact dépend de l'application, généralement de quelques heures à une journée) — vous n'avez rien à réimporter, même après un changement d'horaire à l'UMONS.",
  },
  {
    question:
      "Pourquoi l'application mobile Google Calendar ne permet-elle pas d'ajouter un calendrier par URL ?",
    answer:
      "C'est une limitation de l'application mobile elle-même (iOS et Android) : Google n'y propose pas d'option « Ajouter par URL ». Ajoutez le lien une fois depuis la version web de Google Calendar (sur un ordinateur ou le navigateur de votre téléphone) — le calendrier apparaît ensuite automatiquement dans l'application mobile, sans rien réinstaller.",
  },
  {
    question: "Combien de temps avant qu'un changement d'horaire apparaisse ?",
    answer:
      "Ça dépend de la fréquence à laquelle votre application de calendrier vérifie le lien (elle seule en décide, pas UMonsPlanning) — en général de quelques heures à une journée. Le lien reflète toujours l'horaire actuel dès que vous le consultez (via « Tester votre calendrier » par exemple).",
  },
  {
    question: 'Est-ce officiel ?',
    answer:
      "Non. UMonsPlanning est un outil personnel, non affilié à l'UMONS, qui lit les mêmes données que l'espace invité Hyperplanning/PRONOTE accessible publiquement. En cas de doute sur votre horaire, l'espace Hyperplanning officiel de l'UMONS reste la référence.",
  },
  {
    question: 'Que se passe-t-il si je change de formation ?',
    answer:
      "Le lien généré correspond à la formation et à la section choisies au moment de sa création. Si vous changez de formation, générez simplement un nouveau lien pour votre nouveau choix et ajoutez-le à votre calendrier — l'ancien lien continue de pointer vers l'ancienne formation tant que vous ne le retirez pas.",
  },
  {
    question: 'Mes données sont-elles collectées ?',
    answer:
      "Non. Aucun compte, aucun cookie, aucune donnée personnelle : le formulaire tourne entièrement dans votre navigateur, et le lien généré ne contient que votre formation et votre section (déjà publiques sur l'espace invité PRONOTE). Le seul chiffre conservé est un compteur global et anonyme du nombre de liens générés, affiché plus haut sur cette page.",
  },
  {
    question: 'Quelle différence avec Hyperplanning ?',
    answer:
      "Hyperplanning (PRONOTE) est le portail de l'UMONS où consulter votre horaire en vous connectant à chaque fois. UMonsPlanning ne remplace pas ce portail : il génère un lien à ajouter une seule fois dans votre propre application de calendrier, qui se synchronise ensuite automatiquement — vous n'avez plus besoin de revisiter le portail pour voir votre semaine.",
  },
];
