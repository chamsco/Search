# Feature parity

Status of every feature found in Search 1.0.19 for Mac (from its source and UI strings).
✅ done · 🟡 partial · ⬜ not started · ⛔ no Windows equivalent (substitute noted)

## Window and column
| Feature | Status | Notes |
|---|---|---|
| Column of tabs, page as a card (8 gap, 10 radius) | ✅ | |
| Window controls in the column | ✅ | Windows buttons where the traffic lights are |
| Back / forward / reload doors | ✅ | |
| Column width drag (176–440) | ✅ | Remembered |
| Hide column (Ctrl+Shift+S) | ✅ | Mac: ⌘S. Hover-to-reveal ⬜ |
| Light / dark following the system | ✅ | Settings › Appearance: Light, Dark, System |
| Pins as squares | ✅ | Pin from the tab's menu or Ctrl+Shift+D, or drag onto a pin |
| Pins as rows | ✅ | Settings › Tabs. Pin folders ⬜ (wave 5, with tab groups) |
| Tab rows: icon or letter, title, close, loading ring | ✅ | |
| Middle-click closes a tab | ✅ | |
| Drag to reorder tabs and pins | ✅ | Rows make room as you drag; drop on the pins (or "Drop here to pin") to pin |
| Layout: sidebar or top strip | ✅ | Settings › Tabs |
| Tab context menu | 🟡 | In Search's own style. Pin/Unpin, Back to Pinned Page, Replace Pinned Page, Rename, Duplicate, Copy Address, Copy as Markdown Link, Put to Sleep, Close Tab, Close Other Tabs. Groups, spaces, split, folders, Copy Clean Link, Site Information, Side Panel ⬜ (each waits on its feature) |
| Tabs sleep until shown | ✅ | Restored tabs load on first view |
| Spaces, colours, icons, strength, gradient | ✅ | Settings › Tabs turns them on; strip above the footer; Ctrl+Alt+1–9; own sign-ins or shared; Move to Space in the tab menu. Swipe gesture ⬜ (touchpad has no equivalent; animation in wave 4) |
| Profiles | ✅ | A window each, own history, bookmarks, spaces and sign-ins (WebView2 profiles); footer door once there are two |
| Split View (up to 4) | ✅ | Ctrl+Alt+N, Alt+click a tab, tab menu (Open in Split View, Swap Pages, Stack Pages Vertically, Remove from Split View, Separate, Close Both). Lines between pages drag; double-click evens them. Ctrl+Alt+Left/Right moves focus. Alt+click a link opens it beside (Settings). Saved and restored with the session |
| Little windows, Peek | ✅ | Little windows: a link from another app (Settings › Little windows) or tab menu › Open in Small Window; Open in Search brings it into the row. Peek: Shift+click (or Alt+Shift, Alt+Ctrl) a link; Open as tab, Esc; stays with its tab (Settings); pins keep their site |
| Archive of auto-closed tabs | ✅ | Settings › Close tabs left alone; search://archive and a button on History |
| Multiple windows (Ctrl+N) | ✅ | Every window, with its place and tabs, comes back next time. Close one on purpose and it is forgotten; Ctrl+Shift+Q quits and keeps them all |
| Bookmarks bar, bookmarks, folders | ✅ | Ctrl+Shift+B card, Ctrl+Alt+B bar, folders, drag to order on the Bookmarks page |
| Footer (home, downloads, bookmarks) | ✅ | |

## Address field
| Feature | Status | Notes |
|---|---|---|
| Typed text to address or search (Address.swift rules) | ✅ | Tests ported from the Mac suite |
| Local addresses get http, 0.0.0.0 opens as localhost | ✅ | |
| Empty tab: centred field | ✅ | |
| Edit address in the row (Ctrl+L, Alt+D, F6) | ✅ | |
| History suggestions, ranking, typo forgiveness | ✅ | |
| Popular sites on the first day (History.swift `known`) | ✅ | Same 47 sites |
| Inline completion | ✅ | |
| Switch to an open tab | ✅ | |
| Search engine choice (8 + custom) | ✅ | Settings › General |
| Engine suggestions as you type | ✅ | All 8 engines; never for an address |
| Commands (Arc command bar) | ✅ | Type `hist`, `dwn`, `settings`… to get the page. Site search with Tab ⬜ |
| Bookmarks in suggestions | ✅ | |

