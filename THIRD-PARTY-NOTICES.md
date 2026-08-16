# Third-Party Notices

Openza Reader depends on third-party packages and platform components. This file summarizes direct dependencies used by the repository; package metadata remains the source of truth for each package license.

## Runtime And App Dependencies

| Component | Version | License metadata | Purpose |
| --- | --- | --- | --- |
| Markdig | 1.1.3 | BSD-2-Clause | Markdown parsing and HTML rendering |
| CommunityToolkit.Mvvm | 8.4.2 | MIT | MVVM helpers |
| Microsoft.Web.WebView2 | 1.0.3912.50 | Package license file | Embedded WebView2 control |
| Microsoft.WindowsAppSDK | 2.0.1 | Package license file | WinUI 3 and Windows App SDK runtime |
| Avalonia | 12.1.0 | MIT | Cross-platform application UI |
| Avalonia.Desktop | 12.1.0 | MIT | Desktop platform backends |
| Avalonia.Fonts.Inter | 12.1.0 | MIT | Bundled application font support |
| Avalonia.Themes.Fluent | 12.1.0 | MIT | Fluent control theme |
| Avalonia.Controls.WebView | 12.0.1 | MIT | Native embedded WebView abstraction |

On Linux, the prototype uses the operating system's WebKitGTK runtime; WebKitGTK is not bundled in this repository.

## Test Dependencies

| Component | Version | License metadata | Purpose |
| --- | --- | --- | --- |
| xUnit.net | 2.9.3 | Apache-2.0 | Unit testing |
| xUnit.net Visual Studio runner | 3.1.4 | Apache-2.0 | Test discovery and execution |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT | Test SDK |
| coverlet.collector | 6.0.4 | MIT | Test coverage collection |

## Bundled Reader Assets

The current `src/Openza.Reader/Assets/prism.js` and `src/Openza.Reader/Assets/prism.css` files are small project-owned Prism-compatible highlighting assets, not vendored upstream Prism source.

If upstream Prism assets are bundled later, add the upstream copyright and license notice here in the same pull request.

Project-created screenshots are available under the repository's MIT License. Openza names, logos, and official app icons are reserved as described in [BRAND.md](BRAND.md).
