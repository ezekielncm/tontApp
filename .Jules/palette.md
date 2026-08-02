## 2025-01-07 - Improved Sidebar Navigation Accessibility in Dark Mode
**Learning:** In dark mode UI sections (e.g., `bg-gray-900`), default browser focus outlines are often invisible. Additionally, screen readers need explicit cues to know which navigation link represents the current page.
**Action:** Always explicitly define `focus-visible` styles with sufficient contrast (e.g., `focus-visible:ring-white focus-visible:ring-offset-gray-900`) for interactive elements in dark mode. Always add `aria-current="page"` to active navigation links.
