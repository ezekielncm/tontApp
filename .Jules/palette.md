## 2025-07-25 - Dark Mode Keyboard Focus Visibility
**Learning:** Default browser focus outlines can become invisible or have very poor contrast against dark backgrounds (like `bg-gray-900`), making keyboard navigation impossible to track for non-mouse users in those areas.
**Action:** When styling interactive elements in dark mode sections, always explicitly define `focus-visible` styles with sufficient contrast (e.g., using a white ring with an offset matching the dark background: `focus-visible:ring-white focus-visible:ring-offset-gray-900`).

## 2025-07-25 - Semantic Active Navigation States
**Learning:** Simply visually indicating the active page in a navigation menu (e.g., by changing background color) is not enough for screen readers. They need a semantic attribute to convey this state.
**Action:** Always add `aria-current="page"` to the active link in navigation menus to ensure proper accessibility for screen reader users.
