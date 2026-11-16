// تحديد الكل في الجداول: <input type="checkbox" data-check-all="name">
document.addEventListener('change', function (e) {
  var t = e.target;
  if (t && t.dataset && t.dataset.checkAll) {
    document.querySelectorAll('input[name="' + t.dataset.checkAll + '"]').forEach(function (c) { c.checked = t.checked; });
  }
});

// القوائم المتسلسلة: محافظة -> مكتب -> قسم -> شعبة
function fillSelect(sel, items, placeholder) {
  sel.innerHTML = '<option value="">' + (placeholder || '-- اختر --') + '</option>';
  items.forEach(function (i) {
    var o = document.createElement('option'); o.value = i.id; o.textContent = i.name; sel.appendChild(o);
  });
}
function bindCascade(parentId, childId, url, param, resetIds) {
  var p = document.getElementById(parentId), c = document.getElementById(childId);
  if (!p || !c) return;
  p.addEventListener('change', function () {
    (resetIds || []).forEach(function (id) { var el = document.getElementById(id); if (el) fillSelect(el, []); });
    if (!p.value) { fillSelect(c, []); return; }
    fetch(url + '?' + param + '=' + encodeURIComponent(p.value)).then(function (r) { return r.json(); }).then(function (d) { fillSelect(c, d); });
  });
}

document.addEventListener('change', function (e) {
  var input = e.target;
  if (!input.matches || !input.matches('[data-file-picker]')) return;

  var display = document.getElementById(input.dataset.filePickerDisplay);
  if (!display) return;

  var files = Array.from(input.files || []);
  display.textContent = files.length
    ? files.map(function (file) { return file.name; }).join('، ')
    : 'لا يوجد ملف مضاف';
});

// ================= تنبيه المدير بالأنشطة الجديدة (صوت + رسالة + إشعار ويندوز) =================
// يعمل فقط عندما يوجد #activityToasts في الصفحة (يُضاف في _Layout للمدير فقط).
(function () {
  var box = document.getElementById('activityToasts');
  if (!box) return;

  var url = box.dataset.newActivitiesUrl;
  var storeKey = 'lastSeenActivityId:' + (box.dataset.user || '');
  var POLL_MS = 15000;
  var lastSeen = null;
  var audioCtx = null;
  var enableBtn = document.getElementById('enableActivityAlerts');
  var baseTitle = document.title;

  function readStore() { try { var v = localStorage.getItem(storeKey); return v === null ? null : parseInt(v, 10); } catch (e) { return null; } }
  function writeStore(v) { try { localStorage.setItem(storeKey, String(v)); } catch (e) { } }

  // ---- الصوت: المتصفح يسمح به فقط بعد نقرة أو ضغطة زر في الصفحة ----
  function unlockAudio() {
    try {
      if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)();
      if (audioCtx.state === 'suspended') audioCtx.resume();
    } catch (e) { }
  }
  ['click', 'keydown', 'touchstart'].forEach(function (ev) { document.addEventListener(ev, unlockAudio, { once: false, passive: true }); });

  function playChime() {
    unlockAudio();
    if (!audioCtx || audioCtx.state !== 'running') return false;
    var t = audioCtx.currentTime;
    [[880, 0], [1175, 0.18], [1568, 0.36]].forEach(function (n) {
      var o = audioCtx.createOscillator(), g = audioCtx.createGain();
      o.type = 'sine'; o.frequency.value = n[0];
      g.gain.setValueAtTime(0.0001, t + n[1]);
      g.gain.exponentialRampToValueAtTime(0.35, t + n[1] + 0.02);
      g.gain.exponentialRampToValueAtTime(0.0001, t + n[1] + 0.45);
      o.connect(g); g.connect(audioCtx.destination);
      o.start(t + n[1]); o.stop(t + n[1] + 0.5);
    });
    return true;
  }

  // ---- زر «تفعيل التنبيهات»: يطلب إذن إشعارات ويندوز ويفعّل الصوت ----
  function refreshEnableBtn() {
    if (!enableBtn) return;
    var needPerm = ('Notification' in window) && Notification.permission === 'default';
    var needAudio = !audioCtx || audioCtx.state !== 'running';
    enableBtn.style.display = (needPerm || needAudio) ? '' : 'none';
  }
  if (enableBtn) {
    enableBtn.addEventListener('click', function () {
      unlockAudio();
      playChime(); // صوت تجربة
      if ('Notification' in window && Notification.permission === 'default') {
        Notification.requestPermission().then(refreshEnableBtn);
      }
      setTimeout(refreshEnableBtn, 300);
    });
  }
  document.addEventListener('click', function () { setTimeout(refreshEnableBtn, 300); });
  refreshEnableBtn();

  function esc(s) { var d = document.createElement('div'); d.textContent = s == null ? '' : String(s); return d.innerHTML; }

  function showToast(item) {
    var el = document.createElement('div');
    el.className = 'toast border-0 shadow';
    el.setAttribute('role', 'alert');
    el.innerHTML =
      '<div class="toast-header bg-success text-white">' +
        '<strong class="me-auto">🔔 نشاط جديد</strong>' +
        '<button type="button" class="btn-close btn-close-white ms-2" data-bs-dismiss="toast"></button>' +
      '</div>' +
      '<div class="toast-body">' +
        'أضاف <b>' + esc(item.creator) + '</b> النشاط #' + esc(item.id) +
        (item.place ? ' — ' + esc(item.place) : '') +
        (item.date ? '<small class="d-block text-muted">' + esc(item.date) + '</small>' : '') +
        '<a class="btn btn-sm btn-primary mt-2" href="' + esc(item.url) + '">عرض النشاط</a>' +
      '</div>';
    box.appendChild(el);
    if (window.bootstrap && bootstrap.Toast) {
      var t = new bootstrap.Toast(el, { autohide: false });
      el.addEventListener('hidden.bs.toast', function () { el.remove(); });
      t.show();
    } else { el.classList.add('show'); }
  }

  function systemNotify(item) {
    if (!('Notification' in window) || Notification.permission !== 'granted') return;
    try {
      var n = new Notification('نشاط جديد #' + item.id, {
        body: 'أضافه ' + (item.creator || '') + (item.place ? ' — ' + item.place : ''),
        tag: 'activity-' + item.id
      });
      n.onclick = function () { window.focus(); location.href = item.url; };
    } catch (e) { }
  }

  function flashTitle(count) {
    document.title = '(' + count + ') نشاط جديد - ' + baseTitle;
    var restore = function () { document.title = baseTitle; document.removeEventListener('visibilitychange', restore); };
    if (document.visibilityState === 'visible') setTimeout(restore, 8000);
    else document.addEventListener('visibilitychange', restore);
  }

  function poll() {
    var after = lastSeen === null ? -1 : lastSeen;
    fetch(url + '?afterId=' + after, { headers: { 'Accept': 'application/json' }, credentials: 'same-origin' })
      .then(function (r) { return r.ok ? r.json() : null; })
      .then(function (d) {
        if (!d) return;
        if (lastSeen === null) { lastSeen = d.latestId; writeStore(lastSeen); return; } // أول مرة: نقطة بداية بلا تنبيه
        if (d.items && d.items.length) {
          d.items.forEach(function (it) { showToast(it); systemNotify(it); });
          playChime();
          flashTitle(d.items.length);
        }
        if (d.latestId > lastSeen) { lastSeen = d.latestId; writeStore(lastSeen); }
      })
      .catch(function () { });
  }

  lastSeen = readStore();
  poll();
  setInterval(poll, POLL_MS);
})();

