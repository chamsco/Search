| # | Step | Result | Note / screenshot |
|---|---|---|---|
| 1 | Launch Search.exe with an empty data folder | pass | Search |
| 2 | Empty tab shows the centred address field with a ball of dots turning above it and a microphone in the field | pass | mic True, still True, dots drawn 357, pixels that moved 0 · 01-new-tab.png |
| 3 | Type "you": popular sites suggested, youtube.com completed inline | pass | 6 rows · 02-suggestions-known-sites.png |
| 4 | Type "weather paris": search row plus Google's own suggestions | pass | 03-suggestions-engine.png |
| 5 | Press Enter: Google search loads and the app stays open (reported crash) | pass | 04-google-search.png |
| 6 | Ctrl+T, type example.com, Enter: opens in a second tab | pass | 05-second-tab.png |
| 7 | The field has a Search / Ask switch; a question in Ask mode is sent to the AI site chosen in Settings (Claude), in a tab | pass | switch True, sent True · 06-ask-mode.png · 07-ask-typed.png |
| 8 | Hover the first tab: soft grey row and a close button, no tooltip box | pass | 08-hover-tab.png |
| 9 | Right-click the tab: Search-styled menu with Pin, Rename, Duplicate… | pass | 09-tab-menu.png |
| 10 | Choose Pin: the tab becomes a square at the top of the column | pass | 10-pinned.png |
| 11 | Ctrl+L on a page: address editable in its row, suggestions over the page | pass | 11-edit-address.png |
| 12 | Ctrl+F, type domain: matches are highlighted on the page and counted | pass | popup says: 1 of 2 · 12-find.png |
| 13 | Ctrl+Shift+B: a card to name the bookmark opens, and the page is saved | pass | card says: Bookmark added · 13-bookmark-card.png |
| 14 | Ctrl+Alt+B: the bookmarks bar appears above the page with the bookmark in it | pass | 14-bookmarks-bar.png |
| 15 | Ctrl+= twice: the page is zoomed in | pass | 15-zoomed.png |
| 16 | Ctrl+Y: History opens as a page in a tab | pass | 16-history-page.png |
| 17 | Ctrl+Shift+J: Downloads opens as a page in a tab | pass | 17-downloads-page.png |
| 18 | Ctrl+, : Settings opens as a page in a tab | pass | 18-settings-page.png |
| 19 | Type http://example.com: it loads over https:// | pass | 19-https-upgrade.png |
| 20 | Type http://httpforever.com (no https there): it still loads, as written | pass | tab url: http://httpforever.com/ · 20-https-fallback.png |
| 21 | Open a link to a zip: it downloads to the Downloads folder and the downloads card floats up | pass | 1 file(s) in C:\Users\runneradmin\Downloads · 21-download-float.png |
| 22 | Ctrl+N: a second window opens | pass | 2 windows · 22-new-window.png |
| 23 | Close the second window: the first stays | pass | 23-back-to-first.png |
| 24 | Start Search again with a link: the running one opens it, no second copy stays | pass | 24-one-copy.png |
| 25 | Alt+F4: the window closes and the session is saved | pass | {"tabs":[{"url":"https://www.google.com/search?q=weather%20paris\u0026sei=CEwJa9THLJPX5NoPr9SC-Q4#cobssid=s","title":"weather paris - Google Search","pinId":"ed |
| 26 | Open Search again: pinned page and tabs come back, pin keeps its icon | pass | pin saved: True, icons cached: True · 25-restored.png |
| 27 | Spaces on: the foot shows the space icons, Personal selected | pass | 26-spaces-strip.png |
| 28 | The foot has the Library at the left, Settings at the right, and the space you are in centred between them | pass | library ends 46, Personal centred at 127 (middle 126), settings starts 206 · 27-foot-centred.png |
| 29 | Ctrl+Alt+2: the foot's spaces slide so Work is in the middle | pass | library ends 46, Work centred at 127 (middle 126), settings starts 206 · 28-foot-work-centred.png |
| 30 | Ctrl+Alt+2: the Work space opens with its own empty row; a page opened there loads | pass | 29-space-work.png |
| 31 | Ctrl+Alt+1: back to Personal with its own tabs (the search, the pin) while Work's file holds only its own page | pass | 30-space-personal.png |
| 32 | Settings open in Personal, then Ctrl+Alt+2: Work shows only its own page, Settings does not stay on top | pass | [09:25:24.964] stage shows 1 page(s) in Work, live https://example.org/ · 31-space-settings-open.png · 32-space-settings-left.png |
| 33 | Ctrl+Alt+1: back in Personal, its Settings page is there again and alone | pass | [09:25:28.421] stage shows 1 page(s) in Personal, live search://settings · 33-space-settings-back.png |
| 34 | New Space popup: name it Travel, Enter: the space is made and opened | pass | 34-space-popup.png · 35-space-new.png |
| 35 | The new space's empty tab is not covered by a page from the space you came from | pass | [09:25:33.660] stage shows 0 page(s) in Travel, live blank |
| 36 | Layout Top, pins as rows: tabs run along the top strip with the space icons, the column is gone | pass | 36-layout-top.png |
| 37 | An update that is ready shows as a disc in the strip, just before Settings | pass | update 832..858, settings at 860 · 37-strip-update-ready.png |
| 38 | The top strip has Windows' own window buttons at the top right: minimise, maximise, close, left to right | pass | min 920, max 954, close 988..1020 of a window ending at 1031 · 38-strip-caption-buttons.png |
| 39 | The top strip has Settings and the Library, and no Home or Downloads | pass | TopSettings True, TopLibraryDoor True |
| 40 | With more tabs open along the top the tabs get narrower than their full width, and not narrower than a tab can read | pass | 1 resizes, last: 72px for 8 tabs (172 when there is room) · 39-strip-tabs-narrower.png |
| 41 | With tabs past the strip's width, the plus stays at the strip's right edge, no scrolling to reach it | pass | plus at 798..828, window right 1031 · 40-strip-many-tabs.png |
| 42 | Top strip: click a tab, Shift+click the one two further: the three tabs are selected | pass | 7 tabs found, trace: [09:26:21.486] picked 3 · 41-strip-tabs-picked.png |
| 43 | Pressing the pinned plus opens one more tab | pass | 42-strip-plus-pressed.png |
| 44 | The strip's Settings button opens Settings, where Spaces & Profiles has a section of its own | pass | 43-settings-spaces-section.png |
| 45 | Right-click a space: its menu offers the Studio profile (there is no profile button in the strip) | pass | 44-profile-menu.png |
| 46 | Open the Studio profile: it opens in a window of its own with its own spaces | pass | 2 windows · 45-profile-window.png |
| 47 | A tab left alone for years, with Close tabs left alone set to a day: it is closed into the archive, the other stays | pass | archive.json holds: example.net · 46-archived-on-start.png |
| 48 | search://archive: the Archived Tabs page lists it, with Open and Remove | pass | 47-archive-page.png |
| 49 | Ctrl+Alt+N on a lone page: an empty page opens beside it, the address field ready; example.com loads in it | pass | splits: [2] · 48-split-two.png |
| 50 | Ctrl+Alt+Left: the left page takes the focus ring | pass | 49-split-focus-left.png |
| 51 | Drag the line between the pages: the left one gets narrower, the right one wider | pass | before 0.50/0.50, after 0.24/0.76 · 50-split-resized.png |
| 52 | Alt+click two more tabs: they join the split; four pages share the room | pass | splits: [4] · 51-split-four.png |
| 53 | Ctrl+Alt+N inside a split: the pages come apart and stay as tabs | pass | 52-split-separated.png |
| 54 | Shift+click a link: its page opens over this one in a panel, the page behind dimmed | pass | link found: True, panel with Open as tab: True, browser started:  · 53-peek.png |
| 55 | Esc: the peek goes away | pass | 54-peek-closed.png |
| 56 | Alt+click a link (Settings › Alt+click opens in Split View): the link loads beside the page | pass | splits: [2] · 55-split-link.png |
| 57 | A link from another app, with Little windows on: it opens in a small window, not a tab | pass | small window with Open in Search: True · 56-little-window.png |
| 58 | Snap layouts: asked what is under the pointer over the maximise button, the window says HTMAXBUTTON (9) | pass | over the button: 9, elsewhere: 1 |
| 59 | Quit with two windows (Ctrl+Shift+Q), open Search again: both windows come back, each with its own tabs | pass | windows.json lists one: True, windows open: 2, window session files: 1 · 57-windows-restored.png |
| 60 | Drag a tab from the column onto the middle of another: the two are shown side by side in Split View | pass | splits before 0, after [2] trace: page drop ready / drag hits 135 Border:T@107 Border:T@107 Border:T@107 / drag hits 133 Border:T@107 Border:T@107 Border:T@107 / drag hits 131 Border:T@107 Border:T@107 Border:T@107 / drag hits 129 TextBlock:T@113 Border:T@107 Border:T@107 Border:T@107 / drag hits 127 TextBlock:T@113 Border:T@107 Border:T@107 Border:T@107 / drag hits 127 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 126 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 125 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 124 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 123 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 122 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 121 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 120 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / split by drop Example Domain beside Example Domain / drag ended Move · 58-split-drag-over.png · 59-split-by-drag.png |
| 61 | Drag a tab from the column onto the right of the page: a zone shows the half it will take, and it opens there in Split View | pass | splits [2] trace: [09:29:04.355] page drop ready / [09:29:06.404] page drop over 0 / [09:29:08.711] page drop over 1 / [09:29:11.629] split by page drop Example Domain at 1 of 2 · 60-split-page-drop-over.png · 61-split-page-drop.png |
| 62 | Right-click a tab › Split with New Tab: the tab on the left, the new-tab field in an empty pane on the right | pass | splits [2], tabs 3 → 4 · 62-split-new-tab.png |
| 63 | Type "example" in that field, pick the open tab offered: it comes into the pane, the empty tab is gone | pass | splits [2], tabs 3 → 3, trace: [09:29:22.200] brought Example Domain into the split · 63-split-new-tab-offers.png · 64-split-new-tab-brought.png |
| 64 | Split with New Tab, then Escape with nothing typed: the empty pane goes, the tab is on its own again | pass | splits [], tabs 3 · 65-split-new-tab-escape.png |
| 65 | Click the first tab, Shift+click the third: all three are selected | pass | trace: [09:29:38.035] picked 3 · 66-tabs-picked-range.png |
| 66 | Ctrl+click the second: it is taken out, two stay selected | pass | trace: [09:29:39.482] picked 2 · 67-tabs-picked-ctrl.png |
| 67 | Right-click a selected tab: the menu is for both ("2 Tabs Selected"); Open 2 Tabs in Split View shows them side by side | pass | menu entry found: True, splits [2] · 68-tabs-picked-menu.png · 69-tabs-picked-split.png |
| 68 | Ctrl+click two tabs, drag one of them onto the left of the page: both are carried and open on the left of a split of three | pass | picks: [09:29:52.123] picked 2, splits [3], trace: [09:28:51.056] drag start Example Domain / [09:29:04.315] drag start Example Domain / [09:29:53.697] drag start Example Domain with 1 more / [09:29:11.629] split by page drop Example Domain at 1 of 2 / [09:29:53.730] page drop ready / [09:29:56.578] page drop over 0 / [09:30:00.184] split by page drop Example Domain at 0 of 3 · 70-tabs-picked-drag-over.png · 71-tabs-picked-drag-split.png |
| 69 | Unexpected error | **FAIL** | Index was out of range. Must be non-negative and less than the size of the collection. (Parameter 'index') |
| 70 | Crash log is empty | pass |  |
