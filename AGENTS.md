# Openza Reader Agent Notes

Also follow the shared Openza guidance in `../AGENTS.md`. Keep this file limited to Reader-specific constraints and commands.

Openza Reader is a Windows-first, read-only Markdown reader.

## Product Constraints
- Keep the app read-only. Do not add editing, sync, accounts, plugins, AI features, tabs, export, Mermaid, or math rendering in V1.
- Treat Markdown files as untrusted input.
- Keep remote assets explicit and local app assets bundled; no CDN dependencies.
- Preserve the native Windows feel with WinUI 3 controls and system theme behavior.

## Desktop UI Direction
- Treat Openza Reader as a desktop document app, not a mobile-style centered card app. Use the available window space for start, recents, settings, about, and document-management surfaces.
- Avoid narrow centered panels for substantial surfaces. Prefer desktop-width layouts with clear regions, such as a left identity/action column and a flexible main content region, or a full settings/about page when content has multiple sections.
- Use `ContentDialog` only for small blocking decisions or compact information. Do not put multi-section settings, large link collections, recent-file lists, or other page-like experiences into cramped modal layouts.
- Settings should follow WinUI Gallery-style settings layouts: grouped sections, card-like rows, icons where helpful, descriptions, right-aligned controls on wide layouts, and stacked controls on narrow layouts.
- About should feel like a native desktop app identity surface: app icon/name/version, concise purpose, and structured icon rows for documentation, source, issues, security, license, and notices. Avoid loose hyperlink lists.
- Empty states should include a clear primary action and use the rest of the page for useful recovery or continuation content, especially recent files.
- Reading surfaces may center document controls around the reading column when it improves calm focus. App-management workspaces such as Settings, About, and Recents should remain left-aligned and structured like native desktop pages.
- Before finalizing UI changes, check the same surface at full desktop width, portrait/narrow width, and High Contrast. Watch for wasted space, clipped text, duplicated titles, hidden exits, and toolbar/stat/title overlap.

## Technical Defaults
- .NET 10 LTS
- Windows App SDK 2.0.x
- WinUI 3
- WebView2
- Markdig
- CommunityToolkit.Mvvm
- MSIX-first packaging

## Project Structure
- `src/Openza.Reader/` holds the WinUI app, shell, WebView2 host, file activation, and Windows-specific services.
- `src/Openza.Reader.Core/` holds Markdown rendering, HTML generation, TOC extraction, and security-sensitive content handling.
- `src/Openza.Reader.Tests/` holds renderer, navigation, and security behavior tests.
- Keep package outputs, signing material, generated screenshots, and local test files out of git.

## Security And Public Hygiene
- This is a public open-source repo. Do not commit local Markdown samples with private data, package outputs, certificates, Store-private metadata, logs, or user-specific paths.
- Markdown is untrusted input. Keep raw HTML disabled in V1 unless the product decision changes with sanitizer tests.
- Keep renderer security tests close to any Markdown/WebView2 navigation change.
- Run `gitleaks detect --source . --verbose` before commit-readiness, PRs, or public-release checks.

## Verification
- Run `dotnet restore src/Openza.Reader.Tests/Openza.Reader.Tests.csproj`.
- Run `dotnet test src/Openza.Reader.Tests/Openza.Reader.Tests.csproj -c Release --no-restore`.
- Run `dotnet build src/Openza.Reader/Openza.Reader.csproj -c Release --no-restore`.
- Build/package the MSIX from Visual Studio or with MSBuild once signing details are configured.
