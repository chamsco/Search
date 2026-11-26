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
| Pins as rows | ✅ | Settings › Tabs. Pin folders ✅ (below) |
| Tab rows: icon or letter, title, close, loading ring | ✅ | |
| Middle-click closes a tab | ✅ | |
| Drag to reorder tabs and pins | ✅ | Rows make room as you drag; drop on the pins (or "Drop here to pin") to pin |
| Layout: sidebar or top strip | ✅ | Settings › Tabs |
| Tab context menu | 🟡 | In Search's own style. Pin/Unpin, Back to Pinned Page, Replace Pinned Page, Rename, Duplicate, Copy Address, Copy as Markdown Link, Put to Sleep, Close Tab, Close Other Tabs. Copy Clean Link, Site Information, Side Panel ⬜ (each waits on its feature) |
| Tabs sleep until shown | ✅ | Restored tabs load on first view |
| Spaces, colours, icons, strength, gradient | ✅ | Settings › Tabs turns them on; strip above the footer; Ctrl+Alt+1–9; own sign-ins or shared; Move to Space in the tab menu. Swipe gesture ⬜ (touchpad has no equivalent; animation in wave 4) |
| Profiles | ✅ | A window each, own history, bookmarks, spaces and sign-ins (WebView2 profiles); footer door once there are two |
| Tab groups | ✅ | Tab menu › Add to Group (New Group, an existing one), Remove from Group; heading click folds, its menu renames, colours, ungroups, closes; drop a tab on a heading to join. Saved per space |
| Pin folders (pinned rows) | ✅ | Pin menu › Move to Folder; folders hold pins and folders, up to three levels; heading click folds; Delete Folder lifts what it held; drop a tab on a folder to pin it in. Only drawn with Settings › Pins as rows |
| Split View (up to 4) | ✅ | Ctrl+Alt+N, Alt+click a tab, tab menu (Open in Split View, Swap Pages, Stack Pages Vertically, Remove from Split View, Separate, Close Both). Lines between pages drag; double-click evens them. Ctrl+Alt+Left/Right moves focus. Alt+click a link opens it beside (Settings). Saved and restored with the session |
| Little windows, Peek | ✅ | Little windows: a link from another app (Settings › Little windows) or tab menu › Open in Small Window; Open in Search brings it into the row. Peek: Shift+click (or Alt+Shift, Alt+Ctrl) a link; Open as tab, Esc; stays with its tab (Settings); pins keep their site |
| Archive of auto-closed tabs | ✅ | Settings › Close tabs left alone; search://archive and a button on History |
| Multiple windows (Ctrl+N) | ✅ | Every window, with its place and tabs, comes back next time. Close one on purpose and it is forgotten; Ctrl+Shift+Q quits and keeps them all |
| Bookmarks bar, bookmarks, folders | ✅ | Ctrl+Shift+B card, Ctrl+Alt+B bar, folders, drag to order on the Bookmarks page |
| Library in the column (Zen): rail of Spaces, History, Downloads, Bookmarks, Site styles, Archived Tabs; widens over 0.3s; Ctrl+Shift+L | ✅ | Foot door; opens as a page instead when the column is hidden or tabs run along the top |
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
| Commands (Arc command bar) | ✅ | Type `hist`, `dwn`, `settings`… to get the page. Site search with Tab ✅ (Settings › General) |
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
| Permissions (camera, mic, location, notifications) remembered per site | ✅ | A card over the page asks; Settings › Sites lists and forgets them; Site Information in the tab menu |
| Web notifications | ✅ | Shown as a card with the site's name; Show brings its tab forward |
| Reading mode | ✅ | Ctrl+Alt+Shift+R; the Mac's prose-minus-links heuristic. Markdown twins ⬜ |
| Translation | ⛔ | Skipped by the owner's decision (wave 6) |
| Site styles | ✅ | Settings › Sites; CSS only; off until turned on |
| Live folders | ✅ | Pin folders filled from an RSS or Atom feed: latest 12 posts, refreshed every 30 minutes and on Refresh, a site address finds its feed; only with pins as rows |
| Picture in picture | ✅ | Ctrl+Shift+P floats the playing video in Chromium's own window |
| HTTPS upgrade | ✅ | Falls back to http if the site has no https; Settings › Privacy |
| Tracker protection | ✅ | Settings › Privacy › Block trackers; known ad and tracking hosts are not asked |
| Proxy | ✅ | Settings › Privacy › Proxy: Windows' own, none, or host:port / socks5://, with an optional user name and password (the password protected for the Windows account, answered only for the proxy). Takes effect at next start |
| Client certificates | ✅ | A picker over the page when a site asks, remembered per site; CI runs it against a mutual-TLS server |

