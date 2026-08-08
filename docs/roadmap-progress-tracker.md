# Openza Reader Roadmap Progress Tracker

Last updated: 2026-05-23

Use this document as the living tracker for the roadmap batches implemented from the Openza Tasks export and Apostrophe research. Keep adding progress notes here after future implementation, QA, Store work, or product decisions.

## Status Legend

- `Done`: implemented and automated verification passed where applicable.
- `Partial`: implemented partly, or code is present but manual/release verification remains.
- `Pending`: not implemented yet.
- `Deferred`: intentionally out of scope for the current plan.

## Current Snapshot

| Batch | Status | Notes |
| --- | --- | --- |
| Batch 1: Fix Reading Basics First | Done | Wide table CSS, Search button, and related reading basics are implemented. Automated build/test passed; final manual smoke QA remains before release. |
| Batch 2: Preview / Raw / Side-By-Side Modes | Done | View modes are implemented as a compact toolbar View menu instead of the earlier full-width SelectorBar strip. |
| Batch 3: Settings, About, External Editor, Recents | Done | Desktop in-app settings/about workspaces, Windows editor picker, polished recents, theme/default view/remote image settings are implemented. |
| Batch 4: Reader Polish From Apostrophe Research | Done | Themes, stats, immersive focus mode with visible exit cue, remote image policy, centered reader toolbar, and improved narrow behavior are implemented. Final High Contrast/narrow manual QA remains before release. |
| Batch 5: Public And Store Polish | Partial | README, docs, website docs, Store listing copy, and the release checklist are in place. Screenshots/assets and final release hygiene remain. |
| Deferred: Tabs | Deferred | Tabs remain out of V1 scope unless product decision changes. |

## Batch Details

### Batch 1: Fix Reading Basics First

Status: Done

- Done: rendered content width and table behavior improved in `src/Openza.Reader/Assets/reader.css`.
- Done: explicit Search button added to the toolbar.
- Done: Search button uses WebView2 native Find behavior.
- Done: zoom, TOC, reload, local/remote images, and link interception were preserved through implementation passes.
- Release QA: wide tables, zoom, TOC, reload, local/remote images, and link interception still need final manual smoke verification.

### Batch 2: Preview / Raw / Side-By-Side Modes

Status: Done

- Done: Preview, Raw, and Side by side view modes added.
- Done: mode switch moved to a compact toolbar View menu using checked `RadioMenuFlyoutItem`s.
- Done: Preview remains default.
- Done: Raw view is selectable, read-only Markdown in a monospaced `TextBox`.
- Done: Side by side shows raw Markdown and preview in equal panes.
- Done: TOC is disabled/hidden in Raw and available in Preview/Side by side.
- Done: copy behavior prefers focused/selected raw text, otherwise selected preview text.

### Batch 3: Settings, About, External Editor, Recents

Status: Done

- Done: Settings moved from a modal dialog to a desktop-width in-app workspace with WinUI Gallery-style rows and immediate persistence.
- Done: Settings include default view mode, reader theme, remote image policy, recents controls, and document stats toggle.
- Done: Editor opens the Windows app picker so the user can choose from installed Markdown-capable applications instead of fixed defaults.
- Done: recent files are local-only and shown on a desktop-width empty-state panel with file icons, folder paths, clear action, and long-text trimming; a toolbar Recent command opens the same recents surface while a document is open.
- Done: About moved from a modal dialog to a desktop-width in-app workspace with app identity, icon link rows for docs/source/issues/security/notices, version, and license/security note.

### Batch 4: Reader Polish From Apostrophe Research

Status: Done

- Done: reader themes: System, Light, Dark, Sepia.
- Done: document stats: word count, estimated read time, heading count.
- Done: immersive focus reading mode enters fullscreen, hides title/toolbar/info/TOC/workspace surfaces, shows a visible Exit focus button, and supports Esc/F11 to exit.
- Done: remote image policy can block remote images and tightens generated CSP.
- Done: TOC hides on narrow windows and the view mode control no longer consumes a full row.
- Done: reader toolbar is centered for the document reading surface; Settings/About/Recents remain left-aligned desktop workspaces.
- Done: toolbar order reviewed and adjusted so Focus sits with reader/view controls.
- Release QA: Windows High Contrast, narrow toolbar behavior, TOC behavior on small windows, and visual polish across real documents still need final manual smoke verification.

