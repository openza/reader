# Openza Reader Agent Notes

Also follow the shared Openza guidance in `../AGENTS.md`. Keep this file limited to Reader-specific constraints and commands.

Openza Reader is a Windows-first, read-only Markdown reader.

## Product Constraints
- Keep the app read-only. Do not add editing, sync, accounts, plugins, AI features, tabs, export, Mermaid, or math rendering in V1.
- Treat Markdown files as untrusted input.
- Keep remote assets explicit and local app assets bundled; no CDN dependencies.
- Preserve the native Windows feel with WinUI 3 controls and system theme behavior.

## Technical Defaults
- .NET 10 LTS
- Windows App SDK 2.0.x
- WinUI 3
- WebView2
- Markdig
- CommunityToolkit.Mvvm
- MSIX-first packaging

## Verification
- Run `dotnet restore src/Openza.Reader.Tests/Openza.Reader.Tests.csproj`.
- Run `dotnet test src/Openza.Reader.Tests/Openza.Reader.Tests.csproj -c Release --no-restore`.
- Run `dotnet build src/Openza.Reader/Openza.Reader.csproj -c Release --no-restore`.
- Build/package the MSIX from Visual Studio or with MSBuild once signing details are configured.
