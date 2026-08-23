# Security Model

Openza Reader renders local Markdown files that may come from downloads, repositories, or email attachments. The renderer must assume hostile input.

## V1 Rules

- Raw HTML parsing is disabled in Markdig.
- The released Windows app wraps generated HTML in an app-owned document shell with a restrictive content-security policy. Its bundled scripts are limited to Prism-compatible code highlighting and small reader behavior.
- The Windows app disables WebView2 host objects and web messages.
- The Avalonia prototype passes the Markdig-generated HTML fragment directly to Avalonia.HtmlRenderer, a fully managed renderer. It does not host a browser runtime or execute JavaScript.
- Navigation is intercepted in both apps:
  - `#heading` anchors stay inside the document.
  - `http`, `https`, and `mailto` open externally.
  - `javascript:`, arbitrary `file:`, and unknown schemes are blocked.
- Local images resolve relative to the opened Markdown file.
- Remote images are controlled by the user setting and can be blocked for privacy.
- The released Windows preview requires Microsoft WebView2. The Avalonia prototype does not require WebView2, WebKitGTK, WPE WebKit, or another browser runtime.

## Future HTML Compatibility

If raw HTML support is added later, it must be allowlist-sanitized before entering either renderer. The allowlist should start with README-safe tags such as `details`, `summary`, `kbd`, `br`, `sub`, `sup`, and restricted `img` attributes. It must block scripts, event handlers, forms, iframes, embedded objects, and arbitrary styles.
