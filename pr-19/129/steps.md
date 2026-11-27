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
| 25 | Alt+F4: the window closes and the session is saved | pass | {"tabs":[{"url":"https://www.google.com/search?q=weather%20paris\u0026sei=eBsJa-asCtfZ5NoPy7eD8Qk#cobssid=s","title":"weather paris - Google Search","pinId":"c2 |
| 26 | Open Search again: pinned page and tabs come back, pin keeps its icon | pass | pin saved: True, icons cached: True · 25-restored.png |
| 27 | Spaces on: the foot shows the space icons, Personal selected | pass | 26-spaces-strip.png |
| 28 | The foot has the Library at the left, Settings at the right, and the space you are in centred between them | pass | library ends 46, Personal centred at 127 (middle 126), settings starts 206 · 27-foot-centred.png |
| 29 | Ctrl+Alt+2: the foot's spaces slide so Work is in the middle | pass | library ends 46, Work centred at 127 (middle 126), settings starts 206 · 28-foot-work-centred.png |
| 30 | Ctrl+Alt+2: the Work space opens with its own empty row; a page opened there loads | pass | 29-space-work.png |
| 31 | Ctrl+Alt+1: back to Personal with its own tabs (the search, the pin) while Work's file holds only its own page | pass | 30-space-personal.png |
| 32 | New Space popup: name it Travel, Enter: the space is made and opened | pass | 31-space-new.png |
| 33 | Layout Top, pins as rows: tabs run along the top strip with the space icons, the column is gone | pass | 32-layout-top.png |
| 34 | An update that is ready shows as a disc in the strip, just before Settings | pass | update 832..858, settings at 860 · 33-strip-update-ready.png |
| 35 | The top strip has Windows' own window buttons at the top right: minimise, maximise, close, left to right | pass | min 920, max 954, close 988..1020 of a window ending at 1031 · 34-strip-caption-buttons.png |
| 36 | The top strip has Settings and the Library, and no Home or Downloads | pass | TopSettings True, TopLibraryDoor True |
| 37 | With more tabs open along the top the tabs get narrower than their full width, and not narrower than a tab can read | pass | 1 resizes, last: 72px for 8 tabs (172 when there is room) · 35-strip-tabs-narrower.png |
| 38 | With tabs past the strip's width, the plus stays at the strip's right edge, no scrolling to reach it | pass | plus at 798..828, window right 1031 · 36-strip-many-tabs.png |
| 39 | Pressing the pinned plus opens one more tab | pass | 37-strip-plus-pressed.png |
| 40 | The strip's Settings button opens Settings, where Spaces & Profiles has a section of its own | pass | 38-settings-spaces-section.png |
| 41 | Right-click a space: its menu offers the Studio profile (there is no profile button in the strip) | pass | 39-profile-menu.png |
| 42 | Open the Studio profile: it opens in a window of its own with its own spaces | pass | 2 windows · 40-profile-window.png |
| 43 | A tab left alone for years, with Close tabs left alone set to a day: it is closed into the archive, the other stays | pass | archive.json holds: example.net · 41-archived-on-start.png |
| 44 | search://archive: the Archived Tabs page lists it, with Open and Remove | pass | 42-archive-page.png |
| 45 | Ctrl+Alt+N on a lone page: an empty page opens beside it, the address field ready; example.com loads in it | pass | splits: [2] · 43-split-two.png |
| 46 | Ctrl+Alt+Left: the left page takes the focus ring | pass | 44-split-focus-left.png |
| 47 | Drag the line between the pages: the left one gets narrower, the right one wider | pass | before 0.50/0.50, after 0.24/0.76 · 45-split-resized.png |
| 48 | Alt+click two more tabs: they join the split; four pages share the room | pass | splits: [4] · 46-split-four.png |
| 49 | Ctrl+Alt+N inside a split: the pages come apart and stay as tabs | pass | 47-split-separated.png |
| 50 | Shift+click a link: its page opens over this one in a panel, the page behind dimmed | pass | link found: True, panel with Open as tab: True, browser started:  · 48-peek.png |
| 51 | Esc: the peek goes away | pass | 49-peek-closed.png |
| 52 | Alt+click a link (Settings › Alt+click opens in Split View): the link loads beside the page | pass | splits: [2] · 50-split-link.png |
| 53 | A link from another app, with Little windows on: it opens in a small window, not a tab | pass | small window with Open in Search: True · 51-little-window.png |
| 54 | Snap layouts: asked what is under the pointer over the maximise button, the window says HTMAXBUTTON (9) | pass | over the button: 9, elsewhere: 1 |
| 55 | Quit with two windows (Ctrl+Shift+Q), open Search again: both windows come back, each with its own tabs | pass | windows.json lists one: True, windows open: 2, window session files: 1 · 52-windows-restored.png |
| 56 | Drag a tab from the column onto the middle of another: the two are shown side by side in Split View | pass | splits before 0, after [2] trace: drag start Example Domain / drag hits 133 TextBlock:T@131 Border:T@125 Border:T@125 Border:T@125 / drag hits 133 Border:T@107 Border:T@107 Border:T@107 / drag hits 131 Border:T@107 Border:T@107 Border:T@107 / drag hits 129 TextBlock:T@113 Border:T@107 Border:T@107 Border:T@107 / drag hits 127 TextBlock:T@113 Border:T@107 Border:T@107 Border:T@107 / drag hits 127 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 126 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 125 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 124 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 123 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 122 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 121 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / drag hits 120 TextBlock:T@112 Border:T@108 Border:T@108 Border:T@107 / split by drop Example Domain beside Example Domain / drag ended Move · 53-split-drag-over.png · 54-split-by-drag.png |
| 57 | Tab menu › Add to Group › New Group, type Research: a heading appears above the tab, the group is saved | pass | groups: [{"id":"087c1a2a-e091-4d7d-aca2-4e371f666dda","name":"Research","collapsed":false}] · 55-group-new.png |
| 58 | Add the third tab to Research from its menu: two tabs sit under the heading | pass | members: 2 · 56-group-two.png |
| 59 | Click the heading: the group folds to its name and a count | pass | collapsed saved: True, tab rows left: 4 · 57-group-folded.png |
| 60 | Drag a loose tab onto the Research heading: it joins the group | pass | members: 3 · from {X=110,Y=72} to {X=123,Y=69} trace: drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drop on heading / drag ended Move · 58-group-dragging.png · 59-group-drop.png |
| 61 | Pin a tab, its menu › Move to Folder › New Folder, type Work: a folder heading holds the pin | pass | folders: [{"id":"db98a3be-fb5e-4d04-a4a6-84d913489c13","name":"Work","collapsed":false}] · 60-folder-new.png |
| 62 | Right-click the folder › New Folder Inside, type Deep: a folder inside a folder, set further in | pass | folders: [{"id":"db98a3be-fb5e-4d04-a4a6-84d913489c13","name":"Work","collapsed":false},{"id":"4d385668-dfe8-441e-b473-0163735c7053","name":"Deep","parent":"db98a3be-fb5e-4d04-a4a6-84d913489c13","collapsed":false}] · 61-folder-nested.png |
| 63 | Quit and open again: the group, the folder and the folder inside it are still there | pass | headings found: Research True, Work True, Deep True · 62-groups-restored.png |
| 64 | Ctrl+Shift+S: the column folds away (a still shot straight after, then settled) | pass | column gone: True · 63-column-folding.png · 64-column-folded.png |
| 65 | Ctrl+Shift+S again: the column comes back | pass | 65-column-back.png |
| 66 | Ctrl+Alt+2: the rows of the next space slide in from the side (a still straight after, then settled) | pass | 66-space-swiping.png · 67-space-swiped.png |
| 67 | Ctrl+2: the page dips as the next tab comes up, its row takes the wash (a still straight after, then settled) | pass | 68-tab-switching.png · 69-tab-switched.png |
| 68 | A page asks for your location: a card over the page asks, Allow is kept for that site | pass | card shown: True, kept: {"sites":{"http://localhost:18942":{"Location":0}}} · 70-permission-card.png |
| 69 | Asked again: no card, the kept answer is used | pass | 71-permission-remembered.png |
| 70 | A page asks to show notifications, then sends one: the card names the site and the message | pass | asked: True, shown: True · 72-notification.png |
| 71 | Block trackers on: a page asking for a doubleclick.net address is never sent it | pass | trace: tracker blocked https://doubleclick.net/x |
| 72 | Tab menu › Site Information: the site, how it is connected and what it may use | pass | 73-site-information.png |
| 73 | Settings › Sites lists what the site was allowed | pass | 74-settings-sites.png |
| 74 | Forget takes the answer back | pass | permissions.json: {"sites":{}} |
| 75 | Ctrl+Alt+Shift+C on a page with utm_ and fbclid in its address, pasted into a new tab: only id=7 is left | pass | session: http://localhost:18942/?id=7 · 75-clean-link.png |
| 76 | Type youtube, Tab, cats, Enter: YouTube's own search for cats opens | pass | 76-site-search-chip.png · 77-site-search-result.png |
| 77 | A style kept for localhost (Settings › Sites) is on its pages: the background reads rgb(1, 2, 3) | pass | 78-site-style.png |
| 78 | Ctrl+Alt+Shift+R on an article: reading mode opens with its three paragraphs, the links around it left out | pass | paragraphs: 3 · 79-reading-mode.png |
| 79 | Ctrl+Shift+P on a page with a playing video: it floats in a picture-in-picture window | pass | [06:04:18.024] pip {"result":{"type":"string","value":"opened"}} · 80-picture-in-picture.png |
| 80 | A live folder (pin folder with a feed address): its latest posts fill it a few seconds after launch | pass | feed: http://localhost:18942/feed.xml · 81-live-folder.png |
| 81 | Click a post: its page opens in a new tab | pass | session has /post1: True · 82-live-folder-post.png |
| 82 | Heading menu › New Live Folder Inside…, a site's address (not the feed's): the feed it points to is found and a folder named after it is made | pass | address box: True, folder made: True · 83-live-folder-new.png |
| 83 | A sign-in sent and settled: a card offers to keep the password; Save puts it in the vault, protected, never in the file as written | pass | card: True, kept: True, file shows the password: False, trace: form form.focus sign-in localhost / form form.focus sign-in localhost / form form.submit  localhost / form form.submit  localhost · 84-password-save-card.png |
| 84 | Back on the sign-in: the caret in the box offers the kept account; Fill puts the name and password in through the field's own setter | pass | card: True, page title shows both filled: True · 85-password-fill-card.png |
| 85 | Settings › Passwords: the account is listed; Show (Windows Hello, or straight through where none is set up) reveals the password | pass | listed: True, shown: True · 86-settings-passwords.png |
| 86 | Settings › Passwords › Passkeys reports whether this PC can hold passkeys (the web engine's WebAuthn answers; the signing itself needs a person at Windows Hello) | pass | status: Windows Hello is not set up on this PC; a security key still works. Passkeys are offered in sign-in boxes |
| 87 | Forget removes it from the vault | pass | vault.json: {   "items": [],   "never": [] } |
| 88 | A sign-up's new-password box: a strong password is offered and fills both boxes | pass | card: True, both boxes hold the same 20 characters: True, trace: form form.focus sign-in localhost / password fill done / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus new localhost · 87-password-strong.png |
| 89 | A contact form's email box offers your card (Settings › Passwords › My card); Fill puts name, email and postal code in the empty boxes and leaves the one already typed alone | pass | card: True, boxes: True · 88-card-offer.png · 89-card-filled.png |
| 90 | Settings › Import lists Chrome; Import brings over its bookmarks (a folder keeps its name), history and saved passwords; the vault holds the password protected, not as written | pass | sources listed: True, imported: True, folder kept: True, file shows the password: False · 90-import-sources.png · 91-import-chrome.png |
| 91 | The same for Zen (a Firefox-family profile in the roaming folder): its bookmarks come over | pass | bookmark kept: True · 92-import-zen.png |
| 92 | Zen's sidebar comes too: its space is a space here, and the page pinned inside its folder is pinned in that space, in a folder of the same name | pass | space: True, pin in folder: True · 93-import-zen-spaces.png |
| 93 | Sync through a folder: at launch another PC's bookmark, space, pin and setting are here, and this PC's own file is written beside it | pass | bookmark/space/setting: True, pin in the space's session: True, own files written: True · 94-sync-applied.png |
| 94 | Settings › General says who it synced with | pass | status shown: True · 95-sync-settings.png |
| 95 | Settings › Keyboard & Mouse lists the commands with their keys | pass | New Tab listedKey: True · 96-shortcuts-page.png |
| 96 | Click a key: the box waits for the new one | pass | 97-shortcuts-waiting.png |
| 97 | Press Ctrl+Alt+K: New Tab is kept on that key, and the key is not taken as a command meanwhile | pass | shortcuts.json: {"file.newTab":"Ctrl\u002BAlt\u002BK"} · 98-shortcuts-changed.png |
| 98 | Ctrl+T no longer opens a tab; Ctrl+Alt+K does | pass | on Settings before: True, after Ctrl+T: True, after Ctrl+Alt+K: False · 99-shortcuts-works.png |
| 99 | Put every key back: the keys are as they came | pass | shortcuts.json: {} |
| 100 | Hover the tab: a copy-link button sits at the far right of the row, no second address bar above the pages | pass | copy button on row: True · 100-column-address.png |
| 101 | Clicking the tab you are on changes its address right there, no new tab; Copy link copied it | pass | tab titled Sign up: True, copied: True · 101-column-address-edit.png |
| 102 | The tab's right-click menu has Bookmark, Screenshot, Share and Style This Site; Bookmark saves the page | pass | items: True, bookmarked: True · 102-column-actions.png |
| 103 | Take Screenshot from the tab menu: the page is saved to Pictures › Screenshots and a card says so | pass | card: True, file: True · 103-column-screenshot.png |
| 104 | Dragging the column's edge as narrow as it goes: the reload button stays whole with a little space before the next one | pass | column's right button moved from 222 to 214, gap after reload: 4px · 104-column-narrowest.png |
| 105 | The foot has Settings at the right, and Update beside it when one is ready | pass | update at 160, settings at 188 · 105-column-foot.png |
| 106 | Hovering the update button: a small window shows the version and what is different | pass | popup shown: True · 106-update-popup.png |
| 107 | Clicking it downloads the update with a bar filling up | pass | downloading text shown: True · 107-update-downloading.png |
| 108 | When the download is done it says it is ready; a second click would restart | pass | ready text shown: True · 108-update-ready.png |
| 109 | Settings at the foot opens Settings | pass | Settings page shown: True |
| 110 | The Library door in the foot widens the column: a rail of six sections down its left, Spaces first with the space as a card | pass | rail buttons: 6, space card: True, invoked at {X=10,Y=729,Width=26,Height=26}; trace: [06:07:04.053] library spaces · 109-library-spaces.png |
| 111 | History in the rail: the history list shows inside the column, no tab opened | pass | search box shown: True · 110-library-history.png |
| 112 | Downloads in the rail: the downloads list shows inside the column | pass | search box shown: True · 111-library-downloads.png |
| 113 | Bookmarks in the rail: the saved page is listed inside the column | pass | search box shown: True · 112-library-bookmarks.png |
| 114 | The back arrow closes the Library: the column narrows to what it was and the tabs are back | pass | closed: True, reload at 158 then 158 · 113-library-closed.png |
| 115 | Ctrl+Shift+L opens the Library at the section it was left on, and again closes it | pass | opened: True, closed: True |
| 116 | The hide button takes the column away; the pointer at the window's edge brings it back and it leaves again with the pointer | pass | hidden: True, shown from the edge: True, left again: True · 114-column-hidden.png · 115-column-edge.png |
| 117 | Pressing the button while the column is out from the edge keeps it open | pass | still there after the pointer left: True · 116-column-kept.png |
| 118 | An extension kept in Search's folder is loaded: its content script has marked the page's title | pass | tab shows [ext]: True · 117-extension-content-script.png |
| 119 | Ctrl+Shift+E opens the Library at Extensions: the extension is listed with its switch | pass | library at Extensions: True, listed: True · 118-extension-hub.png |
| 120 | Open: the extension's popup appears in a small window | pass | popup text shown: True · 119-extension-popup.png |
| 121 | An address to a .crx in the box, Add: the package is downloaded, unpacked and listed | pass | listed: True · 120-extension-added.png |
| 122 | A Chrome Web Store link in the box, Add: the real store's package is downloaded, unpacked and listed | pass | listed · 121-extension-store.png |
| 123 | The switch turns an extension off | pass | kept as off: True · 122-extension-off.png |
| 124 | Remove takes the extension and its folder away | pass | gone: True; folders left: seeded0001 · 123-extension-removed.png |
| 125 | Settings › Privacy › Proxy set to a server of its own: a page for a name that exists nowhere loads through it | pass | the proxy saw: GET http://proxy-probe.test/ HTTP/1.1 / GET http://proxy-probe.test/favicon.ico HTTP/1.1 · 124-proxy.png |
| 126 | Settings › Privacy › Proxy › User name and Password typed: the name is vaulted in prefs, the password protected in its own file and nowhere else | pass | settings has the fields: True, protected file: True · 125-proxy-credentials.png |
| 127 | A proxy that answers 407: Search signs in with the kept name and password, and the page loads | pass | the proxy saw: asked who / signed in / signed in · 126-proxy-signed-in.png |
| 128 | A site that asks for a certificate: a card lists yours, Continue sends it, and the site sees it | pass | card: True, server saw the certificate: True, page shows it: True · 127-client-certificate-card.png · 128-client-certificate-sent.png |
| 129 | Crash log is empty | pass |  |
