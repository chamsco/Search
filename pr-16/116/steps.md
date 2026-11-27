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
| 24 | Alt+F4: the window closes and the session is saved | pass | {"tabs":[{"url":"https://www.google.com/search?q=weather%20paris\u0026sei=5PoIa9HHN8KRm9cPw6zjuQ0","title":"weather paris - Google Search","pinId":"2e286fcb-797 |
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
| 36 | Four more tabs along the top: the tabs get narrower and everything still fits, so the plus stays after the last tab | **FAIL** | pinned plus 798 · 33-strip-tabs-narrower.png |
| 37 | With tabs past the strip's width, the plus stays at the strip's right edge, no scrolling to reach it | pass | plus at 798..828, window right 1031 · 34-strip-many-tabs.png |
| 38 | Pressing the pinned plus opens one more tab | pass | 35-strip-plus-pressed.png |
| 39 | The strip's Settings button opens Settings, where Spaces & Profiles has a section of its own | pass | 36-settings-spaces-section.png |
| 40 | Right-click a space: its menu offers the Studio profile (there is no profile button in the strip) | pass | 37-profile-menu.png |
| 41 | Open the Studio profile: it opens in a window of its own with its own spaces | pass | 2 windows · 38-profile-window.png |
| 42 | A tab left alone for years, with Close tabs left alone set to a day: it is closed into the archive, the other stays | pass | archive.json holds: example.net · 39-archived-on-start.png |
| 43 | search://archive: the Archived Tabs page lists it, with Open and Remove | pass | 40-archive-page.png |
| 44 | Ctrl+Alt+N on a lone page: an empty page opens beside it, the address field ready; example.com loads in it | pass | splits: [2] · 41-split-two.png |
| 45 | Ctrl+Alt+Left: the left page takes the focus ring | pass | 42-split-focus-left.png |
| 46 | Drag the line between the pages: the left one gets narrower, the right one wider | pass | before 0.50/0.50, after 0.24/0.76 · 43-split-resized.png |
| 47 | Alt+click two more tabs: they join the split; four pages share the room | pass | splits: [4] · 44-split-four.png |
| 48 | Ctrl+Alt+N inside a split: the pages come apart and stay as tabs | pass | 45-split-separated.png |
| 49 | Shift+click a link: its page opens over this one in a panel, the page behind dimmed | pass | link found: True, panel with Open as tab: True, browser started:  · 46-peek.png |
| 50 | Esc: the peek goes away | pass | 47-peek-closed.png |
| 51 | Alt+click a link (Settings › Alt+click opens in Split View): the link loads beside the page | pass | splits: [2] · 48-split-link.png |
| 52 | A link from another app, with Little windows on: it opens in a small window, not a tab | pass | small window with Open in Search: True · 49-little-window.png |
| 53 | Snap layouts: asked what is under the pointer over the maximise button, the window says HTMAXBUTTON (9) | pass | over the button: 9, elsewhere: 1 |
| 54 | Quit with two windows (Ctrl+Shift+Q), open Search again: both windows come back, each with its own tabs | pass | windows.json lists one: True, windows open: 2, window session files: 1 · 50-windows-restored.png |
| 55 | Tab menu › Add to Group › New Group, type Research: a heading appears above the tab, the group is saved | pass | groups: [{"id":"8526cef8-25b1-436d-8459-acc93e5641f9","name":"Research","collapsed":false}] · 51-group-new.png |
| 56 | Add the third tab to Research from its menu: two tabs sit under the heading | pass | members: 2 · 52-group-two.png |
| 57 | Click the heading: the group folds to its name and a count | pass | collapsed saved: True, tab rows left: 3 · 53-group-folded.png |
| 58 | Drag a loose tab onto the Research heading: it joins the group | pass | members: 3 · from {X=110,Y=72} to {X=123,Y=69} trace: drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drop on heading / drag ended Move · 54-group-dragging.png · 55-group-drop.png |
| 59 | Pin a tab, its menu › Move to Folder › New Folder, type Work: a folder heading holds the pin | pass | folders: [{"id":"2e487f2c-ea32-49f7-a536-deebfefc0f41","name":"Work","collapsed":false}] · 56-folder-new.png |
| 60 | Right-click the folder › New Folder Inside, type Deep: a folder inside a folder, set further in | pass | folders: [{"id":"2e487f2c-ea32-49f7-a536-deebfefc0f41","name":"Work","collapsed":false},{"id":"4ebe8cfd-8152-4821-8f3c-9a470cc7a21e","name":"Deep","parent":"2e487f2c-ea32-49f7-a536-deebfefc0f41","collapsed":false}] · 57-folder-nested.png |
| 61 | Quit and open again: the group, the folder and the folder inside it are still there | pass | headings found: Research True, Work True, Deep True · 58-groups-restored.png |
| 62 | Ctrl+Shift+S: the column folds away (a still shot straight after, then settled) | pass | column gone: True · 59-column-folding.png · 60-column-folded.png |
| 63 | Ctrl+Shift+S again: the column comes back | pass | 61-column-back.png |
| 64 | Ctrl+Alt+2: the rows of the next space slide in from the side (a still straight after, then settled) | pass | 62-space-swiping.png · 63-space-swiped.png |
| 65 | Ctrl+2: the page dips as the next tab comes up, its row takes the wash (a still straight after, then settled) | pass | 64-tab-switching.png · 65-tab-switched.png |
| 66 | A page asks for your location: a card over the page asks, Allow is kept for that site | pass | card shown: True, kept: {"sites":{"http://localhost:18956":{"Location":0}}} · 66-permission-card.png |
| 67 | Asked again: no card, the kept answer is used | pass | 67-permission-remembered.png |
| 68 | A page asks to show notifications, then sends one: the card names the site and the message | pass | asked: True, shown: True · 68-notification.png |
| 69 | Block trackers on: a page asking for a doubleclick.net address is never sent it | pass | trace: tracker blocked https://doubleclick.net/x |
| 70 | Tab menu › Site Information: the site, how it is connected and what it may use | pass | 69-site-information.png |
| 71 | Settings › Sites lists what the site was allowed | pass | 70-settings-sites.png |
| 72 | Forget takes the answer back | pass | permissions.json: {"sites":{}} |
| 73 | Ctrl+Alt+Shift+C on a page with utm_ and fbclid in its address, pasted into a new tab: only id=7 is left | pass | session: http://localhost:18956/?id=7 · 71-clean-link.png |
| 74 | Type youtube, Tab, cats, Enter: YouTube's own search for cats opens | pass | 72-site-search-chip.png · 73-site-search-result.png |
| 75 | A style kept for localhost (Settings › Sites) is on its pages: the background reads rgb(1, 2, 3) | pass | 74-site-style.png |
| 76 | Ctrl+Alt+Shift+R on an article: reading mode opens with its three paragraphs, the links around it left out | pass | paragraphs: 3 · 75-reading-mode.png |
| 77 | Ctrl+Shift+P on a page with a playing video: it floats in a picture-in-picture window | pass | [03:44:55.859] pip {"result":{"type":"string","value":"opened"}} · 76-picture-in-picture.png |
| 78 | A live folder (pin folder with a feed address): its latest posts fill it a few seconds after launch | pass | feed: http://localhost:18956/feed.xml · 77-live-folder.png |
| 79 | Click a post: its page opens in a new tab | pass | session has /post1: True · 78-live-folder-post.png |
| 80 | Heading menu › New Live Folder Inside…, a site's address (not the feed's): the feed it points to is found and a folder named after it is made | pass | address box: True, folder made: True · 79-live-folder-new.png |
| 81 | A sign-in sent and settled: a card offers to keep the password; Save puts it in the vault, protected, never in the file as written | pass | card: True, kept: True, file shows the password: False, trace: form form.focus sign-in localhost / form form.focus sign-in localhost / form form.submit  localhost / form form.submit  localhost · 80-password-save-card.png |
| 82 | Back on the sign-in: the caret in the box offers the kept account; Fill puts the name and password in through the field's own setter | pass | card: True, page title shows both filled: True · 81-password-fill-card.png |
| 83 | Settings › Passwords: the account is listed; Show (Windows Hello, or straight through where none is set up) reveals the password | pass | listed: True, shown: True · 82-settings-passwords.png |
| 84 | Settings › Passwords › Passkeys reports whether this PC can hold passkeys (the web engine's WebAuthn answers; the signing itself needs a person at Windows Hello) | pass | status: Windows Hello is not set up on this PC; a security key still works. Passkeys are offered in sign-in boxes |
| 85 | Forget removes it from the vault | pass | vault.json: {   "items": [],   "never": [] } |
| 86 | A sign-up's new-password box: a strong password is offered and fills both boxes | pass | card: True, both boxes hold the same 20 characters: True, trace: form form.focus sign-in localhost / password fill done / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus new localhost · 83-password-strong.png |
| 87 | A contact form's email box offers your card (Settings › Passwords › My card); Fill puts name, email and postal code in the empty boxes and leaves the one already typed alone | pass | card: True, boxes: True · 84-card-offer.png · 85-card-filled.png |
| 88 | Settings › Import lists Chrome; Import brings over its bookmarks (a folder keeps its name), history and saved passwords; the vault holds the password protected, not as written | pass | sources listed: True, imported: True, folder kept: True, file shows the password: False · 86-import-sources.png · 87-import-chrome.png |
| 89 | The same for Zen (a Firefox-family profile in the roaming folder): its bookmarks come over | pass | bookmark kept: True · 88-import-zen.png |
| 90 | Zen's sidebar comes too: its space is a space here, and the page pinned inside its folder is pinned in that space, in a folder of the same name | pass | space: True, pin in folder: True · 89-import-zen-spaces.png |
| 91 | Sync through a folder: at launch another PC's bookmark, space, pin and setting are here, and this PC's own file is written beside it | pass | bookmark/space/setting: True, pin in the space's session: True, own files written: True · 90-sync-applied.png |
| 92 | Settings › General says who it synced with | pass | status shown: True · 91-sync-settings.png |
| 93 | Settings › Keyboard & Mouse lists the commands with their keys | pass | New Tab listedKey: True · 92-shortcuts-page.png |
| 94 | Click a key: the box waits for the new one | pass | 93-shortcuts-waiting.png |
| 95 | Press Ctrl+Alt+K: New Tab is kept on that key, and the key is not taken as a command meanwhile | pass | shortcuts.json: {"file.newTab":"Ctrl\u002BAlt\u002BK"} · 94-shortcuts-changed.png |
| 96 | Ctrl+T no longer opens a tab; Ctrl+Alt+K does | pass | on Settings before: True, after Ctrl+T: True, after Ctrl+Alt+K: False · 95-shortcuts-works.png |
| 97 | Put every key back: the keys are as they came | pass | shortcuts.json: {} |
| 98 | Hover the tab: a copy-link button sits at the far right of the row, no second address bar above the pages | pass | copy button on row: True · 96-column-address.png |
| 99 | Clicking the tab you are on changes its address right there, no new tab; Copy link copied it | pass | tab titled Sign up: True, copied: True · 97-column-address-edit.png |
| 100 | The tab's right-click menu has Bookmark, Screenshot, Share and Style This Site; Bookmark saves the page | pass | items: True, bookmarked: True · 98-column-actions.png |
| 101 | Take Screenshot from the tab menu: the page is saved to Pictures › Screenshots and a card says so | pass | card: True, file: True · 99-column-screenshot.png |
| 102 | Dragging the column's edge as narrow as it goes: the reload button stays whole with a little space before the next one | pass | column's right button moved from 222 to 214, gap after reload: 4px · 100-column-narrowest.png |
| 103 | The foot has Settings at the right, and Update beside it when one is ready | pass | update at 160, settings at 188 · 101-column-foot.png |
| 104 | Hovering the update button: a small window shows the version and what is different | pass | popup shown: True · 102-update-popup.png |
| 105 | Clicking it downloads the update with a bar filling up | pass | downloading text shown: True · 103-update-downloading.png |
| 106 | When the download is done it says it is ready; a second click would restart | pass | ready text shown: True · 104-update-ready.png |
| 107 | Settings at the foot opens Settings | pass | Settings page shown: True |
| 108 | The Library door in the foot widens the column: a rail of six sections down its left, Spaces first with the space as a card | pass | rail buttons: 6, space card: True, invoked at {X=10,Y=729,Width=26,Height=26}; trace: [03:47:41.523] library spaces · 105-library-spaces.png |
| 109 | History in the rail: the history list shows inside the column, no tab opened | pass | search box shown: True · 106-library-history.png |
| 110 | Downloads in the rail: the downloads list shows inside the column | pass | search box shown: True · 107-library-downloads.png |
| 111 | Bookmarks in the rail: the saved page is listed inside the column | pass | search box shown: True · 108-library-bookmarks.png |
| 112 | The back arrow closes the Library: the column narrows to what it was and the tabs are back | pass | closed: True, reload at 158 then 158 · 109-library-closed.png |
| 113 | Ctrl+Shift+L opens the Library at the section it was left on, and again closes it | pass | opened: True, closed: True |
| 114 | The hide button takes the column away; the pointer at the window's edge brings it back and it leaves again with the pointer | pass | hidden: True, shown from the edge: True, left again: True · 110-column-hidden.png · 111-column-edge.png |
| 115 | Pressing the button while the column is out from the edge keeps it open | pass | still there after the pointer left: True · 112-column-kept.png |
| 116 | An extension kept in Search's folder is loaded: its content script has marked the page's title | pass | tab shows [ext]: True · 113-extension-content-script.png |
| 117 | Ctrl+Shift+E opens the Library at Extensions: the extension is listed with its switch | pass | library at Extensions: True, listed: True · 114-extension-hub.png |
| 118 | Open: the extension's popup appears in a small window | pass | popup text shown: True · 115-extension-popup.png |
| 119 | An address to a .crx in the box, Add: the package is downloaded, unpacked and listed | pass | listed: True · 116-extension-added.png |
| 120 | A Chrome Web Store link in the box, Add: the real store's package is downloaded, unpacked and listed | pass | listed · 117-extension-store.png |
| 121 | The switch turns an extension off | pass | kept as off: True · 118-extension-off.png |
| 122 | Remove takes the extension and its folder away | pass | gone: True; folders left: seeded0001 · 119-extension-removed.png |
| 123 | Settings › Privacy › Proxy set to a server of its own: a page for a name that exists nowhere loads through it | pass | the proxy saw: GET http://proxy-probe.test/ HTTP/1.1 · 120-proxy.png |
| 124 | Settings › Privacy › Proxy › User name and Password typed: the name is vaulted in prefs, the password protected in its own file and nowhere else | pass | settings has the fields: True, protected file: True · 121-proxy-credentials.png |
| 125 | A proxy that answers 407: Search signs in with the kept name and password, and the page loads | pass | the proxy saw: asked who / signed in / signed in · 122-proxy-signed-in.png |
| 126 | A site that asks for a certificate: a card lists yours, Continue sends it, and the site sees it | pass | card: True, server saw the certificate: True, page shows it: True · 123-client-certificate-card.png · 124-client-certificate-sent.png |
| 127 | Crash log is empty | pass |  |
