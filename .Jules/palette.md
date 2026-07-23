## 2026-07-23 - Missing Keyboard Focus Styles in Dark Mode UI
**Learning:** Default browser focus rings are often invisible or have insufficient contrast on dark backgrounds (e.g., `bg-gray-900`), making keyboard navigation impossible to track for non-mouse users.
**Action:** Always explicitly define `focus-visible` styles with a contrasting ring color (e.g., `focus-visible:ring-white`) and an offset matching the background (e.g., `focus-visible:ring-offset-gray-900`) in dark mode sections.
