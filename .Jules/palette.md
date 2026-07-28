
## 2026-07-28 - Dark mode focus visibility and active navigation links
**Learning:** Default browser focus outlines are often invisible against dark backgrounds (like `bg-gray-900`), making keyboard navigation very difficult for users. Additionally, relying solely on visual cues (like a different background color) for the active navigation link is insufficient for screen reader users.
**Action:** Always explicitly define `focus-visible` styles with sufficient contrast (e.g., `focus-visible:ring-white focus-visible:ring-offset-gray-900`) for interactive elements in dark mode sections. Always add `aria-current="page"` to the active navigation link so assistive technologies can properly identify the current page context.
