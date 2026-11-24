# Search for Windows

A Windows port of [Search](https://github.com/driceroland/search), the browser for the Mac by Office Commun.

> **Name and icon.** The original's code is MIT, but "Search" and its app icon belong to Office Commun,
> who ask that forks be renamed before they are distributed. This repository keeps the name and icon for
> private builds only. Both must be replaced before any public release (the name lives in
> `src/Search.App/Search.App.csproj` and `installer/Search.iss`; the icon in `src/Search.App/Assets`).

## Stack

| | Mac original | Windows port |
|---|---|---|
| Language | Swift | C# (.NET 8) |
| UI | SwiftUI + AppKit | WPF |
| Web engine | WKWebView (WebKit, part of macOS) | WebView2 (Chromium, part of Windows) |
| Installer | DMG | Inno Setup (per user, no admin) |

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) and the feature-parity list in [docs/PARITY.md](docs/PARITY.md).

## Build

Requires the .NET 8 SDK. On Windows:

```
dotnet test tests/Search.Core.Tests
dotnet run --project src/Search.App
```

Release build and installer (what CI runs, see `.github/workflows/windows.yml`):

```
dotnet publish src/Search.App -c Release -r win-x64 --self-contained true -o out/win-x64
"%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe" installer\Search.iss
```

The app needs the Microsoft Edge WebView2 Runtime, which ships with Windows 11 and is on almost all Windows 10 PCs.

The core library and its tests also build on Linux and macOS. The WPF app compiles there with
`EnableWindowsTargeting`, but only runs on Windows.

## License

MIT, see [LICENSE](LICENSE).