## Accounts and data
| Feature | Status | Notes |
|---|---|---|
| Passkeys | 🟡 | Handled by the web engine through Windows' own WebAuthn (Windows Hello, security keys); Settings › Passwords › Passkeys reports availability and opens Windows' passkey list. Creating and signing in with a passkey needs a person at Windows Hello, so CI checks only the report |
| Saved passwords (own vault) | ✅ | Offer to save/update, Never for this site, fill by a pick under a sign-in box, strong password on sign-ups, Settings › Passwords (list, Show/Copy with Windows Hello, add, forget). Each secret protected by DPAPI for the Windows account |
| Apple Passwords | ⛔ | Own vault instead |
| Autofill from Contacts "Me" card | ✅ | Windows has no card of yours to read, so Settings › Passwords › My card holds it (name, emails, phones, addresses); picked from a row under the box, only the empty boxes of that form are filled |
| Import from Arc, Zen, Firefox, Chrome | ✅ | Settings › Import: Chrome, Edge, Brave, Arc (Chromium data) and Firefox, Zen: bookmarks, history, saved passwords (Chromium v10/v11 and Firefox NSS; Chrome's newer app-bound ones can't be read by anyone else and are reported), and Arc/Zen spaces, pins, folders and favourites |
| iCloud sync | ⛔ | Folder sync instead |
| Folder sync | ✅ | Settings › General › Sync with your other PCs: spaces, pins, pin folders, bookmarks and a few settings through a folder (OneDrive, Dropbox…), same stamped-item merge with tombstones as the Mac. A space open in a window gets another PC's pin changes at next launch |

## Extensions
| Feature | Status | Notes |
|---|---|---|
| Chrome Web Store install | ✅ | Library › Extensions: paste a store link or id (or a .crx/.zip address, or load an unpacked folder); kept unpacked in Search's folder, switched on/off, removed |
| Popups and options pages | ✅ | Open / Options in a small window; tab menu › Extensions |
| Auto-update | ✅ | Once a day after launch (and Check for updates in the Library) asks Chrome's updater and swaps a newer package in |
| Toolbar buttons, side panels | ⛔ | The engine has no toolbar or side panel to draw them in: popups open from Library › Extensions or the tab menu › Extensions |

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

Every command has a name, a section and a key, as in the Mac's `Shortcuts.swift`; Settings › Keyboard & Mouse lists them with a search box. Click a key and press the new one (Backspace takes it away, the arrow beside it puts it back); a key given to a command is taken from whoever had it, and keys that would type into the page or edit it (a bare letter, Ctrl+C/V/X/A/Z) are refused. Changes are kept in `shortcuts.json`.

