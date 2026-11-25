namespace Search.App;

/// <summary>Reading mode: the article and nothing arranged around it. Run again, it closes. The article is the piece of the page carrying the most prose, punished for the links inside it (Reader.swift's heuristic).</summary>
static class ReaderScript
{
    public const string Source = """
    (() => {
      const ID = '__search_reader';
      const old = document.getElementById(ID);
      if (old) { old.remove(); document.documentElement.style.overflow = ''; return 'closed'; }
      const score = new Map();
      for (const p of document.querySelectorAll('p')) {
        const t = (p.innerText || '').trim();
        if (t.length < 60) continue;
        const links = [...p.querySelectorAll('a')].reduce((n, a) => n + (a.innerText || '').length, 0);
        const own = Math.max(0, t.length - 2 * links);
        for (const [el, share] of [[p.parentElement, 1], [p.parentElement && p.parentElement.parentElement, 0.5]]) {
          if (el) score.set(el, (score.get(el) || 0) + own * share);
        }
      }
      let best = null, top = 0;
      for (const [el, s] of score) if (s > top) { top = s; best = el; }
      if (!best || top < 200) return 'none';
      const box = document.createElement('div');
      box.id = ID;
      const heading = document.querySelector('h1');
      const title = (heading && heading.innerText.trim()) || document.title;
      const clone = best.cloneNode(true);
      clone.querySelectorAll('script,style,noscript,nav,aside,footer,form,iframe,button,svg,[aria-hidden="true"],[role="navigation"],[class*="share"],[class*="related"],[class*="comment"],[class*="newsletter"]').forEach(n => n.remove());
      let paragraphs = 0;
      const body = document.createElement('div');
      for (const n of clone.querySelectorAll('h1,h2,h3,h4,p,ul,ol,blockquote,pre,figure,img')) {
        if (n.closest('figure') && n.tagName === 'IMG') continue;
        if (n.tagName === 'P') { if (!n.innerText.trim()) continue; paragraphs++; }
        if (n.tagName === 'H1' && n.innerText.trim() === title) continue;
        if (n.parentElement && n.parentElement.closest('ul,ol,blockquote') && !['UL','OL','BLOCKQUOTE'].includes(n.tagName)) continue;
        body.appendChild(n.cloneNode(true));
      }
      const style = document.createElement('style');
      style.textContent = `
        #${ID} { position: fixed; inset: 0; z-index: 2147483647; overflow: auto; background: #fbfaf8; color: #222; font: 19px/1.65 Georgia, 'Times New Roman', serif; }
        @media (prefers-color-scheme: dark) { #${ID} { background: #1e1e1f; color: #e4e4e4; } }
        #${ID} article { max-width: 680px; margin: 0 auto; padding: 64px 24px 120px; }
        #${ID} h1 { font: 700 36px/1.2 'Segoe UI', system-ui, sans-serif; margin: 0 0 28px; }
        #${ID} h2, #${ID} h3, #${ID} h4 { font-family: 'Segoe UI', system-ui, sans-serif; line-height: 1.3; margin: 1.6em 0 .5em; }
        #${ID} p { margin: 0 0 1.1em; } #${ID} img { max-width: 100%; height: auto; border-radius: 6px; }
        #${ID} a { color: inherit; text-decoration: underline; } #${ID} blockquote { margin: 1em 0; padding-left: 1em; border-left: 3px solid #8885; }
        #${ID} pre { overflow: auto; font-size: 15px; background: #8881; padding: 12px; border-radius: 6px; }
        #${ID} .close { position: fixed; top: 14px; right: 18px; font: 13px 'Segoe UI', system-ui, sans-serif; opacity: .6; cursor: pointer; }`;
      const article = document.createElement('article');
      const h = document.createElement('h1'); h.textContent = title;
      article.append(h, body);
      const close = document.createElement('div'); close.className = 'close'; close.textContent = 'Esc to close';
      box.append(style, article, close);
      document.documentElement.appendChild(box);
      document.documentElement.style.overflow = 'hidden';
      const off = e => { if (e.key === 'Escape') { box.remove(); document.documentElement.style.overflow = ''; removeEventListener('keydown', off, true); } };
      addEventListener('keydown', off, true);
      return 'opened ' + paragraphs;
    })()
    """;
}
