| # | Step | Result | Note / screenshot |
|---|---|---|---|
| 1 | Launch Search.exe with an empty data folder | pass | Search |
| 2 | Empty tab shows the centred address field | pass | 01-new-tab.png |
| 3 | Type "you": popular sites suggested, youtube.com completed inline | pass | 6 rows · 02-suggestions-known-sites.png |
| 4 | Type "weather paris": search row plus Google's own suggestions | pass | 03-suggestions-engine.png |
| 5 | Press Enter: Google search loads and the app stays open (reported crash) | pass | 04-google-search.png |
| 6 | Ctrl+T, type example.com, Enter: opens in a second tab | pass | 05-second-tab.png |
| 7 | Hover the first tab: soft grey row and a close button, no tooltip box | pass | 06-hover-tab.png |
| 8 | Right-click the tab: Search-styled menu with Pin, Rename, Duplicate… | pass | 07-tab-menu.png |
| 9 | Choose Pin: the tab becomes a square at the top of the column | pass | 08-pinned.png |
| 10 | Ctrl+L on a page: address editable in its row, suggestions over the page | pass | 09-edit-address.png |
| 11 | Ctrl+F, type domain: matches are highlighted on the page and counted | pass | popup says: 1 of 2 · 10-find.png |
| 12 | Ctrl+Shift+B: a card to name the bookmark opens, and the page is saved | pass | card says: Bookmark added · 11-bookmark-card.png |
| 13 | Ctrl+Alt+B: the bookmarks bar appears above the page with the bookmark in it | pass | 12-bookmarks-bar.png |
| 14 | Ctrl+= twice: the page is zoomed in | pass | 13-zoomed.png |
| 15 | Ctrl+Y: History opens as a page in a tab | pass | 14-history-page.png |
| 16 | Ctrl+Shift+J: Downloads opens as a page in a tab | pass | 15-downloads-page.png |
| 17 | Ctrl+, : Settings opens as a page in a tab | pass | 16-settings-page.png |
| 18 | Type http://example.com: it loads over https:// | pass | 17-https-upgrade.png |
| 19 | Type http://httpforever.com (no https there): it still loads, as written | pass | tab url: http://httpforever.com/ · 18-https-fallback.png |
| 20 | Open a link to a zip: it downloads to the Downloads folder and the downloads card floats up | pass | 1 file(s) in C:\Users\runneradmin\Downloads · 19-download-float.png |
| 21 | Ctrl+N: a second window opens | pass | 2 windows · 20-new-window.png |
| 22 | Close the second window: the first stays | pass | 21-back-to-first.png |
| 23 | Start Search again with a link: the running one opens it, no second copy stays | pass | 22-one-copy.png |
| 24 | Alt+F4: the window closes and the session is saved | pass | {"tabs":[{"url":"https://www.google.com/search?q=weather%20paris\u0026sei=N_gIa8h9-67k2g_j6-C4DQ","title":"weather paris - Google Search","pinId":"ae82b66c-65b4 |
| 25 | Open Search again: pinned page and tabs come back, pin keeps its icon | pass | pin saved: True, icons cached: True · 23-restored.png |
| 26 | Spaces on: the foot shows the space icons, Personal selected | pass | 24-spaces-strip.png |
| 27 | The foot has the Library at the left, Settings at the right, and the space you are in centred between them | pass | library ends 46, Personal centred at 127 (middle 126), settings starts 206 · 25-foot-centred.png |
| 28 | Ctrl+Alt+2: the foot's spaces slide so Work is in the middle | pass | library ends 46, Work centred at 127 (middle 126), settings starts 206 · 26-foot-work-centred.png |
| 29 | Ctrl+Alt+2: the Work space opens with its own empty row; a page opened there loads | pass | 27-space-work.png |
| 30 | Ctrl+Alt+1: back to Personal with its own tabs (the search, the pin) while Work's file holds only its own page | pass | 28-space-personal.png |
| 31 | New Space popup: name it Travel, Enter: the space is made and opened | pass | 29-space-new.png |
| 32 | Layout Top, pins as rows: tabs run along the top strip with the space icons, the column is gone | pass | 30-layout-top.png |
| 33 | An update that is ready shows as a disc in the strip, just before Settings | pass | update 832..858, settings at 860 · 31-strip-update-ready.png |
| 34 | The top strip has Windows' own window buttons at the top right: minimise, maximise, close, left to right | pass | min 920, max 954, close 988..1020 of a window ending at 1031 · 32-strip-caption-buttons.png |
| 35 | The top strip has Settings and the Library, and no Home or Downloads | pass | TopSettings True, TopLibraryDoor True |
| 36 | With tabs past the strip's width, the plus stays at the strip's right edge, no scrolling to reach it | pass | plus at 792..822, window right 1031 · 33-strip-many-tabs.png |
| 37 | Pressing the pinned plus opens one more tab | pass | 34-strip-plus-pressed.png |
| 38 | The strip's Settings button opens Settings, where Spaces & Profiles has a section of its own | pass | 35-settings-spaces-section.png |
| 39 | Right-click a space: its menu offers the Studio profile (there is no profile button in the strip) | pass | 36-profile-menu.png |
| 40 | Open the Studio profile: it opens in a window of its own with its own spaces | pass | 2 windows · 37-profile-window.png |
| 41 | A tab left alone for years, with Close tabs left alone set to a day: it is closed into the archive, the other stays | pass | archive.json holds: example.net · 38-archived-on-start.png |
| 42 | search://archive: the Archived Tabs page lists it, with Open and Remove | pass | 39-archive-page.png |
| 43 | Ctrl+Alt+N on a lone page: an empty page opens beside it, the address field ready; example.com loads in it | pass | splits: [2] · 40-split-two.png |
| 44 | Ctrl+Alt+Left: the left page takes the focus ring | pass | 41-split-focus-left.png |
| 45 | Drag the line between the pages: the left one gets narrower, the right one wider | pass | before 0.50/0.50, after 0.24/0.76 · 42-split-resized.png |
| 46 | Alt+click two more tabs: they join the split; four pages share the room | pass | splits: [4] · 43-split-four.png |
| 47 | Ctrl+Alt+N inside a split: the pages come apart and stay as tabs | pass | 44-split-separated.png |
| 48 | Shift+click a link: its page opens over this one in a panel, the page behind dimmed | pass | link found: True, panel with Open as tab: True, browser started:  · 45-peek.png |
| 49 | Esc: the peek goes away | pass | 46-peek-closed.png |
| 50 | Alt+click a link (Settings › Alt+click opens in Split View): the link loads beside the page | pass | splits: [2] · 47-split-link.png |
| 51 | A link from another app, with Little windows on: it opens in a small window, not a tab | pass | small window with Open in Search: True · 48-little-window.png |
| 52 | Snap layouts: asked what is under the pointer over the maximise button, the window says HTMAXBUTTON (9) | pass | over the button: 9, elsewhere: 1 |
| 53 | Quit with two windows (Ctrl+Shift+Q), open Search again: both windows come back, each with its own tabs | pass | windows.json lists one: True, windows open: 2, window session files: 1 · 49-windows-restored.png |
| 54 | Tab menu › Add to Group › New Group, type Research: a heading appears above the tab, the group is saved | pass | groups: [{"id":"f8cbc875-2128-4d60-a6e2-49311aa2348e","name":"Research","collapsed":false}] · 50-group-new.png |
| 55 | Add the third tab to Research from its menu: two tabs sit under the heading | pass | members: 2 · 51-group-two.png |
| 56 | Click the heading: the group folds to its name and a count | pass | collapsed saved: True, tab rows left: 3 · 52-group-folded.png |
| 57 | Drag a loose tab onto the Research heading: it joins the group | pass | members: 3 · from {X=110,Y=72} to {X=123,Y=69} trace: drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drop on heading / drag ended Move · 53-group-dragging.png · 54-group-drop.png |
| 58 | Pin a tab, its menu › Move to Folder › New Folder, type Work: a folder heading holds the pin | pass | folders: [{"id":"7a2523b3-d21d-43c0-aba2-92e098e67d8f","name":"Work","collapsed":false}] · 55-folder-new.png |
| 59 | Right-click the folder › New Folder Inside, type Deep: a folder inside a folder, set further in | pass | folders: [{"id":"7a2523b3-d21d-43c0-aba2-92e098e67d8f","name":"Work","collapsed":false},{"id":"605dabad-9faf-4b86-9d35-14199172e092","name":"Deep","parent":"7a2523b3-d21d-43c0-aba2-92e098e67d8f","collapsed":false}] · 56-folder-nested.png |
| 60 | Quit and open again: the group, the folder and the folder inside it are still there | pass | headings found: Research True, Work True, Deep True · 57-groups-restored.png |
| 61 | Ctrl+Shift+S: the column folds away (a still shot straight after, then settled) | pass | column gone: True · 58-column-folding.png · 59-column-folded.png |
| 62 | Ctrl+Shift+S again: the column comes back | pass | 60-column-back.png |
| 63 | Ctrl+Alt+2: the rows of the next space slide in from the side (a still straight after, then settled) | pass | 61-space-swiping.png · 62-space-swiped.png |
| 64 | Ctrl+2: the page dips as the next tab comes up, its row takes the wash (a still straight after, then settled) | pass | 63-tab-switching.png · 64-tab-switched.png |
| 65 | A page asks for your location: a card over the page asks, Allow is kept for that site | pass | card shown: True, kept: {"sites":{"http://localhost:18974":{"Location":0}}} · 65-permission-card.png |
| 66 | Asked again: no card, the kept answer is used | pass | 66-permission-remembered.png |
| 67 | A page asks to show notifications, then sends one: the card names the site and the message | pass | asked: True, shown: True · 67-notification.png |
| 68 | Block trackers on: a page asking for a doubleclick.net address is never sent it | pass | trace: tracker blocked https://doubleclick.net/x |
| 69 | Tab menu › Site Information: the site, how it is connected and what it may use | pass | 68-site-information.png |
| 70 | Settings › Sites lists what the site was allowed | pass | 69-settings-sites.png |
| 71 | Forget takes the answer back | pass | permissions.json: {"sites":{}} |
| 72 | Ctrl+Alt+Shift+C on a page with utm_ and fbclid in its address, pasted into a new tab: only id=7 is left | pass | session: http://localhost:18974/?id=7 · 70-clean-link.png |
| 73 | Type youtube, Tab, cats, Enter: YouTube's own search for cats opens | pass | 71-site-search-chip.png · 72-site-search-result.png |
| 74 | A style kept for localhost (Settings › Sites) is on its pages: the background reads rgb(1, 2, 3) | pass | 73-site-style.png |
| 75 | Ctrl+Alt+Shift+R on an article: reading mode opens with its three paragraphs, the links around it left out | pass | paragraphs: 3 · 74-reading-mode.png |
| 76 | Ctrl+Shift+P on a page with a playing video: it floats in a picture-in-picture window | pass | [03:33:26.390] pip {"result":{"type":"string","value":"opened"}} · 75-picture-in-picture.png |
| 77 | A live folder (pin folder with a feed address): its latest posts fill it a few seconds after launch | pass | feed: http://localhost:18974/feed.xml · 76-live-folder.png |
| 78 | Click a post: its page opens in a new tab | pass | session has /post1: True · 77-live-folder-post.png |
| 79 | Heading menu › New Live Folder Inside…, a site's address (not the feed's): the feed it points to is found and a folder named after it is made | pass | address box: True, folder made: True · 78-live-folder-new.png |
| 80 | A sign-in sent and settled: a card offers to keep the password; Save puts it in the vault, protected, never in the file as written | pass | card: True, kept: True, file shows the password: False, trace: form form.focus sign-in localhost / form form.focus sign-in localhost / form form.submit  localhost / form form.submit  localhost · 79-password-save-card.png |
| 81 | Back on the sign-in: the caret in the box offers the kept account; Fill puts the name and password in through the field's own setter | pass | card: True, page title shows both filled: True · 80-password-fill-card.png |
| 82 | Settings › Passwords: the account is listed; Show (Windows Hello, or straight through where none is set up) reveals the password | pass | listed: True, shown: True · 81-settings-passwords.png |
| 83 | Settings › Passwords › Passkeys reports whether this PC can hold passkeys (the web engine's WebAuthn answers; the signing itself needs a person at Windows Hello) | pass | status: Windows Hello is not set up on this PC; a security key still works. Passkeys are offered in sign-in boxes |
| 84 | Forget removes it from the vault | pass | vault.json: {   "items": [],   "never": [] } |
| 85 | A sign-up's new-password box: a strong password is offered and fills both boxes | pass | card: True, both boxes hold the same 20 characters: True, trace: form form.focus sign-in localhost / password fill done / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus new localhost · 82-password-strong.png |
| 86 | A contact form's email box offers your card (Settings › Passwords › My card); Fill puts name, email and postal code in the empty boxes and leaves the one already typed alone | pass | card: True, boxes: True · 83-card-offer.png · 84-card-filled.png |
| 87 | Settings › Import lists Chrome; Import brings over its bookmarks (a folder keeps its name), history and saved passwords; the vault holds the password protected, not as written | pass | sources listed: True, imported: True, folder kept: True, file shows the password: False · 85-import-sources.png · 86-import-chrome.png |
| 88 | The same for Zen (a Firefox-family profile in the roaming folder): its bookmarks come over | pass | bookmark kept: True · 87-import-zen.png |
| 89 | Zen's sidebar comes too: its space is a space here, and the page pinned inside its folder is pinned in that space, in a folder of the same name | pass | space: True, pin in folder: True · 88-import-zen-spaces.png |
| 90 | Sync through a folder: at launch another PC's bookmark, space, pin and setting are here, and this PC's own file is written beside it | pass | bookmark/space/setting: True, pin in the space's session: True, own files written: True · 89-sync-applied.png |
| 91 | Settings › General says who it synced with | pass | status shown: True · 90-sync-settings.png |
| 92 | Settings › Keyboard & Mouse lists the commands with their keys | pass | New Tab listedKey: True · 91-shortcuts-page.png |
| 93 | Click a key: the box waits for the new one | pass | 92-shortcuts-waiting.png |
| 94 | Press Ctrl+Alt+K: New Tab is kept on that key, and the key is not taken as a command meanwhile | pass | shortcuts.json: {"file.newTab":"Ctrl\u002BAlt\u002BK"} · 93-shortcuts-changed.png |
| 95 | Ctrl+T no longer opens a tab; Ctrl+Alt+K does | pass | on Settings before: True, after Ctrl+T: True, after Ctrl+Alt+K: False · 94-shortcuts-works.png |
| 96 | Put every key back: the keys are as they came | pass | shortcuts.json: {} |
| 97 | Hover the tab: a copy-link button sits at the far right of the row, no second address bar above the pages | pass | copy button on row: True · 95-column-address.png |
| 98 | Clicking the tab you are on changes its address right there, no new tab; Copy link copied it | pass | tab titled Sign up: True, copied: True · 96-column-address-edit.png |
| 99 | The tab's right-click menu has Bookmark, Screenshot, Share and Style This Site; Bookmark saves the page | pass | items: True, bookmarked: True · 97-column-actions.png |
| 100 | Take Screenshot from the tab menu: the page is saved to Pictures › Screenshots and a card says so | pass | card: True, file: True · 98-column-screenshot.png |
| 101 | Dragging the column's edge as narrow as it goes: the reload button stays whole with a little space before the next one | pass | column's right button moved from 222 to 214, gap after reload: 4px · 99-column-narrowest.png |
| 102 | The foot has Settings at the right, and Update beside it when one is ready | pass | update at 160, settings at 188 · 100-column-foot.png |
| 103 | Hovering the update button: a small window shows the version and what is different | pass | popup shown: True · 101-update-popup.png |
| 104 | Clicking it downloads the update with a bar filling up | pass | downloading text shown: True · 102-update-downloading.png |
| 105 | When the download is done it says it is ready; a second click would restart | pass | ready text shown: True · 103-update-ready.png |
| 106 | Settings at the foot opens Settings | pass | Settings page shown: True |
| 107 | The Library door in the foot widens the column: a rail of six sections down its left, Spaces first with the space as a card | pass | rail buttons: 6, space card: True, invoked at {X=10,Y=729,Width=26,Height=26}; trace: [03:36:14.773] library spaces · 104-library-spaces.png |
| 108 | History in the rail: the history list shows inside the column, no tab opened | pass | search box shown: True · 105-library-history.png |
| 109 | Downloads in the rail: the downloads list shows inside the column | pass | search box shown: True · 106-library-downloads.png |
| 110 | Bookmarks in the rail: the saved page is listed inside the column | pass | search box shown: True · 107-library-bookmarks.png |
| 111 | The back arrow closes the Library: the column narrows to what it was and the tabs are back | pass | closed: True, reload at 158 then 158 · 108-library-closed.png |
| 112 | Ctrl+Shift+L opens the Library at the section it was left on, and again closes it | pass | opened: True, closed: True |
| 113 | The hide button takes the column away; the pointer at the window's edge brings it back and it leaves again with the pointer | pass | hidden: True, shown from the edge: True, left again: True · 109-column-hidden.png · 110-column-edge.png |
| 114 | Pressing the button while the column is out from the edge keeps it open | pass | still there after the pointer left: True · 111-column-kept.png |
| 115 | An extension kept in Search's folder is loaded: its content script has marked the page's title | pass | tab shows [ext]: True · 112-extension-content-script.png |
| 116 | Ctrl+Shift+E opens the Library at Extensions: the extension is listed with its switch | pass | library at Extensions: True, listed: True · 113-extension-hub.png |
| 117 | Open: the extension's popup appears in a small window | pass | popup text shown: True · 114-extension-popup.png |
| 118 | An address to a .crx in the box, Add: the package is downloaded, unpacked and listed | pass | listed: True · 115-extension-added.png |
| 119 | A Chrome Web Store link in the box, Add: the real store's package is downloaded, unpacked and listed | pass | listed · 116-extension-store.png |
| 120 | The switch turns an extension off | pass | kept as off: True · 117-extension-off.png |
| 121 | Remove takes the extension and its folder away | pass | gone: True; folders left: seeded0001 · 118-extension-removed.png |
| 122 | Settings › Privacy › Proxy set to a server of its own: a page for a name that exists nowhere loads through it | pass | the proxy saw: GET http://proxy-probe.test/ HTTP/1.1 / GET http://proxy-probe.test/favicon.ico HTTP/1.1 · 119-proxy.png |
| 123 | Settings › Privacy › Proxy › User name and Password typed: the name is vaulted in prefs, the password protected in its own file and nowhere else | pass | settings has the fields: True, protected file: True · 120-proxy-credentials.png |
| 124 | A proxy that answers 407: Search signs in with the kept name and password, and the page loads | pass | the proxy saw: asked who / signed in / signed in · 121-proxy-signed-in.png |
| 125 | A site that asks for a certificate: a card lists yours, Continue sends it, and the site sees it | pass | card: True, server saw the certificate: True, page shows it: True · 122-client-certificate-card.png · 123-client-certificate-sent.png |
| 126 | Crash log is empty | pass |  |
