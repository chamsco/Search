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
| 24 | Alt+F4: the window closes and the session is saved | pass | {"tabs":[{"url":"https://www.google.com/search?q=weather%20paris\u0026sei=_fQFa6aCAs6n0PEP67TmqQE","title":"weather paris - Google Search","pinId":"df3aeebc-e00 |
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
| 46 | Tab menu › Add to Group › New Group, type Research: a heading appears above the tab, the group is saved | pass | groups: [{"id":"650cac37-fd9b-4c22-9c86-e7ddc7a91ef5","name":"Research","collapsed":false}] · 43-group-new.png |
| 47 | Add the third tab to Research from its menu: two tabs sit under the heading | pass | members: 2 · 44-group-two.png |
| 48 | Click the heading: the group folds to its name and a count | pass | collapsed saved: True, tab rows left: 3 · 45-group-folded.png |
| 49 | Drag a loose tab onto the Research heading: it joins the group | **FAIL** | members: 2 · from {X=110,Y=72} to {X=123,Y=99} trace: row press 1 at 117,79 / row moved 2,4 / drag start Example Domain / drag over row at=17,65 h=28 local=103,20 ctx=Tab idx=0 drag=0 grp= / drag over row at=17,107 h=28 local=113,1 ctx=Tab idx=0 drag=0 grp= / drag over row at=17,107 h=28 local=114,2 ctx=Tab idx=0 drag=0 grp= / drag over row at=17,107 h=28 local=115,3 ctx=Tab idx=0 drag=0 grp= / drag over row at=17,107 h=28 local=116,4 ctx=Tab idx=0 drag=0 grp= / drag ended Move · 46-group-drop.png |
| 50 | Pin a tab, its menu › Move to Folder › New Folder, type Work: a folder heading holds the pin | pass | folders: [{"id":"a9cff233-9135-4542-b57c-95b249d819bf","name":"Work","collapsed":false}] · 47-folder-new.png |
| 51 | Right-click the folder › New Folder Inside, type Deep: a folder inside a folder, set further in | pass | folders: [{"id":"a9cff233-9135-4542-b57c-95b249d819bf","name":"Work","collapsed":false},{"id":"ca8d848a-b370-4137-9c56-d8930f8b6d42","name":"Deep","parent":"a9cff233-9135-4542-b57c-95b249d819bf","collapsed":false}] · 48-folder-nested.png |
| 52 | Quit and open again: the group, the folder and the folder inside it are still there | pass | headings found: Research True, Work True, Deep True · 49-groups-restored.png |
| 53 | Ctrl+Shift+S: the column folds away (a still shot straight after, then settled) | pass | column gone: True · 50-column-folding.png · 51-column-folded.png |
| 54 | Ctrl+Shift+S again: the column comes back | pass | 52-column-back.png |
| 55 | Ctrl+Alt+2: the rows of the next space slide in from the side (a still straight after, then settled) | pass | 53-space-swiping.png · 54-space-swiped.png |
| 56 | Ctrl+2: the page dips as the next tab comes up, its row takes the wash (a still straight after, then settled) | pass | 55-tab-switching.png · 56-tab-switched.png |
| 57 | Crash log is empty | pass |  |
