## 2023-10-27 - Explicit focus-visible on dark backgrounds
**Learning:** Default browser focus rings (`outline`) often lack sufficient contrast against dark background sections (e.g., `bg-gray-900`), making keyboard navigation invisible or difficult to follow for users relying on it.
**Action:** Always explicitly define `focus-visible` styles with a high-contrast ring (e.g., `focus-visible:ring-white focus-visible:ring-offset-gray-900`) for interactive elements located in dark mode UI sections to ensure focus indicators are clearly visible and accessible.
