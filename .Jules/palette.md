## 2024-11-20 - Dark Mode Focus Styles
**Learning:** In dark mode UI sections (e.g., bg-gray-900), default browser focus outlines are often invisible, and aria-current is necessary for active links.
**Action:** Always add explicit focus-visible styles with sufficient contrast (e.g., focus-visible:ring-white focus-visible:ring-offset-gray-900) and aria-current='page' to active links.
