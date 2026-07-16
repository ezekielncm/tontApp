
## 2025-02-09 - Dark Mode Sidebar Keyboard Focus and Semantics
**Learning:** In dark UI sections like `bg-gray-900`, the default browser focus ring becomes invisible, harming keyboard accessibility. Furthermore, active navigation links need `aria-current="page"` to properly convey state to screen readers.
**Action:** Always explicitly define `focus-visible:ring-white focus-visible:ring-offset-gray-900` for interactive elements in dark modes, and append `aria-current="page"` to active nav links.