| Mac | Windows default | Status |
|---|---|---|
| Settings ⌘, · Quit ⇧⌘Q | Ctrl+, · Ctrl+Shift+Q | ✅ changeable |
| New Window / Tab ⌘N ⌘T, Reopen ⇧⌘T | Ctrl+N, Ctrl+T, Ctrl+Shift+T | ✅ changeable |
| Open Address ⌘L, Open File ⌘O, Close Tab ⌘W, Close Window ⇧⌘W, Print ⌘P | Ctrl+L (also Alt+D, F6), Ctrl+O, Ctrl+W (also Ctrl+F4), Ctrl+Shift+W, Ctrl+P | ✅ changeable |
| Find ⌘F, Next ⌘G, Previous ⇧⌘G, Copy Clean Link ⌥⇧⌘C | Ctrl+F, Ctrl+G, Ctrl+Shift+G, Ctrl+Alt+Shift+C | ✅ changeable |
| Hide Sidebar ⌘S | Ctrl+Shift+S | ✅ changeable |
| Reload ⌘R, from origin ⌥⌘R | Ctrl+R (also F5), Ctrl+Alt+R (also Ctrl+F5, Ctrl+Shift+R) | ✅ changeable |
| Reading Mode, Float Video | Ctrl+Alt+Shift+R, Ctrl+Shift+P | ✅ changeable |
| Zoom ⌘+ ⌘- ⌘0 | Ctrl+=, Ctrl+-, Ctrl+0 | ✅ changeable |
| Web Inspector ⌥⌘I | Ctrl+Alt+I (also F12) | ✅ changeable |
| Full screen ⌃⌘F | F11 | ✅ changeable |
| Back ⌘[ · Forward ⌘] | Alt+Left · Alt+Right (also the mouse and browser keys) | ✅ changeable |
| Next / Previous Tab | Ctrl+Tab, Ctrl+Shift+Tab (also Ctrl+PgDn/PgUp) | ✅ changeable |
| Split ⌥⌘N, focus left/right page | Ctrl+Alt+N, Ctrl+Alt+Left, Ctrl+Alt+Right | ✅ changeable |
| Pin Tab (no Mac key), Duplicate ⌘D, Copy Address ⇧⌘C, Paste and Go ⇧⌘V | Ctrl+Shift+D, Ctrl+D, Ctrl+Shift+C, Ctrl+Shift+V | ✅ changeable |
| Add Bookmark ⇧⌘B, Bookmarks Bar | Ctrl+Shift+B, Ctrl+Alt+B | ✅ changeable |
| History ⌘Y, Downloads ⇧⌘J, Clear Browsing Data ⇧⌘⌫ | Ctrl+Y, Ctrl+Shift+J, Ctrl+Shift+Delete | ✅ changeable |
| Library, Extensions | Ctrl+Shift+L, Ctrl+Shift+E | ✅ changeable (Windows additions) |
| Spaces ⌃1–⌃9 | Ctrl+Alt+1–9 | ✅ changeable |
| ⌘1–⌘9 pick a tab by its place | Ctrl+1–Ctrl+9 | ✅ fixed, as in every browser |
| Commands with no key until you give one: Passwords, Share, Bring Things Over, Take Screenshot, Edit Site Style, Rename Tab, Copy as Markdown Link, Close Other Tabs, Next/Previous Space | | ✅ changeable |
| Mac-only commands with no Windows counterpart yet (Save Page As, Export as PDF, Reading list, profile switching keys) | | ⬜ |

## Motion
| Feature | Status | Notes |
|---|---|---|
| Hover fades | ✅ | Rows and pins |
| New rows slide in | ✅ | |
| Suggestion list scales in | ✅ | |
| Column fold (Ctrl+Shift+S), space swipe, tab switch | ✅ | 200 ms fold of the column; rows slide in from the side the space lies on; the page dips for 170 ms and the row's wash fades. All off when Windows has animations off. A web view cannot fade (it is a window of its own), so the page dips with a pane of its ground over it |

## Known differences

- Pages render with Chromium, not WebKit: closer to Chrome/Edge than Safari.
- The web view's rounded corners come from a window region and are not anti-aliased.
- Self-contained build is about 190 MB unpacked (the .NET runtime is bundled); the Mac app is about 10 MB.
  A framework-dependent build would be a few MB but needs the .NET 8 Desktop Runtime installed.
