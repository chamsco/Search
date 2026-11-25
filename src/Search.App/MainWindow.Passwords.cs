using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Search.Core;

namespace Search.App;

// Passwords (Forms.swift, Passwords.swift): a sign-in on a page is noticed, a password sent is offered a place in the vault once the
// page has settled, and a kept account is offered where the caret goes into a sign-in box. Filling goes through the field's own setter and
// fires the events a keystroke would; assigning to .value behind a framework's back leaves it thinking the box is empty.
public partial class MainWindow
{
    sealed record PendingLogin(string Host, string User, string Password, bool Clear, DateTime At);
    readonly Dictionary<Guid, PendingLogin> pendingLogin = [];
    readonly Dictionary<Guid, string> formOffered = [];
    readonly Dictionary<Guid, (string Id, string Source)> formScripts = [];

    static readonly string FormScript = FormScriptTemplate.Replace("%KINDS%", JsonSerializer.Serialize(MeCard.Kinds));

    const string FormScriptTemplate = """
    (() => {
      if (window.__searchForms) return; window.__searchForms = true;
      const post = m => { try { chrome.webview.postMessage(m); } catch (e) {} };
      const textual = i => ['text', 'email', 'tel', 'search', ''].includes(i.type);
      const visible = el => { const r = el.getBoundingClientRect(), s = getComputedStyle(el); return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none'; };
      const scope = el => el.form || el.closest('form') || document;
      const pwIn = sc => [...sc.querySelectorAll('input[type=password]')].filter(visible);
      const userFor = pw => {
        let best = null;
        for (const b of scope(pw).querySelectorAll('input')) if (b !== pw && textual(b) && visible(b) && !b.disabled && (b.compareDocumentPosition(pw) & Node.DOCUMENT_POSITION_FOLLOWING)) best = b;
        return best;
      };
      const setValue = (el, v) => {
        const set = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(el), 'value').set;
        set.call(el, v);
        el.dispatchEvent(new Event('input', { bubbles: true })); el.dispatchEvent(new Event('change', { bubbles: true }));
      };
      window.__searchFill = (user, pw) => {
        const p = pwIn(document)[0]; if (!p) return false;
        const u = userFor(p); if (u && user) setValue(u, user);
        setValue(p, pw); return true;
      };
      const cardKinds = new Set(%KINDS%);
      let cardEl = null;
      const kindOf = el => (el.autocomplete || '').toLowerCase().split(/\s+/).filter(t => cardKinds.has(t)).pop();
      // The boxes of the form the caret is in that are still empty and visible take what the picked row holds.
      window.__searchFillCard = fields => {
        if (!cardEl || !document.contains(cardEl)) return 0;
        let n = 0;
        for (const b of scope(cardEl).querySelectorAll('input, textarea')) {
          if (b.disabled || b.readOnly || !visible(b) || b.value || (b.tagName === 'INPUT' && !textual(b))) continue;
          const k = kindOf(b); if (!k || fields[k] === undefined) continue;
          setValue(b, b.tagName === 'INPUT' ? fields[k].replace(/\n/g, ', ') : fields[k]); n++;
        }
        return n;
      };
      window.__searchFillNew = pw => { const ps = pwIn(document); ps.forEach(p => setValue(p, pw)); return ps.length; };
      addEventListener('focusin', e => {
        const el = e.target; if (!(el instanceof HTMLInputElement)) return;
        const ac = (el.autocomplete || '').toLowerCase();
        if (el.type === 'password') {
          const fresh = ac.includes('new-password') || (pwIn(scope(el)).length >= 2 && !ac.includes('current-password'));
          post({ type: 'form.focus', kind: fresh ? 'new' : 'sign-in' });
        } else if (textual(el) && !el.value && kindOf(el) && !ac.includes('username') && pwIn(scope(el)).length === 0) { cardEl = el; post({ type: 'form.card', kind: kindOf(el) }); }
        else if (textual(el) && (ac.includes('username') || pwIn(scope(el)).length > 0)) post({ type: 'form.focus', kind: 'sign-in' });
      }, true);
      let sent = 0;
      const sending = el => {
        const sc = el ? scope(el) : document;
        const pws = pwIn(sc).filter(p => p.value.length > 0);
        if (!pws.length || Date.now() - sent < 800) return;
        sent = Date.now();
        const pw = pws[0], u = userFor(pw);
        post({ type: 'form.submit', user: u ? u.value : '', password: pw.value, fresh: pws.length >= 2 });
        const url = location.href; let n = 0;
        const t = setInterval(() => {
          n++;
          if (!document.contains(pw) || !visible(pw) || location.href !== url) { clearInterval(t); post({ type: 'form.settled' }); }
          else if (n > 8) clearInterval(t);
        }, 500);
      };
      addEventListener('submit', e => sending(e.target), true);
      addEventListener('keydown', e => { if (e.key === 'Enter' && e.target instanceof HTMLInputElement && (e.target.type === 'password' || textual(e.target))) sending(e.target); }, true);
      addEventListener('click', e => { const b = e.target && e.target.closest ? e.target.closest('button, input[type=submit], input[type=button], [role=button]') : null; if (b) sending(b); }, true);
    })();
    """;

