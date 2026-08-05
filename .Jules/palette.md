## 2026-08-05 - Ensure clear focus styles on dark background
**Learning:** Default browser focus rings are often completely invisible against dark backgrounds like `bg-gray-900`.
**Action:** When adding interactive elements to dark UI sections, always explicitly define `focus-visible` ring styles (e.g., `focus-visible:ring-white focus-visible:ring-offset-gray-900`) rather than relying on browser defaults.