### Batch 5: Public And Store Polish

Status: Partial

- Done: README updated for view modes, search, settings, recents, privacy, and themes.
- Done: website docs updated for view modes, security model, rendering, reading workflow, architecture, and Store copy.
- Done: Store listing text updated in docs.
- Done: release readiness checklist added in `docs/release-readiness-checklist.md`.
- Partial: critical GitHub release issues are included in this PR/release gate:
  - #13: package minimum now aligns with the source manifest/docs at Windows 10 22H2 / `10.0.19045.0`; generated build manifest inspection passed with `MaxVersionTested="10.0.26100.0"`.
  - #16: checklist exists and covers developer, smoke-test, and package validation.
- Pending: regenerate Store/listing screenshots after UI stabilizes.
- Pending: verify website copy against the actual shipped behavior after final QA.
- Done: `gitleaks detect --source . --verbose` passed outside the sandbox after PATH verification.

## Verification Log

2026-05-22:

- Passed: `dotnet restore src/Openza.Reader.Tests/Openza.Reader.Tests.csproj`
- Passed: `dotnet test src/Openza.Reader.Tests/Openza.Reader.Tests.csproj -c Release --no-restore`
- Passed: `dotnet build src/Openza.Reader/Openza.Reader.csproj -c Release --no-restore`
- Passed: `ASTRO_TELEMETRY_DISABLED=1 npm run build` from `website/`
- Passed: `git diff --check` with only CRLF warnings
- Blocked: `gitleaks detect --source . --verbose` because `gitleaks` was not found

2026-05-23:

- Passed: `dotnet restore src/Openza.Reader.Tests/Openza.Reader.Tests.csproj`
- Passed: `dotnet test src/Openza.Reader.Tests/Openza.Reader.Tests.csproj -c Release --no-restore`
- Passed: `dotnet build src/Openza.Reader/Openza.Reader.csproj -c Release --no-restore`
- Passed: generated build manifest has `MinVersion="10.0.19045.0"` and `MaxVersionTested="10.0.26100.0"`.
- Passed: runtimeconfig uses `includedFrameworks` for `Microsoft.NETCore.App`, indicating self-contained .NET build output.
- Passed: Release x64 build layout measured about 217 MiB before MSIX packaging.
- Passed: `gitleaks detect --source . --verbose` scanned 23 commits and found no leaks.
- Passed: `ASTRO_TELEMETRY_DISABLED=1 npm run build` from `website/`.

## Manual QA Still Needed

- Wide tables render with horizontal scrolling and no document clipping.
- Search button and `Ctrl+F` both use native find flow.
- Raw view is read-only and text selection/copy works.
- Side by side mode uses equal panes with independent scrolling.
- TOC works in Preview and Side by side, and stays hidden/disabled in Raw.
- Recent files open correctly and can be cleared.
- Settings persist after restart.
- Editor button shows installed Windows app choices for the current Markdown file.
- Theme switching works for System, Light, Dark, and Sepia.
- Windows High Contrast remains readable.
- Narrow layout has no toolbar/stat/title overlap.
- Drag/drop, reload, auto reload, local images, remote image policy, and link interception still work.

## Future Progress Notes

Append dated notes below.

- 2026-05-22: Initial tracker created after roadmap implementation pass.
- 2026-05-23: Corrected Editor behavior to use the Windows app picker, polished Recent files, moved Settings/About to desktop in-app workspaces, and added visible/Esc exit affordances for Focus mode.
- 2026-05-23: Centered the reader toolbar, fixed toolbar label persistence, added Recent command while documents are open, improved Settings/About alignment, and moved Focus into the reader/view command group.
- 2026-05-23: Added GitHub release blockers #12, #13, #14, and #16 to the release gate; aligned package minimum Windows version to 10.0.19045.0; configured and verified .NET self-contained build output; added WebView2 Runtime unavailable error; added release readiness checklist; passed gitleaks.
- 2026-05-23: Fixed review findings for focus-mode window restoration, narrow Settings row layout, and Editor launch failure handling; Release build and tests passed.
- 2026-08-08: Removed clean-machine .NET and missing-WebView2 environment checks from the release process because they are not sustainable for this solo-maintained side project. Local package smoke testing remains in scope.
