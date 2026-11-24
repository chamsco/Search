# Architecture

## Why WPF + WebView2

- **Engine.** WebKit can't be embedded on Windows. WebView2 is Chromium (Edge's build) and ships with
  Windows 10/11, which keeps the original's idea of using the engine the OS already has. It loads Chrome
  extensions itself (`CoreWebView2Profile.AddBrowserExtensionAsync`), but draws no toolbar button, popup or
  side panel for them; the Mac app already fills that gap with its own shims (`Extension*.swift`), and the port
  will do the same.
- **UI.** Search draws all of its own chrome (column, pins, rows, card), so a framework of ready-made controls
  buys little. WPF has the most mature WebView2 control and compiles on Linux CI with `EnableWindowsTargeting`,
  so every change is build-checked before it reaches Windows. WinUI 3 was the alternative; its XAML compiler
  only runs on Windows.
- **Not Electron / CEF.** Both bundle a second Chromium (~150 MB) and don't solve extension UI either.

## Layout

```
src/Search.Core      Platform-free logic ported from Swift, unit-tested anywhere
  Address.cs         Address.swift: what typed text means (URL or search)
  Engine.cs          Engine.swift: search engines and suggestion endpoints
  Fuzzy.cs           Fuzzy.swift: typo-tolerant matching
  History.cs         History.swift: visits, ranking (fit x log(2 + popularity)), inline completion
  Omnibox.cs         Rows under the address field
  Session.cs         Session.swift / Pins.swift: what was open last time
  Store.cs           %LOCALAPPDATA%\Search, atomic JSON writes
src/Search.App       WPF shell
  Palette.cs         Design.swift colours (light/dark pairs, Greys.frame)
  MainWindow.*       Browser.swift + Side.swift: column, pins, tabs, card, field, keys
  Tab.cs             Tab.swift essentials; tabs sleep (no web view) until shown
installer/           Inno Setup script; registers Search as a browser for Default apps
tests/               xUnit tests, including cases ported from the Mac test suite
```

## Mac to Windows mapping

| Mac | Windows | Notes |
|---|---|---|
| Traffic lights (top left of the column) | Close / maximise / minimise in the same place | Kept in the column, as Arc does on Windows |
| ⌘ shortcuts | Ctrl shortcuts | See PARITY.md |
| Window card corners (SwiftUI clip) | Window region on the web view's HWND | Region corners are not anti-aliased |
| `~/Library/Application Support/Search` | `%LOCALAPPDATA%\Search` | |
| WKWebsiteDataStore | WebView2 user data folder `%LOCALAPPDATA%\Search\Engine` | Profiles will map to WebView2 profiles |
| Passkeys (AuthenticationServices) | WebAuthn through Windows Hello (built into WebView2) | On-device, TPM-backed |
| Apple Passwords / own vault | Local vault encrypted with DPAPI, unlocked with Windows Hello | Planned |
| iCloud sync | Folder sync (the Mac app's own alternative) | iCloud isn't available on Windows |
