## 2024-05-24 - Dark Mode Focus Styles and Semantic Active States
**Learning:** In dark mode UI sections (e.g., bg-gray-900), the default browser focus outlines are often invisible. Additionally, active navigation links should clearly convey their state to screen readers.
**Action:** Always explicitly define `focus-visible` styles with sufficient contrast (e.g., `focus-visible:ring-white focus-visible:ring-offset-gray-900`) for interactive elements in dark mode. Always add `aria-current="page"` to active navigation links to ensure proper screen reader support for the current location.
