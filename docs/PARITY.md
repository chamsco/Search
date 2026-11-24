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
| Light / dark following the system | ✅ | Settings › Appearance override ⬜ |
| Pins as squares | ✅ | Pin from the tab's menu or Ctrl+Shift+D, or drag onto a pin |
| Pins as rows, pin folders | ⬜ | |
| Tab rows: icon or letter, title, close, loading ring | ✅ | |
| Middle-click closes a tab | ✅ | |
| Drag to reorder tabs and pins | ✅ | |
| Tab context menu | 🟡 | Pin, duplicate, copy address, copy as Markdown, reload, close, close below, reopen. Rename, mute, move to space ⬜ |
| Tabs sleep until shown | ✅ | Restored tabs load on first view |
| Spaces, space colours, swipe | ⬜ | |
| Profiles | ⬜ | Will map to WebView2 profiles |
| Split View (up to 4) | ⬜ | |
| Little windows, Peek | ⬜ | |
| Archive of auto-closed tabs | ⬜ | |
| Multiple windows (Ctrl+N) | ⬜ | |
| Bookmarks bar, bookmarks, folders | ⬜ | |
| Footer (home, downloads, bookmarks) | ⬜ | Left out until each button works |

## Address field
| Feature | Status | Notes |
|---|---|---|
| Typed text to address or search (Address.swift rules) | ✅ | Tests ported from the Mac suite |
| Local addresses get http, 0.0.0.0 opens as localhost | ✅ | |
| Empty tab: centred field | ✅ | |
| Edit address in the row (Ctrl+L, Alt+D, F6) | ✅ | |
| History suggestions, ranking, typo forgiveness | ✅ | |
| Inline completion | ✅ | |
| Switch to an open tab | ✅ | |
| Search engine choice (8 + custom) | 🟡 | Logic done; Settings UI ⬜ |
| Engine suggestions as you type | ⬜ | Endpoints ported |
| Site search with Tab, commands (Arc command bar) | ⬜ | |
| Bookmarks in suggestions | ⬜ | |

## Pages
| Feature | Status | Notes |
|---|---|---|
| Browsing, JavaScript, cookies, storage | ✅ | WebView2 |
| Pop-ups / window.open as tabs (opener kept) | ✅ | Sign-in pop-ups work |
| Page full screen (video) | ✅ | F11 for the window |
| Favicons, titles | ✅ | |
| Session restore | ✅ | |
| History file | ✅ | `%LOCALAPPDATA%\Search\history.json` |
| History page, clear browsing data | ⬜ | |
| Downloads (pause, resume, panel) | 🟡 | WebView2's own download flyout for now |
| Find in page | 🟡 | WebView2's own Ctrl+F bar for now |
| Print | 🟡 | WebView2's own Ctrl+P |
| Permissions (camera, mic, location) remembered per site | ⬜ | WebView2 asks every time for now |
| Reading mode, translation | ⬜ | Translation has no built-in Windows API |
| Site styles, live folders | ⬜ | |
| Picture in picture | 🟡 | Chromium's own |
| Tracker protection, HTTPS upgrade | ⬜ | |
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
| Single instance (links open in the running window) | ⬜ | |
| Rounded window corners on Windows 11 | ✅ | |
| Snap layouts on the maximise button | ⬜ | |
| Mouse back / forward buttons | ✅ | |
| Notifications from sites | 🟡 | WebView2's own |
| Updater | ⬜ | |
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

## Known differences

- Pages render with Chromium, not WebKit: closer to Chrome/Edge than Safari.
- The web view's rounded corners come from a window region and are not anti-aliased.
- Self-contained build is about 190 MB unpacked (the .NET runtime is bundled); the Mac app is about 10 MB.
  A framework-dependent build would be a few MB but needs the .NET 8 Desktop Runtime installed.