## Pages
| Feature | Status | Notes |
|---|---|---|
| Browsing, JavaScript, cookies, storage | ✅ | WebView2 |
| Pop-ups / window.open as tabs (opener kept) | ✅ | Sign-in pop-ups work |
| Page full screen (video) | ✅ | F11 for the window |
| Favicons, titles | ✅ | |
| Session restore | ✅ | |
| History file | ✅ | `%LOCALAPPDATA%\Search\history.json` |
| History page, clear browsing data | ✅ | Ctrl+Y; Ctrl+Shift+Delete opens Settings › Privacy with time range and kinds |
| Downloads (pause, resume, panel) | ✅ | Float by the footer door, Downloads page (Ctrl+Shift+J), folder chosen in Settings |
| Find in page | ✅ | Ctrl+F, Ctrl+G, Shift+Enter; highlights with the page's CSS Custom Highlight API |
| Print | ✅ | Ctrl+P |
| Zoom (Ctrl +, −, 0) | ✅ | |
| Permissions (camera, mic, location) remembered per site | ⬜ | WebView2 asks every time for now |
| Reading mode, translation | ⬜ | Translation has no built-in Windows API |
| Site styles, live folders | ⬜ | |
| Picture in picture | 🟡 | Chromium's own |
| HTTPS upgrade | ✅ | Falls back to http if the site has no https; Settings › Privacy |
| Tracker protection | ⬜ | |
| Proxy | ⬜ | |
| Client certificates | ⬜ | |

## Accounts and data
| Feature | Status | Notes |
|---|---|---|
| Passkeys | 🟡 | Windows Hello via WebView2 out of the box; untested |
| Saved passwords (own vault) | ⬜ | DPAPI + Windows Hello |
| Apple Passwords | ⛔ | Own vault instead |
| Autofill from Contacts "Me" card | ⛔ | Card in Settings instead |
| Import from Arc, Zen, Firefox, Chrome | ⬜ | Arc for Windows and Chrome/Edge data folders |
| iCloud sync | ⛔ | Folder sync instead |
| Folder sync | ⬜ | |

## Extensions
| Feature | Status | Notes |
|---|---|---|
| Chrome Web Store install / update | ⬜ | Engine support is switched on |
| Toolbar buttons, popups, side panels | ⬜ | Port of the Mac shims |

## AI
| Feature | Status | Notes |
|---|---|---|
| Assistant (OpenAI, Anthropic, Gemini, OpenRouter, Ollama, LM Studio) | ⬜ | |
| On-device model (Qwen3 1.7B) | ⬜ | `Engine/search-ai-engine.cpp` uses llama.cpp, portable |
| Agent controlling pages | ⬜ | |

## System
| Feature | Status | Notes |
|---|---|---|
| Installer, per user | ✅ | Inno Setup |
| Default browser registration | ✅ | Settings › Apps › Default apps |
| Open links / files passed on the command line | ✅ | |
| Single instance (links open in the running window) | ✅ | |
| Rounded window corners on Windows 11 | ✅ | |
| Snap layouts on the maximise button | ✅ | Windows 11 flyout over the maximise button (both layouts) |
| Mouse back / forward buttons | ✅ | |
| Notifications from sites | 🟡 | WebView2's own |
| Updater | ✅ | Checks GitHub releases at launch and hourly, downloads quietly, "Restart to Update" in the column; Alt+Space › Check for Updates… |
| About Search | ✅ | Alt+Space › About Search |
| Crash log | ✅ | `%LOCALAPPDATA%\Search\crash.log`; errors no longer close the app |
| AppleScript | ⛔ | |
| Handoff | ⛔ | |

## Keyboard

The Mac app has a full, customisable shortcut list (`Shortcuts.swift`); only the ones below are ported so far.
| Mac | Windows | Status |
|---|---|---|
| ⌘T | Ctrl+T | ✅ |
| ⌘W | Ctrl+W, Ctrl+F4 | ✅ |
| ⇧⌘T | Ctrl+Shift+T | ✅ |
| ⌘L | Ctrl+L, Alt+D, F6 | ✅ |
| ⌘R | Ctrl+R, F5 (Ctrl+F5 / Ctrl+Shift+R ignore cache) | ✅ |
| ⌘[ ⌘] | Alt+Left, Alt+Right | ✅ |
| ⌃Tab | Ctrl+Tab, Ctrl+PgUp/PgDn | ✅ |
| ⌘1–⌘9 | Ctrl+1–Ctrl+9 | ✅ |
| ⇧⌘C copy address | Ctrl+Shift+C | ✅ |
| ⌘S toggle column | Ctrl+Shift+S | ✅ |
| Pin Tab (no Mac shortcut) | Ctrl+Shift+D (added) | ✅ |
| ⌃⌘F full screen | F11 | ✅ |

## Motion
| Feature | Status | Notes |
|---|---|---|
| Hover fades | ✅ | Rows and pins |
| New rows slide in | ✅ | |
| Suggestion list scales in | ✅ | |
| Tab switch, column fold, space swipe animations | ⬜ | |

## Known differences

- Pages render with Chromium, not WebKit: closer to Chrome/Edge than Safari.
- The web view's rounded corners come from a window region and are not anti-aliased.
- Self-contained build is about 190 MB unpacked (the .NET runtime is bundled); the Mac app is about 10 MB.
  A framework-dependent build would be a few MB but needs the .NET 8 Desktop Runtime installed.
