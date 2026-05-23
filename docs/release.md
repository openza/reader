# Release Notes

## V1 Release Bar

The first public release is an MVP:

- MSIX package installs cleanly.
- `.md` and `.markdown` file associations work.
- File Explorer activation opens one window per file.
- Markdown rendering is safe and visually polished.
- TOC, search button, Preview/Raw/Side by side modes, recents, settings, zoom, copy selection, reload, external editor, and auto reload work.
- Unit tests pass.
- Manual smoke tests pass on Windows 10 22H2 and Windows 11.
- Release-blocking GitHub issues #12, #13, #14, and #16 are verified or explicitly deferred in release notes.

See [release-readiness-checklist.md](release-readiness-checklist.md) for the full developer and user-install validation checklist.

## Release Blockers From GitHub

- [#12](https://github.com/openza/reader/issues/12): package should be self-contained for the .NET runtime so normal users do not see a separate .NET 10 runtime prompt.
- [#13](https://github.com/openza/reader/issues/13): Windows minimum version must match project, source manifest, generated package, README, website, and Store metadata.
- [#14](https://github.com/openza/reader/issues/14): missing or broken WebView2 Runtime behavior must be verified and user-facing.
- [#16](https://github.com/openza/reader/issues/16): release readiness checklist must be repeatable and current.

## Current Distribution

Openza Reader is live on the Microsoft Store:

- https://apps.microsoft.com/detail/9NNPMN0JSSW5?hl=en-us&gl=IN

Microsoft Store remains the trusted public install and update channel for V1.

## Runtime And Platform Notes

- Supported OS target: Windows 10 22H2 (`10.0.19045.0`) or later, and Windows 11.
- V1 architecture: x64.
- The app project is configured for Windows App SDK self-contained deployment and .NET self-contained build output for `win-x64`.
- The 2026-05-23 Release x64 build output used `includedFrameworks` for `Microsoft.NETCore.App` in `Openza.Reader.runtimeconfig.json` and was about 217 MiB before MSIX packaging.
- Final package validation must still install on a clean machine or VM without .NET 10.
- Openza Reader uses WebView2. Windows 11 normally includes the runtime; if WebView2 is absent or broken, the app surfaces a WebView2 Runtime install/repair link.

## Out Of Scope

- Direct winget publishing before Store availability has been verified
- Direct-download self-signed installer distribution
- Paid code signing certificate automation
- Tabs
- Editing
- Export
- Mermaid
- Math rendering
- Plugins
- Sync or accounts
- AI features
