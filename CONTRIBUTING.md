# Contributing

Thanks for helping make Openza Reader better.

Openza Reader is currently a V1 Windows Markdown reader. Please keep contributions aligned with the V1 scope unless an issue explicitly expands it.

## Development Setup

Install:

- Windows 10 22H2 or later, or Windows 11
- .NET 10 SDK
- Visual Studio 2026 Community or later with WinUI application development tools
- Windows App SDK 2.0.x workload

Microsoft's WinUI setup configuration can install the required Visual Studio workloads:

```powershell
winget configure -f https://aka.ms/winui-config
```

## Build And Test

From the repository root:

```powershell
dotnet restore src/Openza.Reader.Tests/Openza.Reader.Tests.csproj
dotnet test src/Openza.Reader.Tests/Openza.Reader.Tests.csproj -c Release --no-restore
dotnet build src/Openza.Reader/Openza.Reader.csproj -c Release --no-restore
```

For packaged debugging and MSIX packaging, use Visual Studio and ensure Deploy is enabled for the package project in Configuration Manager.

## Contribution Rules

- Keep the app read-only for V1.
- Treat Markdown input as untrusted.
- Do not add runtime CDN dependencies.
- Keep UI changes native-feeling and consistent with WinUI 3.
- Add or update tests for renderer, navigation, activation, and file watcher behavior when those areas change.
- Keep pull requests focused and explain the user-visible behavior change.

## Commit And PR Style

Use short imperative commit messages, for example:

```text
Fix file activation dispatcher handling
```

Pull requests should include:

- Summary of the change
- Test commands run
- Screenshots or short screen recordings for UI changes
- Known limitations or follow-up work
