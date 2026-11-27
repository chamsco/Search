| # | Step | Result | Note / screenshot |
|---|---|---|---|
| 1 | Launch Search.exe with an empty data folder | pass | Search |
| 2 | Empty tab shows the centred address field with a ball of dots turning above it and a microphone in the field | pass | mic True, still True, dots drawn 357, pixels that moved 0 · 01-new-tab.png |
| 3 | Type "you": popular sites suggested, youtube.com completed inline | pass | 6 rows · 02-suggestions-known-sites.png |
| 4 | Type "weather paris": search row plus Google's own suggestions | pass | 03-suggestions-engine.png |
| 5 | Press Enter: Google search loads and the app stays open (reported crash) | pass | 04-google-search.png |
| 6 | Ctrl+T, type example.com, Enter: opens in a second tab | pass | 05-second-tab.png |
| 7 | The field has a Search / Ask switch; a question in Ask mode is sent to the AI site chosen in Settings (Claude), in a tab | pass | switch True, sent True · 06-ask-mode.png · 07-ask-typed.png |
| 8 | The Search / Ask pill is whole: no taller than the field's row, so nothing is cut off | pass | mode pill 28 tall in a field row of 28 · 06-ask-mode.png |
| 9 | Hover the first tab: soft grey row and a close button, no tooltip box | pass | 08-hover-tab.png |
| 10 | Right-click the tab: Search-styled menu with Pin, Rename, Duplicate… | pass | 09-tab-menu.png |
| 11 | Choose Pin: the tab becomes a square at the top of the column | pass | 10-pinned.png |
| 12 | Ctrl+L on a page: address editable in its row, suggestions over the page | pass | 11-edit-address.png |
| 13 | Ctrl+F, type domain: matches are highlighted on the page and counted | pass | popup says: 1 of 2 · 12-find.png |
| 14 | Ctrl+Shift+B: a card to name the bookmark opens, and the page is saved | pass | card says: Bookmark added · 13-bookmark-card.png |
| 15 | Ctrl+Alt+B: the bookmarks bar appears above the page with the bookmark in it | pass | 14-bookmarks-bar.png |
| 16 | Ctrl+= twice: the page is zoomed in | pass | 15-zoomed.png |
| 17 | Ctrl+Y: History opens as a page in a tab | pass | 16-history-page.png |
| 18 | Ctrl+Shift+J: Downloads opens as a page in a tab | pass | 17-downloads-page.png |
| 19 | Ctrl+, : Settings opens as a page in a tab | pass | 18-settings-page.png |
| 20 | Settings › General › Ask with AI off hides the empty tab's Search / Ask switch; on brings it back | pass | switch found True, hidden traced True, gone True, back on True, returned True · 19-settings-ask-off.png · 20-ask-switch-hidden.png · 21-ask-switch-back.png |
| 21 | Type http://example.com: it loads over https:// | pass | 22-https-upgrade.png |
| 22 | Type http://httpforever.com (no https there): it still loads, as written | pass | tab url: http://httpforever.com/ · 23-https-fallback.png |
| 23 | Open a link to a zip: it downloads to the Downloads folder and the downloads card floats up | pass | 1 file(s) in C:\Users\runneradmin\Downloads · 24-download-float.png |
| 24 | Ctrl+N: a second window opens | pass | 2 windows · 25-new-window.png |
| 25 | Close the second window: the first stays | pass | 26-back-to-first.png |
| 26 | Start Search again with a link: the running one opens it, no second copy stays | pass | 27-one-copy.png |
| 27 | Alt+F4: the window closes and the session is saved | pass | {"tabs":[{"url":"https://www.google.com/search?q=weather%20paris\u0026sei=L-IJa6fzDaab0PEP1dyj8QI","title":"weather paris - Google Search","pinId":"3a8e115b-559 |
| 28 | Open Search again: pinned page and tabs come back, pin keeps its icon | pass | pin saved: True, icons cached: True · 28-restored.png |
| 29 | Spaces on: the foot shows the space icons, Personal selected | pass | 29-spaces-strip.png |
| 30 | The foot has the Library at the left, Settings at the right, and the space you are in centred between them | pass | library ends 46, Personal centred at 127 (middle 126), settings starts 206 · 30-foot-centred.png |
| 31 | Ctrl+Alt+2: the foot's spaces slide so Work is in the middle | pass | library ends 46, Work centred at 127 (middle 126), settings starts 206 · 31-foot-work-centred.png |
| 32 | Ctrl+Alt+2: the Work space opens with its own empty row; a page opened there loads | pass | 32-space-work.png |
| 33 | Ctrl+Alt+1: back to Personal with its own tabs (the search, the pin) while Work's file holds only its own page | pass | 33-space-personal.png |
| 34 | Settings open in Personal, then Ctrl+Alt+2: Work shows only its own page, Settings does not stay on top | pass | [20:06:02.745] stage shows 1 page(s) in Work, live https://example.org/ · 34-space-settings-open.png · 35-space-settings-left.png |
| 35 | Ctrl+Alt+1: back in Personal, its Settings page is there again and alone | pass | [20:06:06.181] stage shows 1 page(s) in Personal, live search://settings · 36-space-settings-back.png |
| 36 | Split View of two pages in Personal, then click Work in the foot: Work shows only its own page, neither split page stays on top | pass | in Personal: [20:06:19.229] stage check: 2 visible (native 2) in Personal, live https://example.com/: https://example.net/ + https://example.com/ · in Work: [20:06:25.251] stage check: 1 visible (native 1) in Work, live https://example.org/: https://example.org/ · 37-space-split-open.png · 38-space-split-left.png |
| 37 | Ctrl+Alt+1: back in Personal, the split's two pages are there again, side by side | pass | [20:06:27.818] stage check: 2 visible (native 2) in Personal, live https://example.com/: https://example.net/ + https://example.com/ · 39-space-split-back.png |
| 38 | A split in each space: Ctrl+Alt+1 from Work's split shows Personal's two pages only | pass | in Work: [20:06:37.127] stage check: 2 visible (native 2) in Work, live https://example.com/#work: https://example.org/ + https://example.com/#work · in Personal: [20:06:43.084] stage check: 2 visible (native 2) in Personal, live https://example.com/: https://example.net/ + https://example.com/ · 40-space-split-to-split.png |
| 39 | History page open in Personal, then Ctrl+Alt+2: Work shows only its own page, History does not stay on top | pass | [20:07:00.564] stage check: 1 visible (native 1) in Work, live https://example.org/: https://example.org/ · 41-space-history-open.png · 42-space-history-left.png |
| 40 | Downloads page open in Personal, then Ctrl+Alt+2: Work shows only its own page, Downloads does not stay on top | pass | [20:07:11.337] stage check: 1 visible (native 1) in Work, live https://example.org/: https://example.org/ · 43-space-downloads-open.png · 44-space-downloads-left.png |
| 41 | Click Personal's pin, then Ctrl+Alt+2: Work shows only its own page, the pinned page does not stay on top | pass | on the pin: [20:07:18.194] stage check: 1 visible (native 1) in Personal, live https://www.google.com/search?q=weather%20paris&sei=L-IJa6fzDaab0PEP1dyj8QI: https://www.google.com/search?q=weather%20paris&sei=L-IJa6fzDaab0PEP1dyj8QI · in Work: [20:07:21.204] stage check: 1 visible (native 1) in Work, live https://example.org/: https://example.org/ · 45-space-pin-open.png · 46-space-pin-left.png |
| 42 | Pin Work's page and switch to Personal: only Personal's page shows | pass | [20:07:27.080] stage check: 1 visible (native 1) in Personal, live https://www.google.com/search?q=weather%20paris&sei=L-IJa6fzDaab0PEP1dyj8QI: https://www.google.com/search?q=weather%20paris&sei=L-IJa6fzDaab0PEP1dyj8QI · 47-space-workpin-left.png |
| 43 | A long page, scrolled: a slim bar (at most 6 px) shows over the page at its right edge, with no arrows | **FAIL** | bar columns from the edge [] · 48-scrollbar-page.png |
| 44 | Leave the page alone for two seconds: the bar fades away | **FAIL** | columns still differing after idle [] · 49-scrollbar-page-idle.png |
| 45 | Point at the page's edge: the bar comes back wider and a step in from the edge, easy to grab | **FAIL** | hover columns [] vs scrolled [] · 50-scrollbar-page-hover.png |
| 46 | Many tabs in the column, scrolled: its bar shows over the list (no gutter, at most 14 px to grab), then fades after a second | pass | bar width 14, shown True, hidden True · 51-scrollbar-side.png · 52-scrollbar-side-idle.png |
| 47 | Point at the column's bar: it shows again, wider and a step in from the edge | pass | 53-scrollbar-side-hover.png |
| 48 | New Space popup: name it Travel, Enter: the space is made and opened | pass | 54-space-popup.png · 55-space-new.png |
| 49 | The new space's empty tab is not covered by a page from the space you came from | pass | [20:08:55.046] stage shows 0 page(s) in Travel, live blank |
| 50 | Layout Top, pins as rows: tabs run along the top strip with the space icons, the column is gone | pass | 56-layout-top.png |
| 51 | An update that is ready shows as a disc in the strip, just before Settings | pass | update 832..858, settings at 860 · 57-strip-update-ready.png |
| 52 | The top strip has Windows' own window buttons at the top right: minimise, maximise, close, left to right | pass | min 920, max 954, close 988..1020 of a window ending at 1031 · 58-strip-caption-buttons.png |
| 53 | The top strip has Settings and the Library, and no Home or Downloads | pass | TopSettings True, TopLibraryDoor True |
| 54 | With more tabs open along the top the tabs get narrower than their full width, and not narrower than a tab can read | pass | 1 resizes, last: 72px for 10 tabs (172 when there is room) · 59-strip-tabs-narrower.png |
| 55 | With tabs past the strip's width, the plus stays at the strip's right edge, no scrolling to reach it | pass | plus at 798..828, window right 1031 · 60-strip-many-tabs.png |
| 56 | Top strip: click a tab, Shift+click the one two further: the three tabs are selected | pass | 7 tabs found, trace: [20:09:41.586] picked 3 · 61-strip-tabs-picked.png |
| 57 | Pressing the pinned plus opens one more tab | pass | 62-strip-plus-pressed.png |
| 58 | The strip's Settings button opens Settings, where Spaces & Profiles has a section of its own | pass | 63-settings-spaces-section.png |
| 59 | Right-click a space: its menu offers the Studio profile (there is no profile button in the strip) | pass | 64-profile-menu.png |
| 60 | Open the Studio profile: it opens in a window of its own with its own spaces | pass | 2 windows · 65-profile-window.png |
| 61 | A tab left alone for years, with Close tabs left alone set to a day: it is closed into the archive, the other stays | pass | archive.json holds: example.net · 66-archived-on-start.png |
| 62 | search://archive: the Archived Tabs page lists it, with Open and Remove | pass | 67-archive-page.png |
| 63 | Ctrl+Alt+N on a lone page: an empty page opens beside it, the address field ready; example.com loads in it | pass | splits: [2] · 68-split-two.png |
| 64 | Ctrl+Alt+Left: the left page takes the focus ring | pass | 69-split-focus-left.png |
| 65 | Drag the line between the pages: the left one gets narrower, the right one wider | pass | before 0.50/0.50, after 0.24/0.76 · 70-split-resized.png |
| 66 | Alt+click two more tabs: they join the split; four pages share the room | pass | splits: [4] · 71-split-four.png |
| 67 | Ctrl+Alt+N inside a split: the pages come apart and stay as tabs | pass | 72-split-separated.png |
| 68 | Shift+click a link: its page opens over this one in a panel, the page behind dimmed | pass | link found: True, panel with Open as tab: True, browser started:  · 73-peek.png |
| 69 | Esc: the peek goes away | pass | 74-peek-closed.png |
| 70 | Alt+click a link (Settings › Alt+click opens in Split View): the link loads beside the page | pass | splits: [2] · 75-split-link.png |
| 71 | A link from another app, with Little windows on: it opens in a small window, not a tab | pass | small window with Open in Search: True · 76-little-window.png |
| 72 | Snap layouts: asked what is under the pointer over the maximise button, the window says HTMAXBUTTON (9) | pass | over the button: 9, elsewhere: 1 |
| 73 | Quit with two windows (Ctrl+Shift+Q), open Search again: both windows come back, each with its own tabs | pass | windows.json lists one: True, windows open: 2, window session files: 1 · 77-windows-restored.png |
| 74 | Drag a tab from the column onto the middle of another: the two are shown side by side in Split View | pass | splits before 0, after [2] trace: page drop ready / drag hits 135 Border:T@107 Border:T@107 Border:T@107 / drag hits 133 Border:T@107 Border:T@107 Border:T@107 / drag hits 131 Border:T@107 Border:T@107 Border:T@107 / drag hits 129 TextBlock:T@113 Border:T@107 Border:T@107 Border:T@107 / drag hits 127 TextBlock:T@113 Border:T@107 Border:T@107 Border:T@107 / drag hits 127 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 126 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 125 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 124 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 123 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 122 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 121 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 120 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / split by drop Example Domain beside Example Domain / drag ended Move · 78-split-drag-over.png · 79-split-by-drag.png |
| 75 | Drag a tab from the column onto the right of the page: a zone shows the half it will take, and it opens there in Split View | pass | splits [2] trace: [20:12:34.537] page drop ready / [20:12:36.601] page drop over 0 / [20:12:38.827] page drop over 1 / [20:12:41.741] split by page drop Example Domain at 1 of 2 · 80-split-page-drop-over.png · 81-split-page-drop.png |
| 76 | Right-click a tab › Split with New Tab: the tab on the left, the new-tab field in an empty pane on the right | pass | splits [2], tabs 3 → 4 · 82-split-new-tab.png |
| 77 | Type "example" in that field, pick the open tab offered: it comes into the pane, the empty tab is gone | pass | splits [2], tabs 3 → 3, trace: [20:12:52.149] brought Example Domain into the split · 83-split-new-tab-offers.png · 84-split-new-tab-brought.png |
| 78 | Split with New Tab, then Escape with nothing typed: the empty pane goes, the tab is on its own again | pass | splits [], tabs 3 · 85-split-new-tab-escape.png |
| 79 | Click the first tab, Shift+click the third: all three are selected | pass | trace: [20:13:07.840] picked 3 · 86-tabs-picked-range.png |
| 80 | Ctrl+click the second: it is taken out, two stay selected | pass | trace: [20:13:09.278] picked 2 · 87-tabs-picked-ctrl.png |
| 81 | Right-click a selected tab: the menu is for both ("2 Tabs Selected"); Open 2 Tabs in Split View shows them side by side | pass | menu entry found: True, splits [2] · 88-tabs-picked-menu.png · 89-tabs-picked-split.png |
| 82 | Ctrl+click two tabs, drag one of them onto the left of the page: both are carried and open on the left of a split of three | pass | picks: [20:13:21.753] picked 2, splits [3], trace: [20:12:21.428] drag start Example Domain / [20:12:34.503] drag start Example Domain / [20:13:23.325] drag start Example Domain with 1 more / [20:12:41.741] split by page drop Example Domain at 1 of 2 / [20:13:23.342] page drop ready / [20:13:26.204] page drop over 0 / [20:13:29.809] split by page drop Example Domain at 0 of 3 · 90-tabs-picked-drag-over.png · 91-tabs-picked-drag-split.png |
| 83 | Tab menu › Add to Group › New Group, type Research: a heading appears above the tab, the group is saved | pass | groups: [{"id":"7000de96-5fdd-46e2-8689-c7854df148de","name":"Research","collapsed":false}] · 92-group-new.png |
| 84 | Add the third tab to Research from its menu: two tabs sit under the heading | pass | members: 2 · 93-group-two.png |
| 85 | Click the heading: the group folds to its name and a count | pass | collapsed saved: True, tab rows left: 3 · 94-group-folded.png |
| 86 | Drag a loose tab onto the Research heading: it joins the group | pass | members: 3 · from {X=110,Y=72} to {X=123,Y=69} trace: drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drop on heading / drag ended Move · 95-group-dragging.png · 96-group-drop.png |
| 87 | Pin a tab, its menu › Move to Folder › New Folder, type Work: a folder heading holds the pin | pass | folders: [{"id":"b13b9c5c-af4a-48a1-a1bf-8609cdc8f213","name":"Work","collapsed":false}] · 97-folder-new.png |
| 88 | Right-click the folder › New Folder Inside, type Deep: a folder inside a folder, set further in | pass | folders: [{"id":"b13b9c5c-af4a-48a1-a1bf-8609cdc8f213","name":"Work","collapsed":false},{"id":"6eab2a26-29e3-4abc-9ffd-ecabbeb2b3f6","name":"Deep","parent":"b13b9c5c-af4a-48a1-a1bf-8609cdc8f213","collapsed":false}] · 98-folder-nested.png |
| 89 | Quit and open again: the group, the folder and the folder inside it are still there | pass | headings found: Research True, Work True, Deep True · 99-groups-restored.png |
| 90 | Ctrl+Shift+S: the column folds away (a still shot straight after, then settled) | pass | column gone: True · 100-column-folding.png · 101-column-folded.png |
| 91 | Ctrl+Shift+S again: the column comes back | pass | 102-column-back.png |
| 92 | Ctrl+Alt+2: the rows of the next space slide in from the side (a still straight after, then settled) | pass | 103-space-swiping.png · 104-space-swiped.png |
| 93 | Ctrl+2: the page dips as the next tab comes up, its row takes the wash (a still straight after, then settled) | pass | 105-tab-switching.png · 106-tab-switched.png |
| 94 | A page asks for your location: a card over the page asks, Allow is kept for that site | pass | card shown: True, kept: {"sites":{"http://localhost:18966":{"Location":0}}} · 107-permission-card.png |
| 95 | Asked again: no card, the kept answer is used | pass | 108-permission-remembered.png |
| 96 | A page asks to show notifications, then sends one: the card names the site and the message | pass | asked: True, shown: True · 109-notification.png |
| 97 | Block trackers on: a page asking for a doubleclick.net address is never sent it | pass | trace: tracker blocked https://doubleclick.net/x |
| 98 | Tab menu › Site Information: the site, how it is connected and what it may use | pass | 110-site-information.png |
| 99 | Settings › Sites lists what the site was allowed | pass | 111-settings-sites.png |
| 100 | Forget takes the answer back | pass | permissions.json: {"sites":{}} |
| 101 | Ctrl+Alt+Shift+C on a page with utm_ and fbclid in its address, pasted into a new tab: only id=7 is left | pass | session: http://localhost:18966/?id=7 · 112-clean-link.png |
| 102 | Type youtube, Tab, cats, Enter: YouTube's own search for cats opens | pass | 113-site-search-chip.png · 114-site-search-result.png |
| 103 | A style kept for localhost (Settings › Sites) is on its pages: the background reads rgb(1, 2, 3) | pass | 115-site-style.png |
| 104 | Ctrl+Alt+Shift+R on an article: reading mode opens with its three paragraphs, the links around it left out | pass | paragraphs: 3 · 116-reading-mode.png |
| 105 | Ctrl+Shift+P on a page with a playing video: it floats in a picture-in-picture window | pass | [20:16:24.868] pip {"result":{"type":"string","value":"opened"}} · 117-picture-in-picture.png |
| 106 | A live folder (pin folder with a feed address): its latest posts fill it a few seconds after launch | pass | feed: http://localhost:18966/feed.xml · 118-live-folder.png |
| 107 | Click a post: its page opens in a new tab | pass | session has /post1: True · 119-live-folder-post.png |
| 108 | Heading menu › New Live Folder Inside…, a site's address (not the feed's): the feed it points to is found and a folder named after it is made | pass | address box: True, folder made: True · 120-live-folder-new.png |
| 109 | A sign-in sent and settled: a card offers to keep the password; Save puts it in the vault, protected, never in the file as written | pass | card: True, kept: True, file shows the password: False, trace: form form.focus sign-in localhost / form form.focus sign-in localhost / form form.submit  localhost / form form.submit  localhost · 121-password-save-card.png |
| 110 | Back on the sign-in: the caret in the box offers the kept account; Fill puts the name and password in through the field's own setter | pass | card: True, page title shows both filled: True · 122-password-fill-card.png |
| 111 | Settings › Passwords: the account is listed; Show (Windows Hello, or straight through where none is set up) reveals the password | pass | listed: True, shown: True · 123-settings-passwords.png |
| 112 | Settings › Passwords › Passkeys reports whether this PC can hold passkeys (the web engine's WebAuthn answers; the signing itself needs a person at Windows Hello) | pass | status: Windows Hello is not set up on this PC; a security key still works. Passkeys are offered in sign-in boxes |
| 113 | Forget removes it from the vault | pass | vault.json: {   "items": [],   "never": [] } |
| 114 | A sign-up's new-password box: a strong password is offered and fills both boxes | pass | card: True, both boxes hold the same 20 characters: True, trace: form form.focus sign-in localhost / password fill done / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus new localhost · 124-password-strong.png |
| 115 | A contact form's email box offers your card (Settings › Passwords › My card); Fill puts name, email and postal code in the empty boxes and leaves the one already typed alone | pass | card: True, boxes: True · 125-card-offer.png · 126-card-filled.png |
| 116 | Settings › Import lists Chrome; Import brings over its bookmarks (a folder keeps its name), history and saved passwords; the vault holds the password protected, not as written | pass | sources listed: True, imported: True, folder kept: True, file shows the password: False · 127-import-sources.png · 128-import-chrome.png |
| 117 | The same for Zen (a Firefox-family profile in the roaming folder): its bookmarks come over | pass | bookmark kept: True · 129-import-zen.png |
| 118 | Zen's sidebar comes too: its space is a space here, and the page pinned inside its folder is pinned in that space, in a folder of the same name | pass | space: True, pin in folder: True · 130-import-zen-spaces.png |
| 119 | Sync through a folder: at launch another PC's bookmark, space, pin and setting are here, and this PC's own file is written beside it | pass | bookmark/space/setting: True, pin in the space's session: True, own files written: True · 131-sync-applied.png |
| 120 | Settings › General says who it synced with | pass | status shown: True · 132-sync-settings.png |
| 121 | Settings › Keyboard & Mouse lists the commands with their keys | pass | New Tab listedKey: True · 133-shortcuts-page.png |
| 122 | Click a key: the box waits for the new one | pass | 134-shortcuts-waiting.png |
| 123 | Press Ctrl+Alt+K: New Tab is kept on that key, and the key is not taken as a command meanwhile | pass | shortcuts.json: {"file.newTab":"Ctrl\u002BAlt\u002BK"} · 135-shortcuts-changed.png |
| 124 | Ctrl+T no longer opens a tab; Ctrl+Alt+K does | pass | on Settings before: True, after Ctrl+T: True, after Ctrl+Alt+K: False · 136-shortcuts-works.png |
| 125 | Put every key back: the keys are as they came | pass | shortcuts.json: {} |
| 126 | Hover the tab: a copy-link button sits at the far right of the row, no second address bar above the pages | pass | copy button on row: True · 137-column-address.png |
| 127 | Clicking the tab you are on changes its address right there, no new tab; Copy link copied it | pass | tab titled Sign up: True, copied: True · 138-column-address-edit.png |
| 128 | The tab's right-click menu has Bookmark, Screenshot, Share and Style This Site; Bookmark saves the page | pass | items: True, bookmarked: True · 139-column-actions.png |
| 129 | Take Screenshot from the tab menu: the page is saved to Pictures › Screenshots and a card says so | pass | card: True, file: True · 140-column-screenshot.png |
| 130 | Dragging the column's edge as narrow as it goes: the reload button stays whole with a little space before the next one | pass | column's right button moved from 222 to 214, gap after reload: 4px · 141-column-narrowest.png |
| 131 | The foot has Settings at the right, and Update beside it when one is ready | pass | update at 160, settings at 188 · 142-column-foot.png |
| 132 | Hovering the update button: a small window shows the version and what is different | pass | popup shown: True · 143-update-popup.png |
| 133 | Clicking it downloads the update with a bar filling up | pass | downloading text shown: True · 144-update-downloading.png |
| 134 | When the download is done it says it is ready; a second click would restart | pass | ready text shown: True · 145-update-ready.png |
| 135 | Settings at the foot opens Settings | pass | Settings page shown: True |
| 136 | The Library door in the foot widens the column: a rail of six sections down its left, Spaces first with the space as a card | pass | rail buttons: 6, space card: True, invoked at {X=10,Y=729,Width=26,Height=26}; trace: [20:19:15.164] library spaces · 146-library-spaces.png |
| 137 | History in the rail: the history list shows inside the column, no tab opened | pass | search box shown: True · 147-library-history.png |
| 138 | Downloads in the rail: the downloads list shows inside the column | pass | search box shown: True · 148-library-downloads.png |
| 139 | Bookmarks in the rail: the saved page is listed inside the column | pass | search box shown: True · 149-library-bookmarks.png |
| 140 | The back arrow closes the Library: the column narrows to what it was and the tabs are back | pass | closed: True, reload at 158 then 158 · 150-library-closed.png |
| 141 | Ctrl+Shift+L opens the Library at the section it was left on, and again closes it | pass | opened: True, closed: True |
| 142 | The hide button takes the column away; the pointer at the window's edge brings it back and it leaves again with the pointer | pass | hidden: True, shown from the edge: True, left again: True · 151-column-hidden.png · 152-column-edge.png |
| 143 | Pressing the button while the column is out from the edge keeps it open | pass | still there after the pointer left: True · 153-column-kept.png |
| 144 | An extension kept in Search's folder is loaded: its content script has marked the page's title | pass | tab shows [ext]: True · 154-extension-content-script.png |
| 145 | Ctrl+Shift+E opens the Library at Extensions: the extension is listed with its switch | pass | library at Extensions: True, listed: True · 155-extension-hub.png |
| 146 | Open: the extension's popup appears in a small window | pass | popup text shown: True · 156-extension-popup.png |
| 147 | An address to a .crx in the box, Add: the package is downloaded, unpacked and listed | pass | listed: True · 157-extension-added.png |
| 148 | A Chrome Web Store link in the box, Add: the real store's package is downloaded, unpacked and listed | pass | listed · 158-extension-store.png |
| 149 | The switch turns an extension off | pass | kept as off: True · 159-extension-off.png |
| 150 | Remove takes the extension and its folder away | pass | gone: True; folders left: seeded0001 · 160-extension-removed.png |
| 151 | Settings › Privacy › Proxy set to a server of its own: a page for a name that exists nowhere loads through it | pass | the proxy saw: GET http://proxy-probe.test/ HTTP/1.1 / GET http://proxy-probe.test/favicon.ico HTTP/1.1 · 161-proxy.png |
| 152 | Settings › Privacy › Proxy › User name and Password typed: the name is vaulted in prefs, the password protected in its own file and nowhere else | pass | settings has the fields: True, protected file: True · 162-proxy-credentials.png |
| 153 | A proxy that answers 407: Search signs in with the kept name and password, and the page loads | pass | the proxy saw: asked who / signed in / signed in · 163-proxy-signed-in.png |
| 154 | A site that asks for a certificate: a card lists yours, Continue sends it, and the site sees it | pass | card: True, server saw the certificate: True, page shows it: True · 164-client-certificate-card.png · 165-client-certificate-sent.png |
| 155 | Crash log is empty | pass |  |
