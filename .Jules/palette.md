
## 2024-10-24 - Accessibility focus styles for dark themes
**Learning:** Default browser focus rings (usually blue or a faint outline) can become completely invisible against dark backgrounds (like the `bg-gray-900` used in the Sidebar).
**Action:** Always manually define `focus-visible` styles with a high-contrast ring and offset (e.g., `focus-visible:ring-white focus-visible:ring-offset-2 focus-visible:ring-offset-gray-900`) for interactive elements in dark-themed components to ensure keyboard navigability.
