## 2025-01-28 - Active Link Accessibility and Dark Mode Focus Rings
**Learning:** In the dark Sidebar (`bg-gray-900`), default browser focus rings are invisible or have poor contrast. Also, visually distinguishing the active link isn't enough for screen readers.
**Action:** Always add `aria-current="page"` to active navigation links. In dark mode UI sections, explicitly define `focus-visible` styles with sufficient contrast (e.g., `focus-visible:ring-white focus-visible:ring-offset-gray-900`) for interactive elements.
