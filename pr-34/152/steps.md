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
| 27 | Alt+F4: the window closes and the session is saved | pass | {"tabs":[{"url":"https://www.google.com/search?q=weather%20paris\u0026sei=ZuEJa-n-CM2nmtkPk4fM0A0","title":"weather paris - Google Search","pinId":"0dff7310-a47 |
| 28 | Open Search again: pinned page and tabs come back, pin keeps its icon | pass | pin saved: True, icons cached: True · 28-restored.png |
| 29 | Spaces on: the foot shows the space icons, Personal selected | pass | 29-spaces-strip.png |
| 30 | The foot has the Library at the left, Settings at the right, and the space you are in centred between them | pass | library ends 46, Personal centred at 127 (middle 126), settings starts 206 · 30-foot-centred.png |
| 31 | Ctrl+Alt+2: the foot's spaces slide so Work is in the middle | pass | library ends 46, Work centred at 127 (middle 126), settings starts 206 · 31-foot-work-centred.png |
| 32 | Ctrl+Alt+2: the Work space opens with its own empty row; a page opened there loads | pass | 32-space-work.png |
| 33 | Ctrl+Alt+1: back to Personal with its own tabs (the search, the pin) while Work's file holds only its own page | pass | 33-space-personal.png |
| 34 | Settings open in Personal, then Ctrl+Alt+2: Work shows only its own page, Settings does not stay on top | pass | [20:02:43.803] stage shows 1 page(s) in Work, live https://example.org/ · 34-space-settings-open.png · 35-space-settings-left.png |
| 35 | Ctrl+Alt+1: back in Personal, its Settings page is there again and alone | pass | [20:02:47.251] stage shows 1 page(s) in Personal, live search://settings · 36-space-settings-back.png |
| 36 | Split View of two pages in Personal, then click Work in the foot: Work shows only its own page, neither split page stays on top | pass | in Personal: [20:03:00.401] stage check: 2 visible (native 2) in Personal, live https://example.com/: https://example.net/ + https://example.com/ · in Work: [20:03:06.399] stage check: 1 visible (native 1) in Work, live https://example.org/: https://example.org/ · 37-space-split-open.png · 38-space-split-left.png |
| 37 | Ctrl+Alt+1: back in Personal, the split's two pages are there again, side by side | pass | [20:03:09.006] stage check: 2 visible (native 2) in Personal, live https://example.com/: https://example.net/ + https://example.com/ · 39-space-split-back.png |
| 38 | A split in each space: Ctrl+Alt+1 from Work's split shows Personal's two pages only | pass | in Work: [20:03:18.350] stage check: 2 visible (native 2) in Work, live https://example.com/#work: https://example.org/ + https://example.com/#work · in Personal: [20:03:24.272] stage check: 2 visible (native 2) in Personal, live https://example.com/: https://example.net/ + https://example.com/ · 40-space-split-to-split.png |
| 39 | History page open in Personal, then Ctrl+Alt+2: Work shows only its own page, History does not stay on top | pass | [20:03:41.748] stage check: 1 visible (native 1) in Work, live https://example.org/: https://example.org/ · 41-space-history-open.png · 42-space-history-left.png |
| 40 | Downloads page open in Personal, then Ctrl+Alt+2: Work shows only its own page, Downloads does not stay on top | pass | [20:03:52.548] stage check: 1 visible (native 1) in Work, live https://example.org/: https://example.org/ · 43-space-downloads-open.png · 44-space-downloads-left.png |
| 41 | Click Personal's pin, then Ctrl+Alt+2: Work shows only its own page, the pinned page does not stay on top | pass | on the pin: [20:03:59.447] stage check: 1 visible (native 1) in Personal, live https://www.google.com/search?q=weather%20paris&sei=ZuEJa-n-CM2nmtkPk4fM0A0: https://www.google.com/search?q=weather%20paris&sei=ZuEJa-n-CM2nmtkPk4fM0A0 · in Work: [20:04:02.477] stage check: 1 visible (native 1) in Work, live https://example.org/: https://example.org/ · 45-space-pin-open.png · 46-space-pin-left.png |
| 42 | Pin Work's page and switch to Personal: only Personal's page shows | pass | [20:04:08.378] stage check: 1 visible (native 1) in Personal, live https://www.google.com/search?q=weather%20paris&sei=ZuEJa-n-CM2nmtkPk4fM0A0: https://www.google.com/search?q=weather%20paris&sei=ZuEJa-n-CM2nmtkPk4fM0A0 · 47-space-workpin-left.png |
| 43 | A long page: its scrollbar is a thin bar over the page with no arrows, wider under the pointer | pass | 48-scrollbar-page.png · 49-scrollbar-page-hover.png |
| 44 | Many tabs in the column: its scrollbar is a thin rounded thumb (no arrows, no track), at most 10 px wide | pass | scrollbar width 10 · 50-scrollbar-side.png · 51-scrollbar-side-hover.png |
| 45 | New Space popup: name it Travel, Enter: the space is made and opened | pass | 52-space-popup.png · 53-space-new.png |
| 46 | The new space's empty tab is not covered by a page from the space you came from | pass | [20:05:30.433] stage shows 0 page(s) in Travel, live blank |
| 47 | Layout Top, pins as rows: tabs run along the top strip with the space icons, the column is gone | pass | 54-layout-top.png |
| 48 | An update that is ready shows as a disc in the strip, just before Settings | pass | update 832..858, settings at 860 · 55-strip-update-ready.png |
| 49 | The top strip has Windows' own window buttons at the top right: minimise, maximise, close, left to right | pass | min 920, max 954, close 988..1020 of a window ending at 1031 · 56-strip-caption-buttons.png |
| 50 | The top strip has Settings and the Library, and no Home or Downloads | pass | TopSettings True, TopLibraryDoor True |
| 51 | With more tabs open along the top the tabs get narrower than their full width, and not narrower than a tab can read | pass | 1 resizes, last: 72px for 10 tabs (172 when there is room) · 57-strip-tabs-narrower.png |
| 52 | With tabs past the strip's width, the plus stays at the strip's right edge, no scrolling to reach it | pass | plus at 798..828, window right 1031 · 58-strip-many-tabs.png |
| 53 | Top strip: click a tab, Shift+click the one two further: the three tabs are selected | pass | 7 tabs found, trace: [20:06:19.093] picked 3 · 59-strip-tabs-picked.png |
| 54 | Pressing the pinned plus opens one more tab | pass | 60-strip-plus-pressed.png |
| 55 | The strip's Settings button opens Settings, where Spaces & Profiles has a section of its own | pass | 61-settings-spaces-section.png |
| 56 | Right-click a space: its menu offers the Studio profile (there is no profile button in the strip) | pass | 62-profile-menu.png |
| 57 | Open the Studio profile: it opens in a window of its own with its own spaces | pass | 2 windows · 63-profile-window.png |
| 58 | A tab left alone for years, with Close tabs left alone set to a day: it is closed into the archive, the other stays | pass | archive.json holds: example.net · 64-archived-on-start.png |
| 59 | search://archive: the Archived Tabs page lists it, with Open and Remove | pass | 65-archive-page.png |
| 60 | Ctrl+Alt+N on a lone page: an empty page opens beside it, the address field ready; example.com loads in it | pass | splits: [2] · 66-split-two.png |
| 61 | Ctrl+Alt+Left: the left page takes the focus ring | pass | 67-split-focus-left.png |
| 62 | Drag the line between the pages: the left one gets narrower, the right one wider | pass | before 0.50/0.50, after 0.24/0.76 · 68-split-resized.png |
| 63 | Alt+click two more tabs: they join the split; four pages share the room | pass | splits: [4] · 69-split-four.png |
| 64 | Ctrl+Alt+N inside a split: the pages come apart and stay as tabs | pass | 70-split-separated.png |
| 65 | Shift+click a link: its page opens over this one in a panel, the page behind dimmed | pass | link found: True, panel with Open as tab: True, browser started:  · 71-peek.png |
| 66 | Esc: the peek goes away | pass | 72-peek-closed.png |
| 67 | Alt+click a link (Settings › Alt+click opens in Split View): the link loads beside the page | pass | splits: [2] · 73-split-link.png |
| 68 | A link from another app, with Little windows on: it opens in a small window, not a tab | pass | small window with Open in Search: True · 74-little-window.png |
| 69 | Snap layouts: asked what is under the pointer over the maximise button, the window says HTMAXBUTTON (9) | pass | over the button: 9, elsewhere: 1 |
| 70 | Quit with two windows (Ctrl+Shift+Q), open Search again: both windows come back, each with its own tabs | pass | windows.json lists one: True, windows open: 2, window session files: 1 · 75-windows-restored.png |
| 71 | Drag a tab from the column onto the middle of another: the two are shown side by side in Split View | pass | splits before 0, after [2] trace: page drop ready / drag hits 135 Border:T@107 Border:T@107 Border:T@107 / drag hits 133 Border:T@107 Border:T@107 Border:T@107 / drag hits 131 Border:T@107 Border:T@107 Border:T@107 / drag hits 129 TextBlock:T@113 Border:T@107 Border:T@107 Border:T@107 / drag hits 127 TextBlock:T@113 Border:T@107 Border:T@107 Border:T@107 / drag hits 127 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 126 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 125 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 124 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 123 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 122 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 121 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 120 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / split by drop Example Domain beside Example Domain / drag ended Move · 76-split-drag-over.png · 77-split-by-drag.png |
| 72 | Drag a tab from the column onto the right of the page: a zone shows the half it will take, and it opens there in Split View | pass | splits [2] trace: [20:09:03.629] page drop ready / [20:09:05.693] page drop over 0 / [20:09:07.919] page drop over 1 / [20:09:10.883] split by page drop Example Domain at 1 of 2 · 78-split-page-drop-over.png · 79-split-page-drop.png |
| 73 | Right-click a tab › Split with New Tab: the tab on the left, the new-tab field in an empty pane on the right | pass | splits [2], tabs 3 → 4 · 80-split-new-tab.png |
| 74 | Type "example" in that field, pick the open tab offered: it comes into the pane, the empty tab is gone | pass | splits [2], tabs 3 → 3, trace: [20:09:21.480] brought Example Domain into the split · 81-split-new-tab-offers.png · 82-split-new-tab-brought.png |
| 75 | Split with New Tab, then Escape with nothing typed: the empty pane goes, the tab is on its own again | pass | splits [], tabs 3 · 83-split-new-tab-escape.png |
| 76 | Click the first tab, Shift+click the third: all three are selected | pass | trace: [20:09:37.210] picked 3 · 84-tabs-picked-range.png |
| 77 | Ctrl+click the second: it is taken out, two stay selected | pass | trace: [20:09:38.659] picked 2 · 85-tabs-picked-ctrl.png |
| 78 | Right-click a selected tab: the menu is for both ("2 Tabs Selected"); Open 2 Tabs in Split View shows them side by side | pass | menu entry found: True, splits [2] · 86-tabs-picked-menu.png · 87-tabs-picked-split.png |
| 79 | Ctrl+click two tabs, drag one of them onto the left of the page: both are carried and open on the left of a split of three | pass | picks: [20:09:51.314] picked 2, splits [3], trace: [20:08:50.353] drag start Example Domain / [20:09:03.605] drag start Example Domain / [20:09:52.887] drag start Example Domain with 1 more / [20:09:10.883] split by page drop Example Domain at 1 of 2 / [20:09:52.920] page drop ready / [20:09:55.769] page drop over 0 / [20:09:59.372] split by page drop Example Domain at 0 of 3 · 88-tabs-picked-drag-over.png · 89-tabs-picked-drag-split.png |
| 80 | Tab menu › Add to Group › New Group, type Research: a heading appears above the tab, the group is saved | pass | groups: [{"id":"3e257973-1303-4b74-a3af-93823863bdd8","name":"Research","collapsed":false}] · 90-group-new.png |
| 81 | Add the third tab to Research from its menu: two tabs sit under the heading | pass | members: 2 · 91-group-two.png |
| 82 | Click the heading: the group folds to its name and a count | pass | collapsed saved: True, tab rows left: 3 · 92-group-folded.png |
| 83 | Drag a loose tab onto the Research heading: it joins the group | pass | members: 3 · from {X=110,Y=72} to {X=123,Y=69} trace: drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drop on heading / drag ended Move · 93-group-dragging.png · 94-group-drop.png |
| 84 | Pin a tab, its menu › Move to Folder › New Folder, type Work: a folder heading holds the pin | pass | folders: [{"id":"1cbe5650-d789-4517-8a2d-58e08de3ea2d","name":"Work","collapsed":false}] · 95-folder-new.png |
| 85 | Right-click the folder › New Folder Inside, type Deep: a folder inside a folder, set further in | pass | folders: [{"id":"1cbe5650-d789-4517-8a2d-58e08de3ea2d","name":"Work","collapsed":false},{"id":"fb8d4caf-b4e6-49c3-8fa3-fb740206e224","name":"Deep","parent":"1cbe5650-d789-4517-8a2d-58e08de3ea2d","collapsed":false}] · 96-folder-nested.png |
| 86 | Quit and open again: the group, the folder and the folder inside it are still there | pass | headings found: Research True, Work True, Deep True · 97-groups-restored.png |
| 87 | Ctrl+Shift+S: the column folds away (a still shot straight after, then settled) | pass | column gone: True · 98-column-folding.png · 99-column-folded.png |
| 88 | Ctrl+Shift+S again: the column comes back | pass | 100-column-back.png |
| 89 | Ctrl+Alt+2: the rows of the next space slide in from the side (a still straight after, then settled) | pass | 101-space-swiping.png · 102-space-swiped.png |
| 90 | Ctrl+2: the page dips as the next tab comes up, its row takes the wash (a still straight after, then settled) | pass | 103-tab-switching.png · 104-tab-switched.png |
| 91 | A page asks for your location: a card over the page asks, Allow is kept for that site | pass | card shown: True, kept: {"sites":{"http://localhost:18951":{"Location":0}}} · 105-permission-card.png |
| 92 | Asked again: no card, the kept answer is used | pass | 106-permission-remembered.png |
| 93 | A page asks to show notifications, then sends one: the card names the site and the message | pass | asked: True, shown: True · 107-notification.png |
| 94 | Block trackers on: a page asking for a doubleclick.net address is never sent it | pass | trace: tracker blocked https://doubleclick.net/x |
| 95 | Tab menu › Site Information: the site, how it is connected and what it may use | pass | 108-site-information.png |
| 96 | Settings › Sites lists what the site was allowed | pass | 109-settings-sites.png |
| 97 | Forget takes the answer back | pass | permissions.json: {"sites":{}} |
| 98 | Ctrl+Alt+Shift+C on a page with utm_ and fbclid in its address, pasted into a new tab: only id=7 is left | pass | session: http://localhost:18951/?id=7 · 110-clean-link.png |
| 99 | Type youtube, Tab, cats, Enter: YouTube's own search for cats opens | pass | 111-site-search-chip.png · 112-site-search-result.png |
| 100 | A style kept for localhost (Settings › Sites) is on its pages: the background reads rgb(1, 2, 3) | pass | 113-site-style.png |
| 101 | Ctrl+Alt+Shift+R on an article: reading mode opens with its three paragraphs, the links around it left out | pass | paragraphs: 3 · 114-reading-mode.png |
| 102 | Ctrl+Shift+P on a page with a playing video: it floats in a picture-in-picture window | pass | [20:12:58.804] pip {"result":{"type":"string","value":"opened"}} · 115-picture-in-picture.png |
| 103 | A live folder (pin folder with a feed address): its latest posts fill it a few seconds after launch | pass | feed: http://localhost:18951/feed.xml · 116-live-folder.png |
| 104 | Click a post: its page opens in a new tab | pass | session has /post1: True · 117-live-folder-post.png |
| 105 | Heading menu › New Live Folder Inside…, a site's address (not the feed's): the feed it points to is found and a folder named after it is made | pass | address box: True, folder made: True · 118-live-folder-new.png |
| 106 | A sign-in sent and settled: a card offers to keep the password; Save puts it in the vault, protected, never in the file as written | pass | card: True, kept: True, file shows the password: False, trace: form form.focus sign-in localhost / form form.focus sign-in localhost / form form.submit  localhost / form form.submit  localhost · 119-password-save-card.png |
| 107 | Back on the sign-in: the caret in the box offers the kept account; Fill puts the name and password in through the field's own setter | pass | card: True, page title shows both filled: True · 120-password-fill-card.png |
| 108 | Settings › Passwords: the account is listed; Show (Windows Hello, or straight through where none is set up) reveals the password | pass | listed: True, shown: True · 121-settings-passwords.png |
| 109 | Settings › Passwords › Passkeys reports whether this PC can hold passkeys (the web engine's WebAuthn answers; the signing itself needs a person at Windows Hello) | pass | status: Windows Hello is not set up on this PC; a security key still works. Passkeys are offered in sign-in boxes |
| 110 | Forget removes it from the vault | pass | vault.json: {   "items": [],   "never": [] } |
| 111 | A sign-up's new-password box: a strong password is offered and fills both boxes | pass | card: True, both boxes hold the same 20 characters: True, trace: form form.focus sign-in localhost / password fill done / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus new localhost · 122-password-strong.png |
| 112 | A contact form's email box offers your card (Settings › Passwords › My card); Fill puts name, email and postal code in the empty boxes and leaves the one already typed alone | pass | card: True, boxes: True · 123-card-offer.png · 124-card-filled.png |
| 113 | Settings › Import lists Chrome; Import brings over its bookmarks (a folder keeps its name), history and saved passwords; the vault holds the password protected, not as written | pass | sources listed: True, imported: True, folder kept: True, file shows the password: False · 125-import-sources.png · 126-import-chrome.png |
| 114 | The same for Zen (a Firefox-family profile in the roaming folder): its bookmarks come over | pass | bookmark kept: True · 127-import-zen.png |
| 115 | Zen's sidebar comes too: its space is a space here, and the page pinned inside its folder is pinned in that space, in a folder of the same name | pass | space: True, pin in folder: True · 128-import-zen-spaces.png |
| 116 | Sync through a folder: at launch another PC's bookmark, space, pin and setting are here, and this PC's own file is written beside it | pass | bookmark/space/setting: True, pin in the space's session: True, own files written: True · 129-sync-applied.png |
| 117 | Settings › General says who it synced with | pass | status shown: True · 130-sync-settings.png |
| 118 | Settings › Keyboard & Mouse lists the commands with their keys | pass | New Tab listedKey: True · 131-shortcuts-page.png |
| 119 | Click a key: the box waits for the new one | pass | 132-shortcuts-waiting.png |
| 120 | Press Ctrl+Alt+K: New Tab is kept on that key, and the key is not taken as a command meanwhile | pass | shortcuts.json: {"file.newTab":"Ctrl\u002BAlt\u002BK"} · 133-shortcuts-changed.png |
| 121 | Ctrl+T no longer opens a tab; Ctrl+Alt+K does | pass | on Settings before: True, after Ctrl+T: True, after Ctrl+Alt+K: False · 134-shortcuts-works.png |
| 122 | Put every key back: the keys are as they came | pass | shortcuts.json: {} |
| 123 | Hover the tab: a copy-link button sits at the far right of the row, no second address bar above the pages | pass | copy button on row: True · 135-column-address.png |
| 124 | Clicking the tab you are on changes its address right there, no new tab; Copy link copied it | pass | tab titled Sign up: True, copied: True · 136-column-address-edit.png |
| 125 | The tab's right-click menu has Bookmark, Screenshot, Share and Style This Site; Bookmark saves the page | pass | items: True, bookmarked: True · 137-column-actions.png |
| 126 | Take Screenshot from the tab menu: the page is saved to Pictures › Screenshots and a card says so | pass | card: True, file: True · 138-column-screenshot.png |
| 127 | Dragging the column's edge as narrow as it goes: the reload button stays whole with a little space before the next one | pass | column's right button moved from 222 to 214, gap after reload: 4px · 139-column-narrowest.png |
| 128 | The foot has Settings at the right, and Update beside it when one is ready | pass | update at 160, settings at 188 · 140-column-foot.png |
| 129 | Hovering the update button: a small window shows the version and what is different | pass | popup shown: True · 141-update-popup.png |
| 130 | Clicking it downloads the update with a bar filling up | pass | downloading text shown: True · 142-update-downloading.png |
| 131 | When the download is done it says it is ready; a second click would restart | pass | ready text shown: True · 143-update-ready.png |
| 132 | Settings at the foot opens Settings | pass | Settings page shown: True |
| 133 | The Library door in the foot widens the column: a rail of six sections down its left, Spaces first with the space as a card | pass | rail buttons: 6, space card: True, invoked at {X=10,Y=729,Width=26,Height=26}; trace: [20:15:44.396] library spaces · 144-library-spaces.png |
| 134 | History in the rail: the history list shows inside the column, no tab opened | pass | search box shown: True · 145-library-history.png |
| 135 | Downloads in the rail: the downloads list shows inside the column | pass | search box shown: True · 146-library-downloads.png |
| 136 | Bookmarks in the rail: the saved page is listed inside the column | pass | search box shown: True · 147-library-bookmarks.png |
| 137 | The back arrow closes the Library: the column narrows to what it was and the tabs are back | pass | closed: True, reload at 158 then 158 · 148-library-closed.png |
| 138 | Ctrl+Shift+L opens the Library at the section it was left on, and again closes it | pass | opened: True, closed: True |
| 139 | The hide button takes the column away; the pointer at the window's edge brings it back and it leaves again with the pointer | pass | hidden: True, shown from the edge: True, left again: True · 149-column-hidden.png · 150-column-edge.png |
| 140 | Pressing the button while the column is out from the edge keeps it open | pass | still there after the pointer left: True · 151-column-kept.png |
| 141 | An extension kept in Search's folder is loaded: its content script has marked the page's title | pass | tab shows [ext]: True · 152-extension-content-script.png |
| 142 | Ctrl+Shift+E opens the Library at Extensions: the extension is listed with its switch | pass | library at Extensions: True, listed: True · 153-extension-hub.png |
| 143 | Open: the extension's popup appears in a small window | pass | popup text shown: True · 154-extension-popup.png |
| 144 | An address to a .crx in the box, Add: the package is downloaded, unpacked and listed | pass | listed: True · 155-extension-added.png |
| 145 | A Chrome Web Store link in the box, Add: the real store's package is downloaded, unpacked and listed | **FAIL** | not listed; page says:  · 156-extension-store.png |
| 146 | The switch turns an extension off | pass | kept as off: True · 157-extension-off.png |
| 147 | Remove takes the extension and its folder away | pass | gone: True; folders left:  · 158-extension-removed.png |
| 148 | Settings › Privacy › Proxy set to a server of its own: a page for a name that exists nowhere loads through it | pass | the proxy saw: GET http://proxy-probe.test/ HTTP/1.1 / GET http://proxy-probe.test/favicon.ico HTTP/1.1 · 159-proxy.png |
| 149 | Settings › Privacy › Proxy › User name and Password typed: the name is vaulted in prefs, the password protected in its own file and nowhere else | pass | settings has the fields: True, protected file: True · 160-proxy-credentials.png |
| 150 | A proxy that answers 407: Search signs in with the kept name and password, and the page loads | pass | the proxy saw: asked who / signed in / signed in · 161-proxy-signed-in.png |
| 151 | A site that asks for a certificate: a card lists yours, Continue sends it, and the site sees it | pass | card: True, server saw the certificate: True, page shows it: True · 162-client-certificate-card.png · 163-client-certificate-sent.png |
| 152 | Crash log is empty | pass |  |
