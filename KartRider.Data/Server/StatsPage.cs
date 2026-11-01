using System.Text;

namespace KartRider
{
    /// <summary>
    /// 战绩查询页面（内嵌 HTML，由 LocalAdminApi 在 /stats 提供服务）。
    /// 数据来自 /api/userlist、/api/matches、/api/user，全部是本机服务器自己记录的对局。
    /// </summary>
    public static class StatsPage
    {
        public const string Html = @"<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>跑跑卡丁车 · 战绩查询</title>
<style>
  :root { color-scheme: light dark; }
  * { box-sizing: border-box; }
  body { margin:0; padding:32px 16px; min-height:100vh; background:#f3f5f9; color:#2b2f36;
         font-family:""Microsoft YaHei UI"",""Microsoft YaHei"",system-ui,sans-serif; }
  .wrap { max-width:760px; margin:0 auto; }
  h1 { font-size:20px; margin:0 0 4px; }
  .sub { font-size:13px; color:#8a919c; margin-bottom:20px; }
  .card { background:#fff; border:1px solid #e4e7ed; border-radius:12px; padding:18px 20px; margin-bottom:14px; }
  .row { display:flex; gap:10px; align-items:center; flex-wrap:wrap; }
  input, select { padding:8px 12px; border:1px solid #e4e7ed; border-radius:8px; font-size:14px; background:#f5f7fb; color:inherit; }
  button { padding:8px 18px; border:0; border-radius:8px; background:#4c9eea; color:#fff; font-size:14px; cursor:pointer; }
  button:hover { background:#3d8edb; }
  .stats { display:flex; gap:12px; flex-wrap:wrap; margin-top:14px; }
  .stat { flex:1; min-width:90px; background:#f5f7fb; border-radius:10px; padding:12px 14px; text-align:center; }
  .stat b { display:block; font-size:22px; color:#4c9eea; }
  .stat span { font-size:12px; color:#8a919c; }
  table { width:100%; border-collapse:collapse; font-size:13px; margin-top:8px; }
  th, td { padding:8px 6px; text-align:left; border-bottom:1px solid #eef0f4; }
  th { color:#8a919c; font-weight:600; }
  .r1 { color:#e8a33d; font-weight:700; }
  .r2 { color:#9aa4b2; font-weight:700; }
  .r3 { color:#b07a4a; font-weight:700; }
  .dnf { color:#e5484d; }
  .me { color:#4c9eea; font-weight:700; }
  .chips { display:flex; flex-wrap:wrap; gap:8px; margin-top:12px; }
  .chip { background:#f5f7fb; border-radius:8px; padding:6px 10px; font-size:12px; }
  .chip b { color:#4c9eea; margin-right:4px; }
  .empty { color:#8a919c; font-size:13px; padding:10px 0; }
  @media (prefers-color-scheme: dark) {
    body { background:#1e2126; color:#e6e9ef; }
    .card { background:#272b31; border-color:#3a4048; }
    input, select { background:#1b1e23; border-color:#3a4048; }
    .stat { background:#1b1e23; }
    th, td { border-color:#32373e; }
  }
</style>
</head>
<body>
<div class=""wrap"">
  <h1>跑跑卡丁车 · 战绩查询</h1>
  <div class=""sub"">数据来自本机服务器记录的对局（LocalAdminApi /api/*）</div>

  <div class=""card"">
    <div class=""row"">
      <input id=""nick"" list=""nickList"" placeholder=""输入或选择昵称"">
      <datalist id=""nickList""></datalist>
      <button onclick=""load()"">查询</button>
    </div>
    <div id=""stats""></div>
  </div>

  <div class=""card"">
    <b style=""font-size:14px"">当前装扮</b>
    <div id=""appearance""><div class=""empty"">输入昵称后查看</div></div>
  </div>

  <div class=""card"">
    <b style=""font-size:14px"">最近对局</b>
    <div id=""matches""><div class=""empty"">暂无记录</div></div>
  </div>
</div>

<script>
var token = new URLSearchParams(location.search).get('token') || '';
function api(path) {
  var sep = path.indexOf('?') >= 0 ? '&' : '?';
  return fetch(path + sep + 'token=' + encodeURIComponent(token)).then(function (r) {
    if (!r.ok) throw new Error(r.status);
    return r.json();
  });
}
function esc(s) { return String(s == null ? '' : s).replace(/[&<>]/g, function (c) {
  return { '&':'&amp;', '<':'&lt;', '>':'&gt;' }[c]; }); }
function rankText(rank, dnf) { return dnf ? 'DNF' : (rank || '-'); }
function rankClass(rank, dnf) { return dnf ? 'dnf' : (rank === 1 ? 'r1' : rank === 2 ? 'r2' : rank === 3 ? 'r3' : ''); }

function load() {
  var nick = document.getElementById('nick').value.trim();
  renderUser(nick);
  renderAppearance(nick);
  renderMatches(nick);
  if (nick) localStorage.setItem('kr-last-nick', nick);
}

function renderUser(nick) {
  var box = document.getElementById('stats');
  if (!nick) { box.innerHTML = '<div class=""empty"">输入昵称后查看生涯统计</div>'; return; }
  api('/api/user?nickname=' + encodeURIComponent(nick)).then(function (s) {
    box.innerHTML =
      '<div class=""stats"">' +
      '<div class=""stat""><b>' + s.games + '</b><span>总场次</span></div>' +
      '<div class=""stat""><b>' + s.wins + '</b><span>冠军</span></div>' +
      '<div class=""stat""><b>' + s.podium + '</b><span>前三</span></div>' +
      '<div class=""stat""><b>' + s.dnf + '</b><span>未完成</span></div>' +
      '</div>' +
      (s.lastTime ? '<div class=""empty"">最近一场：' + esc(s.lastTime) + '</div>' : '');
  }).catch(function () { box.innerHTML = '<div class=""empty"">查询失败</div>'; });
}

var currentNick = '';

function timeText(ms) {
  if (ms == null || ms <= 0) { return 'DNF'; }
  // 卡丁车计时格式：分.秒.厘秒，如 5分06秒36 → 05.06.36
  var total = Math.floor(ms);
  function pad(n) { return n < 10 ? '0' + n : '' + n; }
  var mm = Math.floor(total / 60000);
  var ss = Math.floor((total % 60000) / 1000);
  var cc = Math.floor((total % 1000) / 10);
  return pad(mm) + '.' + pad(ss) + '.' + pad(cc);
}

function renderAppearance(nick) {
  var box = document.getElementById('appearance');
  if (!nick) { box.innerHTML = '<div class=""empty"">输入昵称后查看</div>'; return; }
  api('/api/appearance?nickname=' + encodeURIComponent(nick)).then(function (a) {
    if (!a.found || !a.items || !a.items.length) {
      box.innerHTML = '<div class=""empty"">没有装扮数据（该玩家需要登录过一次服务器）</div>';
      return;
    }
    var chips = a.items.map(function (it) {
      return '<span class=""chip""><b>' + esc(it.label) + '</b>' + esc(it.name) + '</span>';
    }).join('');
    box.innerHTML = '<div class=""chips"">' + chips + '</div>' +
      (a.catalogReady ? '' : '<div class=""empty"">物品目录正在后台解析，稍后刷新可显示名称</div>');
  }).catch(function () { box.innerHTML = '<div class=""empty"">查询失败</div>' });
}

function renderMatches(nick) {
  currentNick = nick || '';
  var box = document.getElementById('matches');
  api('/api/matches?n=50' + (nick ? '&nickname=' + encodeURIComponent(nick) : '')).then(function (list) {
    if (!list.length) { box.innerHTML = '<div class=""empty"">暂无对局记录（打完一局后这里就会出现）</div>'; return; }
    var rows = list.map(function (m) {
      var ps = (m.players || []).slice()
        .sort(function (a, b) { return (a.rank || 99) - (b.rank || 99); })
        .map(function (p) {
          var cls = p.ai ? 'empty' : rankClass(p.rank, p.dnf);
          var me = (!p.ai && currentNick && p.nickname.toLowerCase() === currentNick.toLowerCase());
          if (me) { cls = 'me'; }
          var rk = p.dnf ? 'DNF' : ('第' + p.rank + '名，' + timeText(p.timeMs));
          var tag = p.ai ? ' <span class=""empty"">AI</span>' : '';
          return '<span class=""' + cls + '"">' + esc(p.nickname) + tag + '（' + rk + '）</span>';
        }).join(' ');

      var mine = (m.players || []).find(function (p) {
        return !p.ai && currentNick && p.nickname.toLowerCase() === currentNick.toLowerCase();
      });
      var myRank = mine ? rankText(mine.rank, mine.dnf) : '-';
      var myTime = mine ? timeText(mine.timeMs) : '-';

      return '<tr><td>' + esc(m.time) + '</td><td>' + esc(m.track) + '</td><td>' + myRank +
             '</td><td>' + myTime + '</td><td>' + ps + '</td></tr>';
    }).join('');
    box.innerHTML =
      '<table><tr><th>时间</th><th>赛道</th><th>名次</th><th>用时</th><th>本局全部玩家（名次，用时）</th></tr>' + rows + '</table>' +
      '<div class=""empty"">DNF = 服务器没有收到这一局的过线时间（常见于提前退出房间，或结算倒计时先到期）。AI 的成绩由客户端模拟上报。</div>';
  }).catch(function () { box.innerHTML = '<div class=""empty"">读取失败</div>' });
}

api('/api/userlist').then(function (names) {
  document.getElementById('nickList').innerHTML =
    names.map(function (n) { return '<option value=""' + esc(n) + '"">'; }).join('');
}).catch(function () {});

var last = localStorage.getItem('kr-last-nick');
if (last) { document.getElementById('nick').value = last; load(); }
</script>
</body>
</html>";
    }
}
