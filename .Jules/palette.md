## 2024-05-18 - [Dark Mode Focus and Active Links]
**Learning:** In dark mode sections (e.g., bg-gray-900), default browser focus outlines are invisible. Explicit `focus-visible` styles with ring offset are required. Also, active links need `aria-current="page"`.
**Action:** Always add explicit `focus-visible` ring styles with sufficient contrast (e.g., `ring-white ring-offset-gray-900`) for interactive elements in dark backgrounds, and `aria-current` to navigation.
