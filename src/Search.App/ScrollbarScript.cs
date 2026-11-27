namespace Search.App;

/// <summary>
/// The page's own scrollbar as Zen's: the window's bar is hidden and drawn again over the page, slim, with no arrows and no track.
/// It shows while the page scrolls or the pointer comes near it, fades after a second of nothing, and under the pointer (or while dragged)
/// it widens and steps in from the edge so it is easy to grab. Lives in a closed shadow root on the top document only; scroll boxes
/// inside the page keep Edge's own overlay bars.
/// </summary>
static class ScrollbarScript
{
    public const string Source = """
    (() => {
      if (window.top !== window || document.__searchBars) return;
      document.__searchBars = true;
      const IDLE = 1000, NEAR = 18;
      try {
        const hide = new CSSStyleSheet();
        hide.replaceSync('html { scrollbar-width: none !important; } html::-webkit-scrollbar { display: none !important; }');
        document.adoptedStyleSheets = [...document.adoptedStyleSheets, hide];
      } catch { return; }
      const host = document.createElement('search-scrollbars');
      host.style.cssText = 'all: initial; position: fixed; inset: 0; pointer-events: none; z-index: 2147483647; display: block;';
      const root = host.attachShadow({ mode: 'closed' });
      const sheet = new CSSStyleSheet();
      sheet.replaceSync(`
        .rail { position: fixed; pointer-events: none; opacity: 0; transition: opacity .3s ease; }
        .rail.on { opacity: 1; transition-duration: .12s; }
        .rail.live { pointer-events: auto; }
        .v { top: 0; right: 0; bottom: 0; width: 16px; }
        .h { left: 0; right: 0; bottom: 0; height: 16px; }
        .both.v { bottom: 16px; } .both.h { right: 16px; }
        .track { position: absolute; border-radius: 99px; background: var(--ink); opacity: 0; transition: opacity .15s ease, width .15s ease, height .15s ease, right .15s ease, bottom .15s ease; }
        .thumb { position: absolute; border-radius: 99px; background: var(--ink); opacity: .45; transition: opacity .15s ease, width .15s ease, height .15s ease, right .15s ease, bottom .15s ease; }
        .v .track, .v .thumb { right: 2px; width: 4px; } .v .track { top: 4px; bottom: 4px; }
        .h .track, .h .thumb { bottom: 2px; height: 4px; } .h .track { left: 4px; right: 4px; }
        .hot .thumb { opacity: .62; } .hot .track { opacity: .08; }
        .v.hot .track, .v.hot .thumb { right: 4px; width: 8px; }
        .h.hot .track, .h.hot .thumb { bottom: 4px; height: 8px; }
        .rail.drag .thumb { opacity: .75; }
        @media print { :host { display: none !important; } }
      `);
      root.adoptedStyleSheets = [sheet];
      const div = cls => { const d = document.createElement('div'); d.className = cls; return d; };
      const make = cls => { const r = div('rail ' + cls); r.append(div('track'), div('thumb')); root.appendChild(r); return r; };
      const bars = { v: make('v'), h: make('h') };
      let timer = 0, dragging = null, shown = false;
      const se = () => document.scrollingElement || document.documentElement;
      const scrolls = (el, axis) => {
        const of = n => { const cs = n && getComputedStyle(n); return cs ? (axis === 'v' ? cs.overflowY : cs.overflowX) : 'visible'; };
        const html = of(document.documentElement);
        // The viewport takes the body's overflow when the root's is visible.
        const ov = html === 'visible' ? of(document.body) : html;
        if (ov === 'hidden' || ov === 'clip') return false;
        return axis === 'v' ? el.scrollHeight - innerHeight > 1 : el.scrollWidth - innerWidth > 1;
      };
      const ink = () => {
        const parse = c => { const m = c && c.match(/rgba?\(([\d.]+),\s*([\d.]+),\s*([\d.]+)(?:,\s*([\d.]+))?/); return m && (m[4] === undefined || +m[4] > 0.5) ? [+m[1], +m[2], +m[3]] : null; };
        let rgb = (document.body && parse(getComputedStyle(document.body).backgroundColor)) || parse(getComputedStyle(document.documentElement).backgroundColor);
        let dark = rgb ? (0.299 * rgb[0] + 0.587 * rgb[1] + 0.114 * rgb[2]) < 128 : /dark/.test(getComputedStyle(document.documentElement).colorScheme) && matchMedia('(prefers-color-scheme: dark)').matches;
        host.style.setProperty('--ink', dark ? '#f2f2f2' : '#1c1c1c');
      };
      const place = () => {
        if (!host.isConnected) document.documentElement.appendChild(host);
        const el = se(), v = scrolls(el, 'v'), h = scrolls(el, 'h');
        for (const [axis, on] of [['v', v], ['h', h]]) {
          const r = bars[axis];
          r.style.display = on ? '' : 'none';
          r.classList.toggle('both', v && h);
          if (!on) continue;
          const t = r.lastChild;
          const span = axis === 'v' ? r.clientHeight - 8 : r.clientWidth - 8;
          const view = axis === 'v' ? innerHeight : innerWidth, full = axis === 'v' ? el.scrollHeight : el.scrollWidth;
          const at = axis === 'v' ? el.scrollTop : Math.abs(el.scrollLeft);
          const size = Math.max(28, span * view / full), room = Math.max(1, full - view);
          const pos = 4 + (span - size) * Math.min(1, at / room);
          if (axis === 'v') { t.style.top = pos + 'px'; t.style.height = size + 'px'; } else { t.style.left = pos + 'px'; t.style.width = size + 'px'; }
        }
        return v || h;
      };
      const show = () => {
        if (!place()) return;
        if (!shown) ink();
        shown = true;
        for (const r of Object.values(bars)) r.classList.add('on', 'live');
        clearTimeout(timer);
        timer = setTimeout(fade, IDLE);
      };
      const fade = () => {
        if (dragging || Object.values(bars).some(r => r.classList.contains('hot'))) { timer = setTimeout(fade, IDLE); return; }
        shown = false;
        for (const r of Object.values(bars)) r.classList.remove('on', 'live', 'hot');
      };
      addEventListener('scroll', () => { if (!dragging) show(); else place(); }, { passive: true });
      addEventListener('resize', () => { if (shown) place(); }, { passive: true });
      document.addEventListener('mousemove', e => {
        if (dragging) return;
        const nearV = e.clientX >= innerWidth - NEAR, nearH = e.clientY >= innerHeight - NEAR;
        if (nearV || nearH) show();
        // Hot by where the pointer is: a bar that just showed under a still pointer gets no pointerenter.
        for (const r of Object.values(bars)) {
          const b = r.getBoundingClientRect(), over = shown && r.style.display !== 'none' && e.clientX >= b.left && e.clientX <= b.right && e.clientY >= b.top && e.clientY <= b.bottom;
          r.classList.toggle('hot', over);
        }
      }, { passive: true, capture: true });
      document.addEventListener('mouseleave', () => { if (!dragging) for (const r of Object.values(bars)) r.classList.remove('hot'); });
      for (const [axis, r] of Object.entries(bars)) {
        r.addEventListener('pointerdown', e => {
          if (e.button !== 0) return;
          e.preventDefault(); e.stopPropagation();
          const el = se(), t = r.lastChild, box = t.getBoundingClientRect();
          const v = axis === 'v', p = v ? e.clientY : e.clientX;
          const start = v ? box.top : box.left, end = v ? box.bottom : box.right;
          if (p < start || p > end) {
            const page = (v ? innerHeight : innerWidth) * 0.875 * (p < start ? -1 : 1);
            el.scrollBy({ [v ? 'top' : 'left']: page, behavior: 'smooth' });
            show(); return;
          }
          dragging = axis; r.classList.add('drag', 'hot'); r.setPointerCapture(e.pointerId);
          const from = v ? el.scrollTop : el.scrollLeft;
          const span = (v ? r.clientHeight : r.clientWidth) - 8 - (end - start);
          const full = (v ? el.scrollHeight - innerHeight : el.scrollWidth - innerWidth);
          const move = m => { const d = (v ? m.clientY : m.clientX) - p; el[v ? 'scrollTop' : 'scrollLeft'] = from + d * full / Math.max(1, span); };
          const up = () => {
            dragging = null; r.classList.remove('drag');
            r.removeEventListener('pointermove', move); r.removeEventListener('pointerup', up); r.removeEventListener('pointercancel', up);
            show();
          };
          r.addEventListener('pointermove', move); r.addEventListener('pointerup', up); r.addEventListener('pointercancel', up);
        });
      }
      const mount = () => { if (!host.isConnected && document.documentElement) document.documentElement.appendChild(host); };
      mount();
      document.addEventListener('DOMContentLoaded', () => { mount(); ink(); }, { once: true });
      addEventListener('load', () => { mount(); ink(); }, { once: true });
    })();
    """;
}
