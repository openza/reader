# Release Notes

## Current Store Release

Openza Reader `1.1.0` is the current Microsoft Store release.

- Release tracking: https://github.com/openza/reader/issues/21
- Release notes: [release-notes/v1.1.0.md](release-notes/v1.1.0.md)
- GitHub Releases are source snapshots and release notes. Microsoft Store remains the trusted install and update channel.

## Planned Next Release

Openza Reader `1.2.0` is the next planned Microsoft Store update and replaces the WinUI application with the Avalonia implementation while preserving the Store identity and user-facing behavior.

- Release workflow and notes draft: [store-submission.md](store-submission.md)

## V1 Release Bar

The first public release is an MVP:

- MSIX package installs cleanly.
- `.md` and `.markdown` file associations work.
- File Explorer activation opens one window per file.
- Markdown rendering is safe and visually polished.
- TOC, search button, Preview/Raw/Side by side modes, recents, settings, zoom, copy selection, reload, external editor, and auto reload work.
- Unit tests pass.
- Manual smoke tests pass on Windows 10 22H2 and Windows 11.
- Release-blocking GitHub issues #13 and #16 are verified or explicitly deferred in release notes.

See [release-readiness-checklist.md](release-readiness-checklist.md) for the developer, smoke-test, and package validation checklist.

## Release Blockers From GitHub

- [#13](https://github.com/openza/reader/issues/13): Windows minimum version must match project, source manifest, generated package, README, website, and Store metadata.
- [#16](https://github.com/openza/reader/issues/16): release readiness checklist must be repeatable and current.

## Current Distribution

Openza Reader is live on the Microsoft Store:

- https://apps.microsoft.com/detail/9NNPMN0JSSW5?hl=en-us&gl=IN

Microsoft Store remains the trusted public install and update channel.

## Runtime And Platform Notes

- Supported OS target: Windows 10 22H2 (`10.0.19045.0`) or later, and Windows 11.
- V1 architecture: x64.
- The released `1.1.0` app uses WinUI 3 and WebView2. Windows 11 normally includes the WebView2 runtime; the released app surfaces an install/repair link if it is absent or broken.
- The planned `1.2.0` replacement is a self-contained Avalonia `win-x64` MSIX with a fully managed HTML renderer and no WebView2 runtime dependency.
- The production package keeps identity `Openza.OpenzaReader`, application ID `App`, Markdown file associations, settings migration, and Store update continuity.

## Out Of Scope

- Direct winget publishing
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
