## 2026-08-11 - [Add High-Contrast Focus Visible Styles in Dark Mode]
**Learning:** When using dark mode sections in the UI (e.g., bg-gray-900), default browser focus outlines are often invisible or have insufficient contrast. You must explicitly define `focus-visible` styles (e.g., `focus-visible:ring-white focus-visible:ring-offset-gray-900`) to ensure interactive elements are accessible via keyboard navigation.
**Action:** Always test keyboard navigation in dark mode components and explicitly apply high-contrast focus rings to all interactive elements.
