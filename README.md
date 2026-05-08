# Openza Reader

[![CI](https://github.com/openza/reader/actions/workflows/ci.yml/badge.svg)](https://github.com/openza/reader/actions/workflows/ci.yml)

Openza Reader is a fast, read-only Markdown reader for Windows. It opens `.md` and `.markdown` files from File Explorer, renders them with local assets, and stays focused on reading rather than editing.

> Status: pre-release V1 work. The app is usable for local development, but public installers and winget packages are not published yet.

## V1 Scope

- Open Markdown files by double-click, drag and drop, or file picker
- Render GitHub-style Markdown with Markdig
- Disable raw HTML in Markdown input
- Highlight fenced code blocks with bundled Prism-compatible assets
- Show a table of contents from document headings
- Search with WebView2's native Find API
- Zoom, copy selected rendered text, reload, and open in the system editor
- Auto reload changed files with debounce
- Ship as MSIX with Markdown file associations

## Requirements

- Windows 10 22H2 or later
- Visual Studio 2026 with WinUI application development workload
- .NET 10 SDK
- Windows App SDK 2.0.x

The development build is configured as Windows App SDK self-contained so `dotnet run` can launch without first registering a machine-wide Windows App Runtime.

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

For packaged app build and MSIX signing, open the solution in Visual Studio and use the packaging tools, or run MSBuild from a Developer PowerShell that has the Windows App SDK workload installed.

## Security Posture

Markdown is rendered as untrusted content. Raw HTML is disabled, WebView2 host objects and web messages are disabled, app navigation is intercepted, and external links are launched through the Windows shell instead of inside the embedded WebView.

See [SECURITY.md](SECURITY.md) and [docs/security.md](docs/security.md) for reporting and implementation details.

## Contributing

Contributions are welcome while the project stays focused on the V1 reader scope. Start with [CONTRIBUTING.md](CONTRIBUTING.md), and please include test commands and screenshots for UI changes.

## License

Openza Reader is licensed under the [MIT License](LICENSE).

Third-party dependency and bundled asset notes are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
