# Openza Reader

[![CI](https://github.com/openza/reader/actions/workflows/ci.yml/badge.svg)](https://github.com/openza/reader/actions/workflows/ci.yml)

Openza Reader is a native Markdown reader for Windows, built with WinUI 3, WebView2, and MSIX packaging. It opens `.md` and `.markdown` files from File Explorer, renders them with local assets, and stays focused on a modern Windows reading experience.

> Status: Openza Reader is live on the Microsoft Store. Developers can also build and run it from source.

User guide: [solanky.dev/openza/reader](https://solanky.dev/openza/reader/)

Install: [Microsoft Store](https://apps.microsoft.com/detail/9NNPMN0JSSW5?hl=en-us&gl=IN)

Current Store release: `1.1.0`.

Next planned Store update: `1.1.1`.

## V1 Scope

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

The development build is configured as Windows App SDK self-contained so `dotnet run` can launch without first registering a machine-wide Windows App Runtime. The app manifest and package minimum target Windows 10 22H2 (`10.0.19045.0`), and the app project produces self-contained `win-x64` build output for release validation.

You can install the required WinUI development workload with Microsoft's winget configuration:

```powershell
winget configure -f https://aka.ms/winui-config
```

## Development

```powershell
dotnet restore src/Openza.Reader.Tests/Openza.Reader.Tests.csproj
dotnet test src/Openza.Reader.Tests/Openza.Reader.Tests.csproj -c Release --no-restore
dotnet build src/Openza.Reader/Openza.Reader.csproj -c Release --no-restore
```

For Store packaging, open the solution in Visual Studio and use **Publish > Create App Packages** with the Microsoft Store flow. See [docs/store-submission.md](docs/store-submission.md) for release and Store maintenance notes.

GitHub Releases record source snapshots and release notes. Microsoft Store remains the trusted public install and update channel.

## Security Posture

Markdown is rendered as untrusted content. Raw HTML is disabled, WebView2 host objects and web messages are disabled, app navigation is intercepted, and external links are launched through the Windows shell instead of inside the embedded WebView. Remote images can be blocked from Settings.

See [SECURITY.md](SECURITY.md) and [docs/security.md](docs/security.md) for reporting and implementation details.

## Contributing

Contributions are welcome while the project stays focused on the V1 reader scope. Start with [CONTRIBUTING.md](CONTRIBUTING.md), and please include test commands and screenshots for UI changes.

## License

The source code and documentation are available under the [MIT License](LICENSE). Openza names, logos, and official app icons are reserved brand assets; see [BRAND.md](BRAND.md).

Third-party dependency and bundled asset notes are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