    async Task InstallFormScript(Tab t, CoreWebView2 core)
    {
        try
        {
            if (formScripts.ContainsKey(t.Id)) return;
            formScripts[t.Id] = (await core.AddScriptToExecuteOnDocumentCreatedAsync(FormScript), FormScript);
            core.NavigationCompleted += (_, _) => OnFormNavigated(t, core);
        }
        catch (Exception e) { App.Log(e); }
    }

    static bool TryWeb(string source, out Uri uri) =>
        Uri.TryCreate(source, UriKind.Absolute, out uri!) && uri.Scheme is "http" or "https";

    /// <summary>A sign-in noticed on a page, a password sent, or one settled. Only the page's own origin is heard: a frame of another site has no say.</summary>
    bool FormMessage(Tab tab, CoreWebView2 core, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            var m = doc.RootElement;
            if (m.ValueKind != JsonValueKind.Object || !m.TryGetProperty("type", out var ty) || ty.GetString() is not { } type || !type.StartsWith("form.", StringComparison.Ordinal)) return false;
            if (!TryWeb(core.Source, out var page) || !TryWeb(e.Source, out var from) || !string.Equals(page.Host, from.Host, StringComparison.OrdinalIgnoreCase) || page.Port != from.Port) return true;
            string S(string k) => m.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
            var host = page.Host.ToLowerInvariant();
            App.Trace($"form {type} {S("kind")} {host}");
            switch (type)
            {
                case "form.focus" when tab == live: OfferForField(tab, core, page, S("kind")); break;
                case "form.card" when tab == live: OfferFromCard(tab, core, page, S("kind")); break;
                case "form.submit":
                    var password = S("password");
                    if (password.Length == 0 || Shared.Vault.IsNever(host)) break;
                    pendingLogin[tab.Id] = new PendingLogin(host, S("user"), password, page.Scheme == "http", DateTime.UtcNow);
                    break;
                case "form.settled": OfferToSave(tab); break;
            }
            return true;
        }
        catch (JsonException) { return false; }
    }

    /// <summary>The page was left or reloaded after a password was sent: it took it, unless it shows a sign-in again.</summary>
    async void OnFormNavigated(Tab t, CoreWebView2 core)
    {
        if (!pendingLogin.TryGetValue(t.Id, out var p) || DateTime.UtcNow - p.At > TimeSpan.FromSeconds(15)) return;
        try
        {
            var again = await core.ExecuteScriptAsync("[...document.querySelectorAll('input[type=password]')].some(p => { const r = p.getBoundingClientRect(); return r.width > 0 && r.height > 0; })");
            if (again == "true") { pendingLogin.Remove(t.Id); return; }
        }
        catch { }
        OfferToSave(t);
    }

    void OfferToSave(Tab t)
    {
        if (!pendingLogin.Remove(t.Id, out var p) || DateTime.UtcNow - p.At > TimeSpan.FromSeconds(20)) { App.Trace("form offer skipped"); return; }
        var vault = Shared.Vault;
        var same = vault.KeptFor(p.Host).FirstOrDefault(k => k.User == p.User);
        var who = p.User.Length > 0 ? p.User : "this sign-in";
        if (same is not null)
        {
            if (vault.SecretOf(same) == p.Password) { vault.Touch(same); return; }
            Ask($"Update the password for {who} at {p.Host}?", "The one kept is replaced. It stays protected for this Windows account.",
            [
                ("Update", () => vault.Save(p.Host, p.User, p.Password, DateTimeOffset.UtcNow, p.Clear)),
                ("Not now", () => { }),
            ]);
            return;
        }
        Ask($"Save the password for {who} at {p.Host}?", "It is kept protected for this Windows account, and Windows Hello can guard it.",
        [
            ("Save", () => vault.Save(p.Host, p.User, p.Password, DateTimeOffset.UtcNow, p.Clear)),
            ("Never for this site", () => vault.Never(p.Host)),
            ("Not now", () => { }),
        ]);
    }

    /// <summary>The caret went into a sign-in box: the accounts kept for the site are offered, once for a page. A sign-up's new-password box is offered a strong one.</summary>
    void OfferForField(Tab tab, CoreWebView2 core, Uri page, string kind)
    {
        var host = page.Host.ToLowerInvariant();
        var key = page.AbsoluteUri + "|" + kind;
        if (formOffered.TryGetValue(tab.Id, out var seen) && seen == key) return;
        if (kind == "new")
        {
            formOffered[tab.Id] = key;
            Ask($"Use a strong password for {host}?", "Search makes one and fills it in. It is offered for keeping when you sign up.",
            [
                ("Use", () => _ = core.ExecuteScriptAsync("window.__searchFillNew(" + JsonSerializer.Serialize(PasswordGenerator.Make()) + ")")),
                ("Not now", () => { }),
            ]);
            return;
        }
        // Kept from a page sent in the clear: only those are offered on one. One kept from https is never handed to a page anyone on the way could have written.
        var accounts = Shared.Vault.KeptMatching(host).Where(k => page.Scheme == "https" || k.Clear).Take(8).ToList();
        if (accounts.Count == 0) return;
        formOffered[tab.Id] = key;
        var list = new ListBox { MaxHeight = 150, Margin = new Thickness(0, 0, 0, 4), SelectedIndex = 0 };
        System.Windows.Automation.AutomationProperties.SetName(list, "Accounts");
        foreach (var k in accounts)
            list.Items.Add(new ListBoxItem { Content = (k.User.Length > 0 ? k.User : "(no name)") + (k.Host != host ? "  ·  " + k.Host : ""), Tag = k });
        Ask($"Sign in to {host}", "Choose an account to fill in.",
        [
            ("Fill", () => { if ((list.SelectedItem as ListBoxItem)?.Tag is Kept chosen) _ = FillLogin(core, host, chosen); }),
            ("Not now", () => { }),
        ], list);
    }

    /// <summary>The caret went into a name, email, phone or address box: the rows of your card for it, once for a page and kind. Nothing goes in until a row is picked.</summary>
    void OfferFromCard(Tab tab, CoreWebView2 core, Uri page, string kind)
    {
        if (!Shared.Prefs.Data.FillFromCard) return;
        var key = page.AbsoluteUri + "|card|" + kind;
        if (formOffered.TryGetValue(tab.Id, out var seen) && seen == key) return;
        var rows = MeCard.Offers(MeCard.Load(), kind);
        if (rows.Count == 0) return;
        formOffered[tab.Id] = key;
        var list = new ListBox { MaxHeight = 150, Margin = new Thickness(0, 0, 0, 4), SelectedIndex = 0 };
        System.Windows.Automation.AutomationProperties.SetName(list, "Card rows");
        foreach (var r in rows) list.Items.Add(new ListBoxItem { Content = r.Detail.Length > 0 ? r.Title + "  ·  " + r.Detail : r.Title, Tag = r });
        Ask($"Fill in from your card at {page.Host}?", "The empty boxes of this form that your card can answer. Nothing is sent until you submit.",
        [
            ("Fill", () => { if ((list.SelectedItem as ListBoxItem)?.Tag is MeOffer o) _ = FillCard(core, o); }),
            ("Not now", () => { }),
        ], list);
    }

    async Task FillCard(CoreWebView2 core, MeOffer offer)
    {
        var n = await core.ExecuteScriptAsync("window.__searchFillCard(" + JsonSerializer.Serialize(offer.Fields) + ")");
        App.Trace("card fill " + n);
    }

    /// <summary>One kept account into the page. Nothing is ever filled on page load: this is by a pick, and, with Settings › Passwords › Ask Windows Hello on, after the person has said yes.</summary>
    async Task FillLogin(CoreWebView2 core, string host, Kept chosen)
    {
        if (Shared.Prefs.Data.AskHello && !await Hello.Prove($"Fill in the password for {chosen.User} at {chosen.Host}", host)) return;
        if (Shared.Vault.Login(chosen) is not { } login) return;
        var ok = await core.ExecuteScriptAsync("window.__searchFill(" + JsonSerializer.Serialize(login.User) + "," + JsonSerializer.Serialize(login.Password) + ")");
        App.Trace("password fill " + (ok == "true" ? "done" : "found no box"));
        if (ok == "true") Shared.Vault.Touch(chosen);
    }

    // MARK: - Settings › Passwords

    object VaultMessage() => new
    {
        type = "vault",
        items = Shared.Vault.All().Select(k => new { host = k.Host, user = k.User, used = k.Used?.ToUniversalTime().ToString("o"), clear = k.Clear }),
        never = Shared.Vault.NeverList(),
    };

    /// <summary>A password is shown or copied only to the person the PC belongs to.</summary>
    async Task RevealPassword(CoreWebView2 core, string host, string user, bool copy)
    {
        if (!await Hello.Prove($"{(copy ? "Copy" : "Show")} the password for {(user.Length > 0 ? user : "this sign-in")} at {host}")) return;
        if (Shared.Vault.SecretOf(host, user) is not { } secret) return;
        if (copy)
        {
            try
            {
                Clipboard.SetText(secret);
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
                timer.Tick += (_, _) => { timer.Stop(); try { if (Clipboard.ContainsText() && Clipboard.GetText() == secret) Clipboard.Clear(); } catch { } };
                timer.Start();
            }
            catch { }
            Send(core, new { type = "vault.copied", host, user });
        }
        else Send(core, new { type = "vault.secret", host, user, secret });
    }

    void WatchVault() => Shared.Vault.Changed += () => Dispatcher.BeginInvoke(() => PostToPages(VaultMessage(), "settings"));
}
