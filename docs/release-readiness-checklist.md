# Openza Reader Release Readiness Checklist

Use this checklist before a public Store update or GitHub release. It covers automated validation, practical manual smoke testing, and package checks that can be completed by the maintainer.

## Developer Validation

- Confirm release blockers are closed or explicitly deferred:
  - [ ] #13: Windows minimum version matches project, source manifest, generated package, README, website, and Store copy.
  - [ ] #16: this checklist is current for the release.
- Run restore:

```powershell
dotnet restore src/Openza.Reader.Tests/Openza.Reader.Tests.csproj
```

- Run tests:

```powershell
dotnet test src/Openza.Reader.Tests/Openza.Reader.Tests.csproj -c Release --no-restore
```

- Run app build:

```powershell
dotnet build src/Openza.Reader/Openza.Reader.csproj -c Release --no-restore
```

- Run public hygiene scan:

```powershell
gitleaks detect --source . --verbose
```

- Inspect the built runtime config. A self-contained package should not require a separate `Microsoft.NETCore.App` install prompt for normal users.
- Inspect the generated `AppxManifest.xml` or MSIX manifest and confirm `MinVersion="10.0.19045.0"`.
- Confirm package size impact from bundling the .NET runtime and record it in release notes. The 2026-05-23 Release x64 build layout was about 217 MiB before MSIX packaging.
- Generate/update screenshots after the final UI is stable.
- Confirm README, website docs, Store copy, release notes, privacy, security, and third-party notices match shipped behavior.

## Manual App Smoke Test

- Open the app from Start and from a Markdown file association.
- Open `.md` and `.markdown` files by file picker, drag/drop, and File Explorer.
- Verify wide tables scroll horizontally without clipping the document.
- Verify Search button and `Ctrl+F` use native WebView2 find.
- Verify Preview, Raw, and Side by side read-only modes.
- Verify TOC behavior in Preview/Side by side and hidden/disabled behavior in Raw.
- Verify recent files open and clear correctly.
- Verify settings persist after restart.
- Verify Editor opens the Windows app picker for installed Markdown-capable applications.
- Verify System, Light, Dark, Sepia, and Windows High Contrast behavior.
- Verify focus mode hides reader chrome, hides TOC, enters fullscreen, and exits with the visible Exit focus button, Esc, and F11.
- Verify zoom, copy, reload, auto reload, local images, remote image policy, and link interception.
- Verify narrow and portrait windows have no clipped labels, title/stat overlap, or broken settings/about layouts.

## Package Validation

- Create the Release x64 package or Store upload package.
- Install and launch the package on the maintainer's supported Windows machine.
- Confirm Markdown preview renders normally.
- Confirm install, update, and uninstall work cleanly.
- Confirm Store package signing and identity are correct.
- Confirm release notes mention runtime/package-size impact when relevant.
