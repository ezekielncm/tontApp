## 2026-07-29 - Accessibility & Focus states on Dark Backgrounds
**Learning:** The default browser outline (which is often dark or blue depending on the browser) for focused items can become nearly invisible against dark UI backgrounds (like bg-gray-900). Also, screen readers need to know which link represents the current page.
**Action:** When working on dark mode elements (bg-gray-800, bg-gray-900), explicitly define `focus-visible` styles (e.g., `focus-visible:ring-white`) to ensure sufficient contrast. Always apply `aria-current="page"` on active navigation links.
