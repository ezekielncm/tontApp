## 2024-05-23 - Login Form Accessibility and Loading States
**Learning:** Found that the login form lacked ARIA attributes for error messages and clear visual/accessible loading states. Users relying on screen readers wouldn't know when a login failed without navigating back through the DOM, and visual users might click multiple times if they didn't realize the login was in progress.
**Action:** Always add `role="alert"` and `aria-live="assertive"` to form error messages. Always provide visual feedback (like a spinner) and `aria-busy` for async operations on submit buttons.
