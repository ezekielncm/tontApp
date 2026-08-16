## 2024-08-16 - Explicit Focus Indicators for Dark Mode Components
**Learning:** Default browser focus rings are often invisible against dark backgrounds (like `bg-gray-900`), severely hindering keyboard navigation for users relying on visual focus.
**Action:** Always explicitly define `focus-visible` styles with sufficient contrast (e.g., `focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white focus-visible:ring-offset-2 focus-visible:ring-offset-gray-900`) for interactive elements in dark UI sections.
