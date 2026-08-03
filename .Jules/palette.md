## 2026-08-03 - Sidebar Accessibility Enhancement
**Learning:** In dark mode UI sections (e.g., bg-gray-900), default browser focus outlines are often invisible. Interactive elements need explicitly defined `focus-visible` styles with sufficient contrast against the dark background to maintain keyboard accessibility.
**Action:** Always verify keyboard navigation in dark-themed components and apply explicit `focus-visible` styles (e.g., `focus-visible:ring-white focus-visible:ring-offset-gray-900`) when using Tailwind CSS.
