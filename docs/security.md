# Security Model

Openza Reader renders local Markdown files that may come from downloads, repositories, or email attachments. The renderer must assume hostile input.

## V1 Rules

- Raw HTML parsing is disabled in Markdig.
- Generated HTML is wrapped in an app-owned document shell.
- Bundled scripts are limited to Prism-compatible code highlighting and small reader behavior.
- The Windows app disables WebView2 host objects and web messages.
- The Avalonia app does not register WebView host objects or message handlers.
- JavaScript and script dialogs from document content are blocked by disabling raw HTML and by using a restrictive generated shell.
- Navigation is intercepted:
  - `#heading` anchors stay inside the document.
  - `http`, `https`, and `mailto` open externally.
  - `javascript:`, arbitrary `file:`, and unknown schemes are blocked.
- Local images resolve relative to the opened Markdown file.
- Remote images are controlled by the user setting and can be blocked for privacy.
- If the platform WebView runtime is missing or broken, preview rendering cannot start. Windows uses WebView2; the Ubuntu prototype uses the system WebKitGTK 4.1 runtime through Avalonia's official WebView package.

## Future HTML Compatibility

If raw HTML support is added later, it must be allowlist-sanitized before entering the WebView. The allowlist should start with README-safe tags such as `details`, `summary`, `kbd`, `br`, `sub`, `sup`, and restricted `img` attributes. It must block scripts, event handlers, forms, iframes, embedded objects, and arbitrary styles.
