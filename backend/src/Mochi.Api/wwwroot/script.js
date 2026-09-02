/* mochi.js - privacy-first analytics. No cookies, no storage, no fingerprinting.
   Honors DNT and Global Privacy Control. https://github.com/hazeliscoding/mochi */
(function () {
  'use strict';
  var w = window, d = document, n = navigator;
  var s = d.currentScript;
  if (!s) return;
  var site = s.getAttribute('data-site');
  if (!site) return;
  if (n.doNotTrack === '1' || w.doNotTrack === '1' || n.msDoNotTrack === '1' || n.globalPrivacyControl) return;

  var endpoint = new URL(s.src).origin + '/api/collect';
  var lastPath = null;
  var first = true;

  function send(payload) {
    try {
      var body = JSON.stringify(payload);
      if (n.sendBeacon) {
        n.sendBeacon(endpoint, new Blob([body], { type: 'text/plain' }));
      } else {
        var x = new XMLHttpRequest();
        x.open('POST', endpoint, true);
        x.setRequestHeader('Content-Type', 'text/plain');
        x.send(body);
      }
    } catch (e) { /* never break the host page */ }
  }

  function pageview() {
    var path = location.pathname + location.search;
    if (path === lastPath) return;
    lastPath = path;
    var payload = { site: site, type: 'pageview', path: path };
    if (first && d.referrer) payload.referrer = d.referrer;
    first = false;
    send(payload);
  }

  function api(cmd, arg) {
    if (cmd === 'event' && arg) {
      send({ site: site, type: 'event', name: String(arg), path: location.pathname });
    }
  }

  /* replay calls queued by the stub before this script loaded */
  var q = w.mochi && w.mochi.q;
  w.mochi = api;
  if (q) for (var i = 0; i < q.length; i++) api.apply(null, q[i]);

  /* SPA route changes: pushState + back/forward */
  if (w.history.pushState) {
    var push = w.history.pushState;
    w.history.pushState = function () {
      push.apply(this, arguments);
      pageview();
    };
    w.addEventListener('popstate', pageview);
  }

  pageview();
})();
