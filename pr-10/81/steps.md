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
| 24 | Alt+F4: the window closes and the session is saved | pass | {"tabs":[{"url":"https://www.google.com/search?q=weather%20paris\u0026sei=8oYHa5SZNbi50PEPtPSIuQk","title":"weather paris - Google Search","pinId":"fd0fcad5-9a0 |
| 25 | Open Search again: pinned page and tabs come back, pin keeps its icon | pass | pin saved: True, icons cached: True · 23-restored.png |
| 26 | Spaces on: a strip of space icons shows above the footer, Personal selected | pass | 24-spaces-strip.png |
| 27 | Ctrl+Alt+2: the Work space opens with its own empty row; a page opened there loads | pass | 25-space-work.png |
| 28 | Ctrl+Alt+1: back to Personal with its own tabs (the search, the pin) while Work's file holds only its own page | pass | 26-space-personal.png |
| 29 | New Space popup: name it Travel, Enter: the space is made and opened | pass | 27-space-new.png |
| 30 | Layout Top, pins as rows: tabs run along the top strip with the space icons, the column is gone | pass | 28-layout-top.png |
| 31 | The profile door lists Personal and Studio | pass | 29-profile-menu.png |
| 32 | Choose Studio: it opens in a window of its own with its own spaces | pass | 2 windows · 30-profile-window.png |
| 33 | A tab left alone for years, with Close tabs left alone set to a day: it is closed into the archive, the other stays | pass | archive.json holds: example.net · 31-archived-on-start.png |
| 34 | search://archive: the Archived Tabs page lists it, with Open and Remove | pass | 32-archive-page.png |
| 35 | Ctrl+Alt+N on a lone page: an empty page opens beside it, the address field ready; example.com loads in it | pass | splits: [2] · 33-split-two.png |
| 36 | Ctrl+Alt+Left: the left page takes the focus ring | pass | 34-split-focus-left.png |
| 37 | Drag the line between the pages: the left one gets narrower, the right one wider | pass | before 0.50/0.50, after 0.24/0.76 · 35-split-resized.png |
| 38 | Alt+click two more tabs: they join the split; four pages share the room | pass | splits: [4] · 36-split-four.png |
| 39 | Ctrl+Alt+N inside a split: the pages come apart and stay as tabs | pass | 37-split-separated.png |
| 40 | Shift+click a link: its page opens over this one in a panel, the page behind dimmed | pass | link found: True, panel with Open as tab: True, browser started:  · 38-peek.png |
| 41 | Esc: the peek goes away | pass | 39-peek-closed.png |
| 42 | Alt+click a link (Settings › Alt+click opens in Split View): the link loads beside the page | pass | splits: [2] · 40-split-link.png |
| 43 | A link from another app, with Little windows on: it opens in a small window, not a tab | pass | small window with Open in Search: True · 41-little-window.png |
| 44 | Snap layouts: asked what is under the pointer over the maximise button, the window says HTMAXBUTTON (9) | pass | over the button: 9, elsewhere: 1 |
| 45 | Quit with two windows (Ctrl+Shift+Q), open Search again: both windows come back, each with its own tabs | pass | windows.json lists one: True, windows open: 2, window session files: 1 · 42-windows-restored.png |
| 46 | Tab menu › Add to Group › New Group, type Research: a heading appears above the tab, the group is saved | pass | groups: [{"id":"736e2812-5e10-4fc1-99a0-f11e128fd39b","name":"Research","collapsed":false}] · 43-group-new.png |
| 47 | Add the third tab to Research from its menu: two tabs sit under the heading | pass | members: 2 · 44-group-two.png |
| 48 | Click the heading: the group folds to its name and a count | pass | collapsed saved: True, tab rows left: 3 · 45-group-folded.png |
| 49 | Drag a loose tab onto the Research heading: it joins the group | pass | members: 3 · from {X=110,Y=72} to {X=123,Y=69} trace: drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drag hits 153 TextBoxView:H@137 Grid:H@137 ScrollViewer:H@137 Border:H@137 TextBlock:H@141 Border:H@137 / drag over heading / drop on heading / drag ended Move · 46-group-dragging.png · 47-group-drop.png |
| 50 | Pin a tab, its menu › Move to Folder › New Folder, type Work: a folder heading holds the pin | pass | folders: [{"id":"9250f555-efa3-454c-91d2-8040efbbac37","name":"Work","collapsed":false}] · 48-folder-new.png |
| 51 | Right-click the folder › New Folder Inside, type Deep: a folder inside a folder, set further in | pass | folders: [{"id":"9250f555-efa3-454c-91d2-8040efbbac37","name":"Work","collapsed":false},{"id":"c2708a2c-a3d1-491f-96d1-400f16a1f7d6","name":"Deep","parent":"9250f555-efa3-454c-91d2-8040efbbac37","collapsed":false}] · 49-folder-nested.png |
| 52 | Quit and open again: the group, the folder and the folder inside it are still there | pass | headings found: Research True, Work True, Deep True · 50-groups-restored.png |
| 53 | Ctrl+Shift+S: the column folds away (a still shot straight after, then settled) | pass | column gone: True · 51-column-folding.png · 52-column-folded.png |
| 54 | Ctrl+Shift+S again: the column comes back | pass | 53-column-back.png |
| 55 | Ctrl+Alt+2: the rows of the next space slide in from the side (a still straight after, then settled) | pass | 54-space-swiping.png · 55-space-swiped.png |
| 56 | Ctrl+2: the page dips as the next tab comes up, its row takes the wash (a still straight after, then settled) | pass | 56-tab-switching.png · 57-tab-switched.png |
| 57 | A page asks for your location: a card over the page asks, Allow is kept for that site | pass | card shown: True, kept: {"sites":{"http://localhost:18956":{"Location":0}}} · 58-permission-card.png |
| 58 | Asked again: no card, the kept answer is used | pass | 59-permission-remembered.png |
| 59 | A page asks to show notifications, then sends one: the card names the site and the message | pass | asked: True, shown: True · 60-notification.png |
| 60 | Block trackers on: a page asking for a doubleclick.net address is never sent it | pass | trace: tracker blocked https://doubleclick.net/x |
| 61 | Tab menu › Site Information: the site, how it is connected and what it may use | pass | 61-site-information.png |
| 62 | Settings › Sites lists what the site was allowed | pass | 62-settings-sites.png |
| 63 | Forget takes the answer back | pass | permissions.json: {"sites":{}} |
| 64 | Ctrl+Alt+Shift+C on a page with utm_ and fbclid in its address, pasted into a new tab: only id=7 is left | pass | session: http://localhost:18956/?id=7 · 63-clean-link.png |
| 65 | Type youtube, Tab, cats, Enter: YouTube's own search for cats opens | pass | 64-site-search-chip.png · 65-site-search-result.png |
| 66 | A style kept for localhost (Settings › Sites) is on its pages: the background reads rgb(1, 2, 3) | pass | 66-site-style.png |
| 67 | Ctrl+Alt+Shift+R on an article: reading mode opens with its three paragraphs, the links around it left out | pass | paragraphs: 3 · 67-reading-mode.png |
| 68 | Ctrl+Shift+P on a page with a playing video: it floats in a picture-in-picture window | pass | [01:17:19.939] pip {"result":{"type":"string","value":"opened"}} · 68-picture-in-picture.png |
| 69 | A live folder (pin folder with a feed address): its latest posts fill it a few seconds after launch | pass | feed: http://localhost:18956/feed.xml · 69-live-folder.png |
| 70 | Click a post: its page opens in a new tab | pass | session has /post1: True · 70-live-folder-post.png |
| 71 | Heading menu › New Live Folder Inside…, a site's address (not the feed's): the feed it points to is found and a folder named after it is made | pass | address box: True, folder made: True · 71-live-folder-new.png |
| 72 | A sign-in sent and settled: a card offers to keep the password; Save puts it in the vault, protected, never in the file as written | pass | card: True, kept: True, file shows the password: False, trace: form form.focus sign-in localhost / form form.focus sign-in localhost / form form.submit  localhost / form form.submit  localhost · 72-password-save-card.png |
| 73 | Back on the sign-in: the caret in the box offers the kept account; Fill puts the name and password in through the field's own setter | pass | card: True, page title shows both filled: True · 73-password-fill-card.png |
| 74 | Settings › Passwords: the account is listed; Show (Windows Hello, or straight through where none is set up) reveals the password | pass | listed: True, shown: True · 74-settings-passwords.png |
| 75 | Settings › Passwords › Passkeys reports whether this PC can hold passkeys (the web engine's WebAuthn answers; the signing itself needs a person at Windows Hello) | pass | status: Windows Hello is not set up on this PC; a security key still works. Passkeys are offered in sign-in boxes |
| 76 | Forget removes it from the vault | pass | vault.json: {   "items": [],   "never": [] } |
| 77 | A sign-up's new-password box: a strong password is offered and fills both boxes | pass | card: True, both boxes hold the same 20 characters: True, trace: form form.focus sign-in localhost / password fill done / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus sign-in localhost / form form.focus new localhost · 75-password-strong.png |
| 78 | A contact form's email box offers your card (Settings › Passwords › My card); Fill puts name, email and postal code in the empty boxes and leaves the one already typed alone | pass | card: True, boxes: True · 76-card-offer.png · 77-card-filled.png |
| 79 | Settings › Import lists Chrome; Import brings over its bookmarks (a folder keeps its name), history and saved passwords; the vault holds the password protected, not as written | pass | sources listed: True, imported: True, folder kept: True, file shows the password: False · 78-import-sources.png · 79-import-chrome.png |
| 80 | The same for Zen (a Firefox-family profile in the roaming folder): its bookmarks come over | pass | bookmark kept: True · 80-import-zen.png |
| 81 | Zen's sidebar comes too: its space is a space here, and the page pinned inside its folder is pinned in that space, in a folder of the same name | pass | space: True, pin in folder: True · 81-import-zen-spaces.png |
| 82 | Sync through a folder: at launch another PC's bookmark, space, pin and setting are here, and this PC's own file is written beside it | pass | bookmark/space/setting: True, pin in the space's session: True, own files written: True · 82-sync-applied.png |
| 83 | Settings › General says who it synced with | pass | status shown: True · 83-sync-settings.png |
| 84 | Hover the tab: a copy-link button sits at the far right of the row, no second address bar above the pages | pass | copy button on row: True · 84-column-address.png |
| 85 | Clicking the tab you are on changes its address right there, no new tab; Copy link copied it | pass | tab titled Sign up: True, copied: True · 85-column-address-edit.png |
| 86 | The tab's right-click menu has Bookmark, Screenshot, Share and Style This Site; Bookmark saves the page | pass | items: True, bookmarked: True · 86-column-actions.png |
| 87 | Take Screenshot from the tab menu: the page is saved to Pictures › Screenshots and a card says so | pass | card: True, file: True · 87-column-screenshot.png |
| 88 | Dragging the column's edge as narrow as it goes: the reload button stays whole with a little space before the next one | pass | column's right button moved from 222 to 214, gap after reload: 4px · 88-column-narrowest.png |
| 89 | The foot has Settings at the right, and Update beside it when one is ready | pass | update at 160, settings at 188 · 89-column-foot.png |
| 90 | Settings at the foot opens Settings | pass | Settings page shown: True |
| 91 | The Library door in the foot widens the column: a rail of six sections down its left, Spaces first with the space as a card | pass | rail buttons: 6, space card: True, invoked at {X=66,Y=732,Width=26,Height=26}; trace: [01:19:36.274] library spaces · 90-library-spaces.png |
| 92 | History in the rail: the history list shows inside the column, no tab opened | pass | search box shown: True · 91-library-history.png |
| 93 | Downloads in the rail: the downloads list shows inside the column | pass | search box shown: True · 92-library-downloads.png |
| 94 | Bookmarks in the rail: the saved page is listed inside the column | pass | search box shown: True · 93-library-bookmarks.png |
| 95 | The back arrow closes the Library: the column narrows to what it was and the tabs are back | pass | closed: True, reload at 158 then 158 · 94-library-closed.png |
| 96 | Ctrl+Shift+L opens the Library at the section it was left on, and again closes it | pass | opened: True, closed: True |
| 97 | The hide button takes the column away; the pointer at the window's edge brings it back and it leaves again with the pointer | pass | hidden: True, shown from the edge: True, left again: True · 95-column-hidden.png · 96-column-edge.png |
| 98 | Pressing the button while the column is out from the edge keeps it open | pass | still there after the pointer left: True · 97-column-kept.png |
| 99 | Settings › Privacy › Proxy set to a server of its own: a page for a name that exists nowhere loads through it | pass | the proxy saw: GET http://proxy-probe.test/ HTTP/1.1 / GET http://proxy-probe.test/favicon.ico HTTP/1.1 · 98-proxy.png |
| 100 | Settings › Privacy › Proxy › User name and Password typed: the name is vaulted in prefs, the password protected in its own file and nowhere else | pass | settings has the fields: True, protected file: True · 99-proxy-credentials.png |
| 101 | A proxy that answers 407: Search signs in with the kept name and password, and the page loads | pass | the proxy saw: asked who / signed in · 100-proxy-signed-in.png |
| 102 | A site that asks for a certificate: a card lists yours, Continue sends it, and the site sees it | pass | card: True, server saw the certificate: True, page shows it: True · 101-client-certificate-card.png · 102-client-certificate-sent.png |
| 103 | Crash log is empty | pass |  |
