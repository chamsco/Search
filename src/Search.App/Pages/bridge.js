// In the column's Library the page is one section among several: less chrome, tighter margins.
if (new URLSearchParams(location.search).has('embed')) document.documentElement.classList.add('embed');
// The page's door to Search. Only pages served from search.internal are listened to (checked on the other side too).
const handlers = {};
const host = {
  send(type, data = {}) { window.chrome.webview.postMessage({ type, ...data }); },
  on(type, fn) { handlers[type] = fn; },
};
window.chrome.webview.addEventListener('message', e => handlers[e.data.type]?.(e.data));
const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const hostOf = u => { try { return new URL(u).host.replace(/^www\./, ''); } catch { return u; } };
const letter = u => (hostOf(u)[0] || '•').toUpperCase();
function toast(text) { let t = document.querySelector('.toast'); if (!t) { t = document.createElement('div'); t.className = 'toast'; document.body.appendChild(t); } t.textContent = text; t.classList.add('show'); setTimeout(() => t.classList.remove('show'), 1800); }
