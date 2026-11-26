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
| 24 | Alt+F4: the window closes and the session is saved | pass | {"tabs":[{"url":"https://www.google.com/search?q=weather%20paris\u0026sei=gDcIa5uzMIGcptQP-6ms6Ac","title":"weather paris - Google Search","pinId":"d0f48877-20a |
| 25 | Open Search again: pinned page and tabs come back, pin keeps its icon | pass | pin saved: True, icons cached: True · 23-restored.png |
| 26 | Spaces on: a strip of space icons shows above the footer, Personal selected | pass | 24-spaces-strip.png |
| 27 | Ctrl+Alt+2: the Work space opens with its own empty row; a page opened there loads | pass | 25-space-work.png |
| 28 | Ctrl+Alt+1: back to Personal with its own tabs (the search, the pin) while Work's file holds only its own page | pass | 26-space-personal.png |
| 29 | New Space popup: name it Travel, Enter: the space is made and opened | pass | 27-space-new.png |
| 30 | Layout Top, pins as rows: tabs run along the top strip with the space icons, the column is gone | pass | 28-layout-top.png |
| 31 | The top strip has Windows' own window buttons at the top right: minimise, maximise, close, left to right | pass | min 886, max 932, close 978..1024 of a window ending at 1031 · 29-strip-caption-buttons.png |
| 32 | The top strip has Settings and the Library, and no Home or Downloads | **FAIL** | TopSettings True, TopLibraryDoor True |
| 33 | With tabs past the strip's width, the plus stays at the strip's right edge, no scrolling to reach it | pass | plus at 756..786, window right 1031 · 30-strip-many-tabs.png |
| 34 | Pressing the pinned plus opens one more tab | pass | 31-strip-plus-pressed.png |
| 35 | The strip's Settings button opens Settings, where Spaces & Profiles has a section of its own | **FAIL** | 32-settings-spaces-section.png |
| 36 | The profile door lists Personal and Studio | pass | 33-profile-menu.png |
| 37 | Choose Studio: it opens in a window of its own with its own spaces | pass | 2 windows · 34-profile-window.png |
| 38 | A tab left alone for years, with Close tabs left alone set to a day: it is closed into the archive, the other stays | pass | archive.json holds: example.net · 35-archived-on-start.png |
| 39 | search://archive: the Archived Tabs page lists it, with Open and Remove | pass | 36-archive-page.png |
| 40 | Ctrl+Alt+N on a lone page: an empty page opens beside it, the address field ready; example.com loads in it | pass | splits: [2] · 37-split-two.png |
| 41 | Ctrl+Alt+Left: the left page takes the focus ring | pass | 38-split-focus-left.png |
| 42 | Drag the line between the pages: the left one gets narrower, the right one wider | pass | before 0.50/0.50, after 0.24/0.76 · 39-split-resized.png |
| 43 | Alt+click two more tabs: they join the split; four pages share the room | pass | splits: [4] · 40-split-four.png |
| 44 | Ctrl+Alt+N inside a split: the pages come apart and stay as tabs | pass | 41-split-separated.png |
| 45 | Shift+click a link: its page opens over this one in a panel, the page behind dimmed | pass | link found: True, panel with Open as tab: True, browser started:  · 42-peek.png |
| 46 | Esc: the peek goes away | pass | 43-peek-closed.png |
| 47 | Alt+click a link (Settings › Alt+click opens in Split View): the link loads beside the page | pass | splits: [2] · 44-split-link.png |
| 48 | A link from another app, with Little windows on: it opens in a small window, not a tab | pass | small window with Open in Search: True · 45-little-window.png |
| 49 | Snap layouts: asked what is under the pointer over the maximise button, the window says HTMAXBUTTON (9) | pass | over the button: 9, elsewhere: 1 |
| 50 | Quit with two windows (Ctrl+Shift+Q), open Search again: both windows come back, each with its own tabs | pass | windows.json lists one: True, windows open: 2, window session files: 1 · 46-windows-restored.png |
| 51 | Tab menu › Add to Group › New Group, type Research: a heading appears above the tab, the group is saved | pass | groups: [{"id":"2851b592-28fd-44aa-b5fa-7cdbb4cb93fd","name":"Research","collapsed":false}] · 47-group-new.png |
| 52 | Add the third tab to Research from its menu: two tabs sit under the heading | pass | members: 2 · 48-group-two.png |
| 53 | Click the heading: the group folds to its name and a count | pass | collapsed saved: True, tab rows left: 3 · 49-group-folded.png |
| 54 | Drag a loose tab onto the Research heading: it joins the group | pass | members: 3 · from {X=110,Y=72} to {X=123,Y=69} trace: drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drop on heading / drag ended Move · 50-group-dragging.png · 51-group-drop.png |
| 55 | Pin a tab, its menu › Move to Folder › New Folder, type Work: a folder heading holds the pin | pass | folders: [{"id":"1e82e95c-649b-4444-8ec1-a8711aab903d","name":"Work","collapsed":false}] · 52-folder-new.png |
| 56 | Right-click the folder › New Folder Inside, type Deep: a folder inside a folder, set further in | pass | folders: [{"id":"1e82e95c-649b-4444-8ec1-a8711aab903d","name":"Work","collapsed":false},{"id":"34e6b2f5-a0ef-4665-ac4b-85eb3fa551dd","name":"Deep","parent":"1e82e95c-649b-4444-8ec1-a8711aab903d","collapsed":false}] · 53-folder-nested.png |
| 57 | Quit and open again: the group, the folder and the folder inside it are still there | pass | headings found: Research True, Work True, Deep True · 54-groups-restored.png |
| 58 | Ctrl+Shift+S: the column folds away (a still shot straight after, then settled) | pass | column gone: True · 55-column-folding.png · 56-column-folded.png |
| 59 | Ctrl+Shift+S again: the column comes back | pass | 57-column-back.png |
| 60 | Ctrl+Alt+2: the rows of the next space slide in from the side (a still straight after, then settled) | pass | 58-space-swiping.png · 59-space-swiped.png |
| 61 | Ctrl+2: the page dips as the next tab comes up, its row takes the wash (a still straight after, then settled) | pass | 60-tab-switching.png · 61-tab-switched.png |
| 62 | A page asks for your location: a card over the page asks, Allow is kept for that site | pass | card shown: True, kept: {"sites":{"http://localhost:18957":{"Location":0}}} · 62-permission-card.png |
| 63 | Asked again: no card, the kept answer is used | pass | 63-permission-remembered.png |
| 64 | A page asks to show notifications, then sends one: the card names the site and the message | pass | asked: True, shown: True · 64-notification.png |
| 65 | Block trackers on: a page asking for a doubleclick.net address is never sent it | pass | trace: tracker blocked https://doubleclick.net/x |
| 66 | Tab menu › Site Information: the site, how it is connected and what it may use | pass | 65-site-information.png |
| 67 | Settings › Sites lists what the site was allowed | **FAIL** | 66-settings-sites.png |
| 68 | Forget takes the answer back | **FAIL** | permissions.json: {"sites":{"http://localhost:18957":{"Location":0,"Notifications":0}}} |
| 69 | Ctrl+Alt+Shift+C on a page with utm_ and fbclid in its address, pasted into a new tab: only id=7 is left | pass | session: http://localhost:18957/?id=7 · 67-clean-link.png |
| 70 | Type youtube, Tab, cats, Enter: YouTube's own search for cats opens | pass | 68-site-search-chip.png · 69-site-search-result.png |
| 71 | A style kept for localhost (Settings › Sites) is on its pages: the background reads rgb(1, 2, 3) | pass | 70-site-style.png |
| 72 | Ctrl+Alt+Shift+R on an article: reading mode opens with its three paragraphs, the links around it left out | pass | paragraphs: 3 · 71-reading-mode.png |
| 73 | Ctrl+Shift+P on a page with a playing video: it floats in a picture-in-picture window | pass | [13:51:23.032] pip {"result":{"type":"string","value":"opened"}} · 72-picture-in-picture.png |
| 74 | A live folder (pin folder with a feed address): its latest posts fill it a few seconds after launch | pass | feed: http://localhost:18957/feed.xml · 73-live-folder.png |
| 75 | Click a post: its page opens in a new tab | pass | session has /post1: True · 74-live-folder-post.png |
| 76 | Heading menu › New Live Folder Inside…, a site's address (not the feed's): the feed it points to is found and a folder named after it is made | pass | address box: True, folder made: True · 75-live-folder-new.png |
| 77 | A sign-in sent and settled: a card offers to keep the password; Save puts it in the vault, protected, never in the file as written | pass | card: True, kept: True, file shows the password: False, trace: form form.focus sign-in localhost / form form.focus sign-in localhost / form form.submit  localhost / form form.submit  localhost · 76-password-save-card.png |
| 78 | Back on the sign-in: the caret in the box offers the kept account; Fill puts the name and password in through the field's own setter | pass | card: True, page title shows both filled: True · 77-password-fill-card.png |
| 79 | Settings › Passwords: the account is listed; Show (Windows Hello, or straight through where none is set up) reveals the password | **FAIL** | listed: True, shown: False · 78-settings-passwords.png |
| 80 | Settings › Passwords › Passkeys reports whether this PC can hold passkeys (the web engine's WebAuthn answers; the signing itself needs a person at Windows Hello) | **FAIL** | status:  |
| 81 | Forget removes it from the vault | **FAIL** | vault.json: {   "items": [     {       "host": "localhost",       "user": "ana@example.com",       "secret": "AQAAANCMnd8BFdERj |
| 82 | A sign-up's new-password box: a strong password is offered and fills both boxes | pass | card: True, both boxes hold the same 20 characters: True, trace: form form.focus sign-in localhost / password fill done / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus new localhost · 79-password-strong.png |
| 83 | A contact form's email box offers your card (Settings › Passwords › My card); Fill puts name, email and postal code in the empty boxes and leaves the one already typed alone | pass | card: True, boxes: True · 80-card-offer.png · 81-card-filled.png |
| 84 | Settings › Import lists Chrome; Import brings over its bookmarks (a folder keeps its name), history and saved passwords; the vault holds the password protected, not as written | **FAIL** | sources listed: False, imported: False, folder kept: False, file shows the password: False · 82-import-sources.png · 83-import-chrome.png |
| 85 | The same for Zen (a Firefox-family profile in the roaming folder): its bookmarks come over | **FAIL** | bookmark kept: False · 84-import-zen.png |
| 86 | Zen's sidebar comes too: its space is a space here, and the page pinned inside its folder is pinned in that space, in a folder of the same name | **FAIL** | space: False, pin in folder: False · 85-import-zen-spaces.png |
| 87 | Sync through a folder: at launch another PC's bookmark, space, pin and setting are here, and this PC's own file is written beside it | pass | bookmark/space/setting: True, pin in the space's session: True, own files written: True · 86-sync-applied.png |
| 88 | Settings › General says who it synced with | pass | status shown: True · 87-sync-settings.png |
| 89 | Hover the tab: a copy-link button sits at the far right of the row, no second address bar above the pages | pass | copy button on row: True · 88-column-address.png |
| 90 | Clicking the tab you are on changes its address right there, no new tab; Copy link copied it | pass | tab titled Sign up: True, copied: True · 89-column-address-edit.png |
| 91 | The tab's right-click menu has Bookmark, Screenshot, Share and Style This Site; Bookmark saves the page | pass | items: True, bookmarked: True · 90-column-actions.png |
| 92 | Take Screenshot from the tab menu: the page is saved to Pictures › Screenshots and a card says so | pass | card: True, file: True · 91-column-screenshot.png |
| 93 | Dragging the column's edge as narrow as it goes: the reload button stays whole with a little space before the next one | pass | column's right button moved from 222 to 214, gap after reload: 4px · 92-column-narrowest.png |
| 94 | The foot has Settings at the right, and Update beside it when one is ready | pass | update at 160, settings at 188 · 93-column-foot.png |
| 95 | Settings at the foot opens Settings | pass | Settings page shown: True |
| 96 | The Library door in the foot widens the column: a rail of six sections down its left, Spaces first with the space as a card | pass | rail buttons: 6, space card: True, invoked at {X=10,Y=732,Width=26,Height=26}; trace: [13:54:41.856] library spaces · 94-library-spaces.png |
| 97 | History in the rail: the history list shows inside the column, no tab opened | pass | search box shown: True · 95-library-history.png |
| 98 | Downloads in the rail: the downloads list shows inside the column | pass | search box shown: True · 96-library-downloads.png |
| 99 | Bookmarks in the rail: the saved page is listed inside the column | pass | search box shown: True · 97-library-bookmarks.png |
| 100 | The back arrow closes the Library: the column narrows to what it was and the tabs are back | pass | closed: True, reload at 158 then 158 · 98-library-closed.png |
| 101 | Ctrl+Shift+L opens the Library at the section it was left on, and again closes it | pass | opened: True, closed: True |
| 102 | The hide button takes the column away; the pointer at the window's edge brings it back and it leaves again with the pointer | pass | hidden: True, shown from the edge: True, left again: True · 99-column-hidden.png · 100-column-edge.png |
| 103 | Pressing the button while the column is out from the edge keeps it open | pass | still there after the pointer left: True · 101-column-kept.png |
| 104 | An extension kept in Search's folder is loaded: its content script has marked the page's title | pass | tab shows [ext]: True · 102-extension-content-script.png |
| 105 | Ctrl+Shift+E opens the Library at Extensions: the extension is listed with its switch | pass | library at Extensions: True, listed: True · 103-extension-hub.png |
| 106 | Open: the extension's popup appears in a small window | pass | popup text shown: True · 104-extension-popup.png |
| 107 | An address to a .crx in the box, Add: the package is downloaded, unpacked and listed | pass | listed: True · 105-extension-added.png |
| 108 | A Chrome Web Store link in the box, Add: the real store's package is downloaded, unpacked and listed | pass | listed · 106-extension-store.png |
| 109 | The switch turns an extension off | pass | kept as off: True · 107-extension-off.png |
| 110 | Remove takes the extension and its folder away | **FAIL** | gone: False · 108-extension-removed.png |
| 111 | Settings › Privacy › Proxy set to a server of its own: a page for a name that exists nowhere loads through it | pass | the proxy saw: GET http://proxy-probe.test/ HTTP/1.1 / GET http://proxy-probe.test/favicon.ico HTTP/1.1 · 109-proxy.png |
| 112 | Settings › Privacy › Proxy › User name and Password typed: the name is vaulted in prefs, the password protected in its own file and nowhere else | **FAIL** | settings has the fields: False, protected file: False · 110-proxy-credentials.png |
| 113 | A proxy that answers 407: Search signs in with the kept name and password, and the page loads | **FAIL** | the proxy saw: asked who · 111-proxy-signed-in.png |
| 114 | A site that asks for a certificate: a card lists yours, Continue sends it, and the site sees it | pass | card: True, server saw the certificate: True, page shows it: True · 112-client-certificate-card.png · 113-client-certificate-sent.png |
| 115 | Crash log is empty | pass |  |
