## 2025-07-27 - Sidebar Accessibility Improvements
**Learning:** Screen reader users need context about which navigation link corresponds to the active page, and keyboard users require clear focus indicators against dark backgrounds (where default browser outlines often fail).
**Action:** Always add `aria-current="page"` to active navigation links and explicitly define high-contrast `focus-visible` styles (e.g., `focus-visible:ring-white focus-visible:ring-offset-gray-900`) on interactive elements in dark UI sections.
