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
| 25 | Alt+F4: the window closes and the session is saved | pass | {"tabs":[{"url":"https://www.google.com/search?q=weather%20paris\u0026sei=kVYJa_bhMc2mptQPueiugAg","title":"weather paris - Google Search","pinId":"ee122795-f8c |
| 26 | Open Search again: pinned page and tabs come back, pin keeps its icon | pass | pin saved: True, icons cached: True · 25-restored.png |
| 27 | Spaces on: the foot shows the space icons, Personal selected | pass | 26-spaces-strip.png |
| 28 | The foot has the Library at the left, Settings at the right, and the space you are in centred between them | pass | library ends 46, Personal centred at 127 (middle 126), settings starts 206 · 27-foot-centred.png |
| 29 | Ctrl+Alt+2: the foot's spaces slide so Work is in the middle | pass | library ends 46, Work centred at 127 (middle 126), settings starts 206 · 28-foot-work-centred.png |
| 30 | Ctrl+Alt+2: the Work space opens with its own empty row; a page opened there loads | pass | 29-space-work.png |
| 31 | Ctrl+Alt+1: back to Personal with its own tabs (the search, the pin) while Work's file holds only its own page | pass | 30-space-personal.png |
| 32 | Settings open in Personal, then Ctrl+Alt+2: Work shows only its own page, Settings does not stay on top | pass | [10:10:21.858] stage shows 1 page(s) in Work, live https://example.org/ · 31-space-settings-open.png · 32-space-settings-left.png |
| 33 | Ctrl+Alt+1: back in Personal, its Settings page is there again and alone | pass | [10:10:25.315] stage shows 1 page(s) in Personal, live search://settings · 33-space-settings-back.png |
| 34 | Split View of two pages in Personal, then click Work in the foot: Work shows only its own page, neither split page stays on top | pass | in Personal: [10:10:38.440] stage check: 2 visible (native 2) in Personal, live https://example.com/: https://example.net/ + https://example.com/ · in Work: [10:10:44.442] stage check: 1 visible (native 1) in Work, live https://example.org/: https://example.org/ · 34-space-split-open.png · 35-space-split-left.png |
| 35 | Ctrl+Alt+1: back in Personal, the split's two pages are there again, side by side | pass | [10:10:47.053] stage check: 2 visible (native 2) in Personal, live https://example.com/: https://example.net/ + https://example.com/ · 36-space-split-back.png |
| 36 | A split in each space: Ctrl+Alt+1 from Work's split shows Personal's two pages only | pass | in Work: [10:10:56.375] stage check: 2 visible (native 2) in Work, live https://example.com/#work: https://example.org/ + https://example.com/#work · in Personal: [10:11:02.312] stage check: 2 visible (native 2) in Personal, live https://example.com/: https://example.net/ + https://example.com/ · 37-space-split-to-split.png |
| 37 | History page open in Personal, then Ctrl+Alt+2: Work shows only its own page, History does not stay on top | pass | [10:11:19.805] stage check: 1 visible (native 1) in Work, live https://example.org/: https://example.org/ · 38-space-history-open.png · 39-space-history-left.png |
| 38 | Downloads page open in Personal, then Ctrl+Alt+2: Work shows only its own page, Downloads does not stay on top | pass | [10:11:30.604] stage check: 1 visible (native 1) in Work, live https://example.org/: https://example.org/ · 40-space-downloads-open.png · 41-space-downloads-left.png |
| 39 | Click Personal's pin, then Ctrl+Alt+2: Work shows only its own page, the pinned page does not stay on top | pass | on the pin: [10:11:37.497] stage check: 1 visible (native 1) in Personal, live https://www.google.com/search?q=weather%20paris&sei=kVYJa_bhMc2mptQPueiugAg: https://www.google.com/search?q=weather%20paris&sei=kVYJa_bhMc2mptQPueiugAg · in Work: [10:11:40.514] stage check: 1 visible (native 1) in Work, live https://example.org/: https://example.org/ · 42-space-pin-open.png · 43-space-pin-left.png |
| 40 | Pin Work's page and switch to Personal: only Personal's page shows | pass | [10:11:46.394] stage check: 1 visible (native 1) in Personal, live https://www.google.com/search?q=weather%20paris&sei=kVYJa_bhMc2mptQPueiugAg: https://www.google.com/search?q=weather%20paris&sei=kVYJa_bhMc2mptQPueiugAg · 44-space-workpin-left.png |
| 41 | A long page: its scrollbar is a thin bar over the page with no arrows, wider under the pointer | pass | 45-scrollbar-page.png · 46-scrollbar-page-hover.png |
| 42 | Many tabs in the column: its scrollbar is a thin rounded thumb (no arrows, no track), at most 10 px wide | pass | scrollbar width 10 · 47-scrollbar-side.png · 48-scrollbar-side-hover.png |
| 43 | New Space popup: name it Travel, Enter: the space is made and opened | pass | 49-space-popup.png · 50-space-new.png |
| 44 | The new space's empty tab is not covered by a page from the space you came from | pass | [10:13:08.413] stage shows 0 page(s) in Travel, live blank |
| 45 | Layout Top, pins as rows: tabs run along the top strip with the space icons, the column is gone | pass | 51-layout-top.png |
| 46 | An update that is ready shows as a disc in the strip, just before Settings | pass | update 832..858, settings at 860 · 52-strip-update-ready.png |
| 47 | The top strip has Windows' own window buttons at the top right: minimise, maximise, close, left to right | pass | min 920, max 954, close 988..1020 of a window ending at 1031 · 53-strip-caption-buttons.png |
| 48 | The top strip has Settings and the Library, and no Home or Downloads | pass | TopSettings True, TopLibraryDoor True |
| 49 | With more tabs open along the top the tabs get narrower than their full width, and not narrower than a tab can read | pass | 1 resizes, last: 72px for 10 tabs (172 when there is room) · 54-strip-tabs-narrower.png |
| 50 | With tabs past the strip's width, the plus stays at the strip's right edge, no scrolling to reach it | pass | plus at 798..828, window right 1031 · 55-strip-many-tabs.png |
| 51 | Top strip: click a tab, Shift+click the one two further: the three tabs are selected | pass | 7 tabs found, trace: [10:13:55.835] picked 3 · 56-strip-tabs-picked.png |
| 52 | Pressing the pinned plus opens one more tab | pass | 57-strip-plus-pressed.png |
| 53 | The strip's Settings button opens Settings, where Spaces & Profiles has a section of its own | pass | 58-settings-spaces-section.png |
| 54 | Right-click a space: its menu offers the Studio profile (there is no profile button in the strip) | pass | 59-profile-menu.png |
| 55 | Open the Studio profile: it opens in a window of its own with its own spaces | pass | 2 windows · 60-profile-window.png |
| 56 | A tab left alone for years, with Close tabs left alone set to a day: it is closed into the archive, the other stays | pass | archive.json holds: example.net · 61-archived-on-start.png |
| 57 | search://archive: the Archived Tabs page lists it, with Open and Remove | pass | 62-archive-page.png |
| 58 | Ctrl+Alt+N on a lone page: an empty page opens beside it, the address field ready; example.com loads in it | pass | splits: [2] · 63-split-two.png |
| 59 | Ctrl+Alt+Left: the left page takes the focus ring | pass | 64-split-focus-left.png |
| 60 | Drag the line between the pages: the left one gets narrower, the right one wider | pass | before 0.50/0.50, after 0.24/0.76 · 65-split-resized.png |
| 61 | Alt+click two more tabs: they join the split; four pages share the room | pass | splits: [4] · 66-split-four.png |
| 62 | Ctrl+Alt+N inside a split: the pages come apart and stay as tabs | pass | 67-split-separated.png |
| 63 | Shift+click a link: its page opens over this one in a panel, the page behind dimmed | pass | link found: True, panel with Open as tab: True, browser started:  · 68-peek.png |
| 64 | Esc: the peek goes away | pass | 69-peek-closed.png |
| 65 | Alt+click a link (Settings › Alt+click opens in Split View): the link loads beside the page | pass | splits: [2] · 70-split-link.png |
| 66 | A link from another app, with Little windows on: it opens in a small window, not a tab | pass | small window with Open in Search: True · 71-little-window.png |
| 67 | Snap layouts: asked what is under the pointer over the maximise button, the window says HTMAXBUTTON (9) | pass | over the button: 9, elsewhere: 1 |
| 68 | Quit with two windows (Ctrl+Shift+Q), open Search again: both windows come back, each with its own tabs | pass | windows.json lists one: True, windows open: 2, window session files: 1 · 72-windows-restored.png |
| 69 | Drag a tab from the column onto the middle of another: the two are shown side by side in Split View | pass | splits before 0, after [2] trace: page drop ready / drag hits 135 Border:T@107 Border:T@107 Border:T@107 / drag hits 133 Border:T@107 Border:T@107 Border:T@107 / drag hits 131 Border:T@107 Border:T@107 Border:T@107 / drag hits 129 TextBlock:T@113 Border:T@107 Border:T@107 Border:T@107 / drag hits 127 TextBlock:T@113 Border:T@107 Border:T@107 Border:T@107 / drag hits 127 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 126 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 125 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 124 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 123 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 122 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 121 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 120 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / split by drop Example Domain beside Example Domain / drag ended Move · 73-split-drag-over.png · 74-split-by-drag.png |
| 70 | Drag a tab from the column onto the right of the page: a zone shows the half it will take, and it opens there in Split View | pass | splits [2] trace: [10:16:39.030] page drop ready / [10:16:41.081] page drop over 0 / [10:16:43.308] page drop over 1 / [10:16:46.240] split by page drop Example Domain at 1 of 2 · 75-split-page-drop-over.png · 76-split-page-drop.png |
| 71 | Right-click a tab › Split with New Tab: the tab on the left, the new-tab field in an empty pane on the right | pass | splits [2], tabs 3 → 4 · 77-split-new-tab.png |
| 72 | Type "example" in that field, pick the open tab offered: it comes into the pane, the empty tab is gone | pass | splits [2], tabs 3 → 3, trace: [10:16:56.745] brought Example Domain into the split · 78-split-new-tab-offers.png · 79-split-new-tab-brought.png |
| 73 | Split with New Tab, then Escape with nothing typed: the empty pane goes, the tab is on its own again | pass | splits [], tabs 3 · 80-split-new-tab-escape.png |
| 74 | Click the first tab, Shift+click the third: all three are selected | pass | trace: [10:17:12.450] picked 3 · 81-tabs-picked-range.png |
| 75 | Ctrl+click the second: it is taken out, two stay selected | pass | trace: [10:17:13.896] picked 2 · 82-tabs-picked-ctrl.png |
| 76 | Right-click a selected tab: the menu is for both ("2 Tabs Selected"); Open 2 Tabs in Split View shows them side by side | pass | menu entry found: True, splits [2] · 83-tabs-picked-menu.png · 84-tabs-picked-split.png |
| 77 | Ctrl+click two tabs, drag one of them onto the left of the page: both are carried and open on the left of a split of three | pass | picks: [10:17:26.507] picked 2, splits [3], trace: [10:16:25.778] drag start Example Domain / [10:16:39.003] drag start Example Domain / [10:17:28.078] drag start Example Domain with 1 more / [10:16:46.240] split by page drop Example Domain at 1 of 2 / [10:17:28.095] page drop ready / [10:17:30.971] page drop over 0 / [10:17:34.565] split by page drop Example Domain at 0 of 3 · 85-tabs-picked-drag-over.png · 86-tabs-picked-drag-split.png |
| 78 | Tab menu › Add to Group › New Group, type Research: a heading appears above the tab, the group is saved | pass | groups: [{"id":"4e377c0a-fd56-4a84-a69e-e306bc2037a7","name":"Research","collapsed":false}] · 87-group-new.png |
| 79 | Add the third tab to Research from its menu: two tabs sit under the heading | pass | members: 2 · 88-group-two.png |
| 80 | Click the heading: the group folds to its name and a count | pass | collapsed saved: True, tab rows left: 3 · 89-group-folded.png |
| 81 | Drag a loose tab onto the Research heading: it joins the group | pass | members: 3 · from {X=110,Y=72} to {X=123,Y=69} trace: drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drop on heading / drag ended Move · 90-group-dragging.png · 91-group-drop.png |
| 82 | Pin a tab, its menu › Move to Folder › New Folder, type Work: a folder heading holds the pin | pass | folders: [{"id":"2a9ead00-07e6-4681-848b-a50144ca9cb0","name":"Work","collapsed":false}] · 92-folder-new.png |
| 83 | Right-click the folder › New Folder Inside, type Deep: a folder inside a folder, set further in | pass | folders: [{"id":"2a9ead00-07e6-4681-848b-a50144ca9cb0","name":"Work","collapsed":false},{"id":"19e77267-5d5a-4018-bf3d-adbb78ec347d","name":"Deep","parent":"2a9ead00-07e6-4681-848b-a50144ca9cb0","collapsed":false}] · 93-folder-nested.png |
| 84 | Quit and open again: the group, the folder and the folder inside it are still there | pass | headings found: Research True, Work True, Deep True · 94-groups-restored.png |
| 85 | Ctrl+Shift+S: the column folds away (a still shot straight after, then settled) | pass | column gone: True · 95-column-folding.png · 96-column-folded.png |
| 86 | Ctrl+Shift+S again: the column comes back | pass | 97-column-back.png |
| 87 | Ctrl+Alt+2: the rows of the next space slide in from the side (a still straight after, then settled) | pass | 98-space-swiping.png · 99-space-swiped.png |
| 88 | Ctrl+2: the page dips as the next tab comes up, its row takes the wash (a still straight after, then settled) | pass | 100-tab-switching.png · 101-tab-switched.png |
| 89 | A page asks for your location: a card over the page asks, Allow is kept for that site | pass | card shown: True, kept: {"sites":{"http://localhost:18951":{"Location":0}}} · 102-permission-card.png |
| 90 | Asked again: no card, the kept answer is used | pass | 103-permission-remembered.png |
| 91 | A page asks to show notifications, then sends one: the card names the site and the message | pass | asked: True, shown: True · 104-notification.png |
| 92 | Block trackers on: a page asking for a doubleclick.net address is never sent it | pass | trace: tracker blocked https://doubleclick.net/x |
| 93 | Tab menu › Site Information: the site, how it is connected and what it may use | pass | 105-site-information.png |
| 94 | Settings › Sites lists what the site was allowed | pass | 106-settings-sites.png |
| 95 | Forget takes the answer back | pass | permissions.json: {"sites":{}} |
| 96 | Ctrl+Alt+Shift+C on a page with utm_ and fbclid in its address, pasted into a new tab: only id=7 is left | pass | session: http://localhost:18951/?id=7 · 107-clean-link.png |
| 97 | Type youtube, Tab, cats, Enter: YouTube's own search for cats opens | pass | 108-site-search-chip.png · 109-site-search-result.png |
| 98 | A style kept for localhost (Settings › Sites) is on its pages: the background reads rgb(1, 2, 3) | pass | 110-site-style.png |
| 99 | Ctrl+Alt+Shift+R on an article: reading mode opens with its three paragraphs, the links around it left out | pass | paragraphs: 3 · 111-reading-mode.png |
| 100 | Ctrl+Shift+P on a page with a playing video: it floats in a picture-in-picture window | pass | [10:20:33.119] pip {"result":{"type":"string","value":"opened"}} · 112-picture-in-picture.png |
| 101 | A live folder (pin folder with a feed address): its latest posts fill it a few seconds after launch | pass | feed: http://localhost:18951/feed.xml · 113-live-folder.png |
| 102 | Click a post: its page opens in a new tab | pass | session has /post1: True · 114-live-folder-post.png |
| 103 | Heading menu › New Live Folder Inside…, a site's address (not the feed's): the feed it points to is found and a folder named after it is made | pass | address box: True, folder made: True · 115-live-folder-new.png |
| 104 | A sign-in sent and settled: a card offers to keep the password; Save puts it in the vault, protected, never in the file as written | pass | card: True, kept: True, file shows the password: False, trace: form form.focus sign-in localhost / form form.focus sign-in localhost / form form.submit  localhost / form form.submit  localhost · 116-password-save-card.png |
| 105 | Back on the sign-in: the caret in the box offers the kept account; Fill puts the name and password in through the field's own setter | pass | card: True, page title shows both filled: True · 117-password-fill-card.png |
| 106 | Settings › Passwords: the account is listed; Show (Windows Hello, or straight through where none is set up) reveals the password | pass | listed: True, shown: True · 118-settings-passwords.png |
| 107 | Settings › Passwords › Passkeys reports whether this PC can hold passkeys (the web engine's WebAuthn answers; the signing itself needs a person at Windows Hello) | pass | status: Windows Hello is not set up on this PC; a security key still works. Passkeys are offered in sign-in boxes |
| 108 | Forget removes it from the vault | pass | vault.json: {   "items": [],   "never": [] } |
| 109 | A sign-up's new-password box: a strong password is offered and fills both boxes | pass | card: True, both boxes hold the same 20 characters: True, trace: form form.focus sign-in localhost / password fill done / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus new localhost · 119-password-strong.png |
| 110 | A contact form's email box offers your card (Settings › Passwords › My card); Fill puts name, email and postal code in the empty boxes and leaves the one already typed alone | pass | card: True, boxes: True · 120-card-offer.png · 121-card-filled.png |
| 111 | Settings › Import lists Chrome; Import brings over its bookmarks (a folder keeps its name), history and saved passwords; the vault holds the password protected, not as written | pass | sources listed: True, imported: True, folder kept: True, file shows the password: False · 122-import-sources.png · 123-import-chrome.png |
| 112 | The same for Zen (a Firefox-family profile in the roaming folder): its bookmarks come over | pass | bookmark kept: True · 124-import-zen.png |
| 113 | Zen's sidebar comes too: its space is a space here, and the page pinned inside its folder is pinned in that space, in a folder of the same name | pass | space: True, pin in folder: True · 125-import-zen-spaces.png |
| 114 | Sync through a folder: at launch another PC's bookmark, space, pin and setting are here, and this PC's own file is written beside it | pass | bookmark/space/setting: True, pin in the space's session: True, own files written: True · 126-sync-applied.png |
| 115 | Settings › General says who it synced with | pass | status shown: True · 127-sync-settings.png |
| 116 | Settings › Keyboard & Mouse lists the commands with their keys | pass | New Tab listedKey: True · 128-shortcuts-page.png |
| 117 | Click a key: the box waits for the new one | pass | 129-shortcuts-waiting.png |
| 118 | Press Ctrl+Alt+K: New Tab is kept on that key, and the key is not taken as a command meanwhile | pass | shortcuts.json: {"file.newTab":"Ctrl\u002BAlt\u002BK"} · 130-shortcuts-changed.png |
| 119 | Ctrl+T no longer opens a tab; Ctrl+Alt+K does | pass | on Settings before: True, after Ctrl+T: True, after Ctrl+Alt+K: False · 131-shortcuts-works.png |
| 120 | Put every key back: the keys are as they came | pass | shortcuts.json: {} |
| 121 | Hover the tab: a copy-link button sits at the far right of the row, no second address bar above the pages | pass | copy button on row: True · 132-column-address.png |
| 122 | Clicking the tab you are on changes its address right there, no new tab; Copy link copied it | pass | tab titled Sign up: True, copied: True · 133-column-address-edit.png |
| 123 | The tab's right-click menu has Bookmark, Screenshot, Share and Style This Site; Bookmark saves the page | pass | items: True, bookmarked: True · 134-column-actions.png |
| 124 | Take Screenshot from the tab menu: the page is saved to Pictures › Screenshots and a card says so | pass | card: True, file: True · 135-column-screenshot.png |
| 125 | Dragging the column's edge as narrow as it goes: the reload button stays whole with a little space before the next one | pass | column's right button moved from 222 to 214, gap after reload: 4px · 136-column-narrowest.png |
| 126 | The foot has Settings at the right, and Update beside it when one is ready | pass | update at 160, settings at 188 · 137-column-foot.png |
| 127 | Hovering the update button: a small window shows the version and what is different | pass | popup shown: True · 138-update-popup.png |
| 128 | Clicking it downloads the update with a bar filling up | pass | downloading text shown: True · 139-update-downloading.png |
| 129 | When the download is done it says it is ready; a second click would restart | pass | ready text shown: True · 140-update-ready.png |
| 130 | Settings at the foot opens Settings | pass | Settings page shown: True |
| 131 | The Library door in the foot widens the column: a rail of six sections down its left, Spaces first with the space as a card | pass | rail buttons: 6, space card: True, invoked at {X=10,Y=729,Width=26,Height=26}; trace: [10:23:17.673] library spaces · 141-library-spaces.png |
| 132 | History in the rail: the history list shows inside the column, no tab opened | pass | search box shown: True · 142-library-history.png |
| 133 | Downloads in the rail: the downloads list shows inside the column | pass | search box shown: True · 143-library-downloads.png |
| 134 | Bookmarks in the rail: the saved page is listed inside the column | pass | search box shown: True · 144-library-bookmarks.png |
| 135 | The back arrow closes the Library: the column narrows to what it was and the tabs are back | pass | closed: True, reload at 158 then 158 · 145-library-closed.png |
| 136 | Ctrl+Shift+L opens the Library at the section it was left on, and again closes it | pass | opened: True, closed: True |
| 137 | The hide button takes the column away; the pointer at the window's edge brings it back and it leaves again with the pointer | pass | hidden: True, shown from the edge: True, left again: True · 146-column-hidden.png · 147-column-edge.png |
| 138 | Pressing the button while the column is out from the edge keeps it open | pass | still there after the pointer left: True · 148-column-kept.png |
| 139 | An extension kept in Search's folder is loaded: its content script has marked the page's title | pass | tab shows [ext]: True · 149-extension-content-script.png |
| 140 | Ctrl+Shift+E opens the Library at Extensions: the extension is listed with its switch | pass | library at Extensions: True, listed: True · 150-extension-hub.png |
| 141 | Open: the extension's popup appears in a small window | pass | popup text shown: True · 151-extension-popup.png |
| 142 | An address to a .crx in the box, Add: the package is downloaded, unpacked and listed | pass | listed: True · 152-extension-added.png |
| 143 | A Chrome Web Store link in the box, Add: the real store's package is downloaded, unpacked and listed | pass | listed · 153-extension-store.png |
| 144 | The switch turns an extension off | pass | kept as off: True · 154-extension-off.png |
| 145 | Remove takes the extension and its folder away | pass | gone: True; folders left: seeded0001 · 155-extension-removed.png |
| 146 | Settings › Privacy › Proxy set to a server of its own: a page for a name that exists nowhere loads through it | pass | the proxy saw: GET http://proxy-probe.test/ HTTP/1.1 / GET http://proxy-probe.test/favicon.ico HTTP/1.1 · 156-proxy.png |
| 147 | Settings › Privacy › Proxy › User name and Password typed: the name is vaulted in prefs, the password protected in its own file and nowhere else | pass | settings has the fields: True, protected file: True · 157-proxy-credentials.png |
| 148 | A proxy that answers 407: Search signs in with the kept name and password, and the page loads | pass | the proxy saw: asked who / signed in / signed in · 158-proxy-signed-in.png |
| 149 | A site that asks for a certificate: a card lists yours, Continue sends it, and the site sees it | pass | card: True, server saw the certificate: True, page shows it: True · 159-client-certificate-card.png · 160-client-certificate-sent.png |
| 150 | Crash log is empty | pass |  |
