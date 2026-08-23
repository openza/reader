# Openza Reader

[![CI](https://github.com/openza/reader/actions/workflows/ci.yml/badge.svg)](https://github.com/openza/reader/actions/workflows/ci.yml)

Openza Reader is a read-only Markdown reader. The current Store release uses WinUI 3 and WebView2. Its Avalonia replacement is being prepared for Windows and Linux with the same product behavior and portable rendering/security core, but without a browser-runtime dependency.

> Status: Openza Reader is live on the Microsoft Store. Developers can also build and run it from source.

| App | Status | UI and renderer |
| --- | --- | --- |
| `Openza.Reader` | Released Windows app | WinUI 3 and Microsoft WebView2 |
| `Openza.Reader.Avalonia` | Windows replacement candidate and Linux prototype | Avalonia 12 and the fully managed MIT-licensed Avalonia.HtmlRenderer |

User guide: [solanky.dev/openza/reader](https://solanky.dev/openza/reader/)

Install: [Microsoft Store](https://apps.microsoft.com/detail/9NNPMN0JSSW5?hl=en-us&gl=IN)

Current Store release: `1.1.0`.

Next planned Store update: `1.2.0`.

## Released Windows V1 Scope

- Open Markdown files by double-click, drag and drop, or file picker
- Render GitHub-style Markdown with Markdig
- Disable raw HTML in Markdown input
- Highlight fenced code blocks with bundled Prism-compatible assets
- Show a table of contents from document headings
- Search with an explicit toolbar button backed by WebView2's native Find API
- Switch between Preview, Raw, and Side by side read-only view modes
- Choose reader theme, remote image policy, and default view mode in Settings
- Open recent files from the empty state
- Zoom, copy selected text, reload, and open the current file with an installed Markdown editor through Windows
- Auto reload changed files with debounce
- Ship as MSIX with Markdown file associations

## Requirements

- Windows 10 22H2 or later
- Visual Studio 2026 with WinUI application development workload
- .NET 10 SDK
- Windows App SDK 2.0.x

The current WinUI development build is Windows App SDK self-contained. The Avalonia replacement is also self-contained for Windows and uses the same Store identity, `App` application ID, package logos, minimum Windows version, and Markdown associations in its production MSIX. On first packaged launch it migrates the WinUI reader theme, default view, remote-image policy, document-stat preference, and recent-file list into its cross-platform settings store.

You can install the required WinUI development workload with Microsoft's winget configuration:

```powershell
winget configure -f https://aka.ms/winui-config
```

### Ubuntu prototype

The Avalonia prototype requires only the .NET 10 SDK on Ubuntu. Its preview is rendered by the fully managed, MIT-licensed Avalonia.HtmlRenderer control, so it does not require WebView2, WebKitGTK, WPE WebKit, or another browser runtime.

```bash
sudo apt update
sudo apt install dotnet-sdk-10.0
```

Build, test, and run it with:

```bash
dotnet restore src/Openza.Reader.Avalonia/Openza.Reader.Avalonia.csproj
dotnet restore src/Openza.Reader.Tests/Openza.Reader.Tests.csproj
dotnet build src/Openza.Reader.Avalonia/Openza.Reader.Avalonia.csproj -c Debug --no-restore
dotnet test src/Openza.Reader.Tests/Openza.Reader.Tests.csproj -c Debug --no-restore
dotnet run --project src/Openza.Reader.Avalonia/Openza.Reader.Avalonia.csproj -- README.md
```

The prototype supports opening and dropping Markdown files, Preview/Raw/Side-by-side modes, contents navigation, search, zoom, copy, focus mode, external editor launch, integrated settings/about/recent-file workspaces, and debounced reloads. Search is case-insensitive and uses a script-free managed highlighter in Preview and Side-by-side modes, with raw-text selection in Raw mode. Fenced code is rendered as styled code rather than Prism-highlighted tokens. Linux packaging and KDE validation are not complete yet.

## Development

```powershell
dotnet restore src/Openza.Reader.Tests/Openza.Reader.Tests.csproj
dotnet test src/Openza.Reader.Tests/Openza.Reader.Tests.csproj -c Release --no-restore
dotnet build src/Openza.Reader/Openza.Reader.csproj -c Release --no-restore
```

Build the Avalonia Windows candidate and its unsigned development MSIX with:

```powershell
dotnet restore src/Openza.Reader.Avalonia/Openza.Reader.Avalonia.csproj -r win-x64
dotnet build src/Openza.Reader.Avalonia/Openza.Reader.Avalonia.csproj -c Release --no-restore
.\eng\package-windows.ps1 -Architecture x64 -Version 1.2.0.0
```

Use `-Store` to create an unsigned package with the existing production Store identity for Partner Center. The default development package uses `Openza.OpenzaReader.Avalonia.Dev`, so local validation cannot replace the installed Store app. See [docs/store-submission.md](docs/store-submission.md) for the release and smoke-test workflow.

GitHub Releases record source snapshots and release notes. Microsoft Store remains the trusted public install and update channel.

## Security Posture

Markdown is rendered as untrusted content and raw HTML is disabled in both apps. The released WinUI app uses an app-owned document shell with a restrictive content-security policy and WebView2 navigation interception. The Avalonia prototype passes Markdig-generated HTML directly to a managed renderer with no browser runtime or scripts; link clicks are intercepted so only in-document anchors stay inside while `http`, `https`, and `mailto` open externally. Remote images can be blocked from Settings.

See [SECURITY.md](SECURITY.md) and [docs/security.md](docs/security.md) for reporting and implementation details.

## Contributing

Contributions are welcome while the project stays focused on the V1 reader scope. Start with [CONTRIBUTING.md](CONTRIBUTING.md), and please include test commands and screenshots for UI changes.

## License

The source code and documentation are available under the [MIT License](LICENSE). Openza names, logos, and official app icons are reserved brand assets; see [BRAND.md](BRAND.md).

Third-party dependency and bundled asset notes are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
