## 2025-03-05 - Ajouter aria-current et focus-visible à la barre de navigation
**Learning:** Les liens de navigation actifs manquent d'indication pour les lecteurs d'écran, et les éléments interactifs n'ont pas d'état de focus clair pour la navigation au clavier.
**Action:** Toujours ajouter `aria-current="page"` au lien actif de la navigation, et utiliser les classes utilitaires de type `focus-visible` (ex: `focus-visible:outline-none focus-visible:ring-2`) pour améliorer la visibilité du focus clavier sans impacter les utilisateurs de souris.