// ================= الوضع الليلي / النهاري =================
// الوضع الأولي يُطبَّق في <head> داخل _Layout. هنا: زر التبديل + الحفظ + الطباعة بالوضع النهاري.
(function () {
  var root = document.documentElement;
  function current() { return root.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light'; }
  function apply(t, save) {
    root.setAttribute('data-bs-theme', t);
    if (save) { try { localStorage.setItem('theme', t); } catch (e) { } }
  }

  document.addEventListener('click', function (e) {
    var btn = e.target.closest && e.target.closest('[data-theme-toggle]');
    if (!btn) return;
    apply(current() === 'dark' ? 'light' : 'dark', true);
  });

  // مزامنة بين التبويبات المفتوحة
  window.addEventListener('storage', function (e) {
    if (e.key === 'theme' && (e.newValue === 'dark' || e.newValue === 'light')) apply(e.newValue, false);
  });

  // إن لم يختر المستخدم وضعاً، نتبع تغيّر إعداد النظام
  if (window.matchMedia) {
    var mq = window.matchMedia('(prefers-color-scheme: dark)');
    var follow = function (ev) {
      var saved = null; try { saved = localStorage.getItem('theme'); } catch (e) { }
      if (saved !== 'dark' && saved !== 'light') apply(ev.matches ? 'dark' : 'light', false);
    };
    if (mq.addEventListener) mq.addEventListener('change', follow); else if (mq.addListener) mq.addListener(follow);
  }

  // الطباعة دائماً بالوضع النهاري (خلفية بيضاء ونص أسود)
  var beforePrint = null;
  window.addEventListener('beforeprint', function () { beforePrint = current(); apply('light', false); });
  window.addEventListener('afterprint', function () { if (beforePrint) apply(beforePrint, false); beforePrint = null; });
})();
