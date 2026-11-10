/*!
 * XoFormPro form handler — drop-in, no configuration.
 *
 * Include once per page:
 *   <script src="https://xoformpro.xeniaone.com/forms.js" async></script>
 *
 * It auto-enhances every <form> that posts to the XoFormPro /submit endpoint
 * (or carries a data-success attribute). On submit it sends the data in the
 * background and keeps the visitor on your page:
 *   - data-success="elementId"  -> hides the form and reveals that element
 *   - .form-status (optional)   -> gets "Sending…" / error text
 *   - if a redirect URL is configured on the form, it navigates there instead
 * No part of this requires you to write any JavaScript.
 *
 * Spam protection is fetched per access key from /form-config, so turning on the
 * "I'm not a robot" box or a captcha in the dashboard takes effect on the next
 * page view. The embedding site never changes its markup.
 *
 * If this script fails to load, forms with a real action/method still post
 * natively — which is why the two checks that depend on it (signed token and
 * auto-injected captcha) are opt-in per key rather than on by default.
 */
(function () {
  "use strict";

  var CAPTCHA_SCRIPTS = {
    hcaptcha: "https://js.hcaptcha.com/1/api.js",
    turnstile: "https://challenges.cloudflare.com/turnstile/v0/api.js",
  };
  var CAPTCHA_FIELDS = {
    hcaptcha: "h-captcha-response",
    turnstile: "cf-turnstile-response",
  };
  var CAPTCHA_CLASSES = {
    hcaptcha: "h-captcha",
    turnstile: "cf-turnstile",
  };

  // One in-flight request per (endpoint, key) — several forms on a page share a key.
  var configCache = {};
  var scriptsLoading = {};

  function showStatus(form, text) {
    var status = form.querySelector(".form-status");
    if (status) status.textContent = text || "";
    return status;
  }

  /** Origin of the /submit endpoint the form posts to, so /form-config resolves alongside it. */
  function endpointBase(form) {
    try {
      var url = new URL(form.action, window.location.href);
      // ".../XoFormPro/submit" -> ".../XoFormPro"
      return url.origin + url.pathname.replace(/\/submit(\/[^/]*)?$/, "");
    } catch (e) {
      return "";
    }
  }

  function accessKeyOf(form) {
    var input = form.querySelector('input[name="access_key"]');
    if (input && input.value) return input.value;
    // Also support the key being carried in the action path: /submit/{key}
    var match = /\/submit\/([0-9a-f-]{36})/i.exec(form.action || "");
    return match ? match[1] : null;
  }

  function warn(message) {
    if (window.console && console.warn) console.warn("[XoFormPro] " + message);
  }

  /**
   * Reads the per-key config. A misrouted endpoint is the likely failure here — a dev
   * proxy or reverse proxy that doesn't forward /form-config returns the SPA's index.html
   * with a 200, which parses as neither JSON nor an error. Say so rather than silently
   * skipping protection the dashboard says is switched on.
   */
  function fetchConfig(base, key) {
    var id = base + "|" + key;
    if (!configCache[id]) {
      var url = base + "/form-config?key=" + encodeURIComponent(key);
      configCache[id] = fetch(url)
        .then(function (r) {
          if (!r.ok) {
            warn("form-config returned HTTP " + r.status + " for " + url + " — spam settings not applied.");
            return null;
          }
          var type = r.headers.get("content-type") || "";
          if (type.indexOf("application/json") === -1) {
            warn(
              "form-config returned '" + (type || "no content-type") + "' instead of JSON for " + url +
              ". The request is probably not reaching the API — check that this path is proxied to the backend."
            );
            return null;
          }
          return r.json();
        })
        .catch(function () {
          warn("form-config request failed for " + url + " — spam settings not applied.");
          return null;
        });
    }
    return configCache[id];
  }

  function loadScript(src) {
    if (!scriptsLoading[src]) {
      scriptsLoading[src] = new Promise(function (resolve) {
        var existing = document.querySelector('script[src="' + src + '"]');
        if (existing) { resolve(); return; }
        var s = document.createElement("script");
        s.src = src;
        s.async = true;
        s.defer = true;
        s.onload = function () { resolve(); };
        s.onerror = function () { resolve(); }; // never block submission on a CDN failure
        document.head.appendChild(s);
      });
    }
    return scriptsLoading[src];
  }

  /** Inserts the captcha widget above the submit button, once. */
  function injectCaptcha(form, captcha) {
    var cls = CAPTCHA_CLASSES[captcha.provider];
    if (!cls || form.querySelector("." + cls)) return;

    var widget = document.createElement("div");
    widget.className = cls;
    widget.setAttribute("data-sitekey", captcha.siteKey);
    // Keep the visitor's flow intact: managed Turnstile is normally invisible, and
    // hCaptcha renders a compact checkbox.
    widget.setAttribute("data-size", captcha.provider === "turnstile" ? "flexible" : "normal");

    var submit = form.querySelector('[type="submit"]');
    if (submit && submit.parentNode) submit.parentNode.insertBefore(widget, submit);
    else form.appendChild(widget);

    loadScript(CAPTCHA_SCRIPTS[captcha.provider]);
  }

  /**
   * The provider scripts write their answer into a hidden input. Managed/invisible
   * challenges resolve on their own a moment after load, so give them a short grace
   * period rather than failing a visitor who submitted quickly.
   */
  function waitForCaptcha(form, provider, timeoutMs) {
    var field = CAPTCHA_FIELDS[provider];
    var deadline = Date.now() + (timeoutMs || 6000);

    return new Promise(function (resolve) {
      (function poll() {
        var input = form.querySelector('[name="' + field + '"]');
        if (input && input.value) { resolve(true); return; }
        if (Date.now() > deadline) { resolve(false); return; }
        setTimeout(poll, 150);
      })();
    });
  }

  /**
   * The tick box's stylesheet, injected once per page.
   *
   * Two constraints shape this. It lands on a site whose CSS we have never seen, so every
   * class is prefixed and no bare element is styled. And it cannot read the host's colours,
   * so the frame and label are drawn with currentColor - the widget takes on whatever text
   * colour the surrounding form already uses, and reads correctly on a white site and a
   * dark one without being told which. Only the ticked state asserts the brand teal.
   *
   * Each colour is declared twice: a plain rgba first for browsers without color-mix(),
   * then the currentColor version for those that have it.
   */
  var HUMAN_CSS = [
    '.xofp-human{',
      'display:inline-flex;align-items:center;gap:13px;box-sizing:border-box;',
      'max-width:310px;margin:.75em 0;padding:15px 17px;border-radius:11px;',
      'font:inherit;color:inherit;line-height:1.4;cursor:pointer;',
      'border:1px solid rgba(128,128,128,.35);',
      'border:1px solid color-mix(in srgb,currentColor 20%,transparent);',
      'background:rgba(128,128,128,.06);',
      'background:color-mix(in srgb,currentColor 4%,transparent);',
      'transition:border-color .18s,background .18s}',
    '.xofp-human:hover{',
      'border-color:rgba(128,128,128,.5);',
      'border-color:color-mix(in srgb,currentColor 34%,transparent)}',
    /* Kept in the stylesheet, not inline: if this sheet is ever blocked, the native
       checkbox stays visible and the form still works. */
    '.xofp-human>input{position:absolute;opacity:0;width:0;height:0;margin:0}',
    '.xofp-box{',
      'width:24px;height:24px;flex:none;box-sizing:border-box;border-radius:6px;',
      'display:grid;place-items:center;',
      'border:1.6px solid rgba(128,128,128,.5);',
      'border:1.6px solid color-mix(in srgb,currentColor 34%,transparent);',
      'transition:border-color .18s,background .18s}',
    '.xofp-box svg{',
      'width:15px;height:15px;fill:none;stroke:#fff;stroke-width:3.2;',
      'stroke-linecap:round;stroke-linejoin:round;',
      'stroke-dasharray:22;stroke-dashoffset:22;',
      'transition:stroke-dashoffset .24s ease .04s}',
    '.xofp-human>input:checked~.xofp-box{background:#1f8a99;border-color:#1f8a99}',
    '.xofp-human>input:checked~.xofp-box svg{stroke-dashoffset:0}',
    '.xofp-human>input:focus-visible~.xofp-box{outline:2px solid #1f8a99;outline-offset:3px}',
    '.xofp-txt{display:flex;flex-direction:column;gap:1px;min-width:0}',
    '.xofp-txt b{font-weight:500;font-size:1em}',
    '.xofp-txt small{',
      'font-size:.68em;letter-spacing:.07em;text-transform:uppercase;',
      'color:rgba(128,128,128,.9);',
      'color:color-mix(in srgb,currentColor 48%,transparent)}',
    '@media (prefers-reduced-motion:reduce){',
      '.xofp-human,.xofp-human *{transition-duration:.01ms !important}}'
  ].join('');

  var stylesInjected = false;
  function injectStyles() {
    if (stylesInjected || document.getElementById("xofp-styles")) return;
    stylesInjected = true;
    var style = document.createElement("style");
    style.id = "xofp-styles";
    style.textContent = HUMAN_CSS;
    document.head.appendChild(style);
  }

  /**
   * Renders the "I'm not a robot" panel above the submit button.
   *
   * The box on its own proves nothing - a direct POST can simply send the field. What
   * makes it mean something is the single-use, origin-bound token fetched at page load
   * and verified by /submit: together they say "a browser loaded this page, and a person
   * acted on it". Neither half is reproducible by a script that only knows the endpoint.
   */
  function injectHumanCheck(form) {
    if (form.querySelector(".xofp-human")) return;
    injectStyles();

    var wrap = document.createElement("label");
    wrap.className = "xofp-human";

    var box = document.createElement("input");
    box.type = "checkbox";
    box.name = "not_robot";
    box.value = "1";

    var tick = document.createElement("span");
    tick.className = "xofp-box";
    tick.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>';

    var txt = document.createElement("span");
    txt.className = "xofp-txt";
    var label = document.createElement("b");
    label.textContent = "I'm not a robot";
    var byline = document.createElement("small");
    byline.textContent = "Protected by XOFormPro";
    txt.appendChild(label);
    txt.appendChild(byline);

    wrap.appendChild(box);
    wrap.appendChild(tick);
    wrap.appendChild(txt);

    var submit = form.querySelector('[type="submit"]');
    if (submit && submit.parentNode) submit.parentNode.insertBefore(wrap, submit);
    else form.appendChild(wrap);
  }

  /** Fetches a single-use token and parks it in a hidden field. */
  function attachToken(form, base, key) {
    return fetch(base + "/form-token?key=" + encodeURIComponent(key))
      .then(function (r) { return r.ok ? r.json() : null; })
      .then(function (data) {
        if (!data || !data.token) {
          warn("could not obtain a form token — submissions will be rejected until this resolves.");
          return;
        }
        var input = form.querySelector('input[name="_xfp_token"]');
        if (!input) {
          input = document.createElement("input");
          input.type = "hidden";
          input.name = "_xfp_token";
          form.appendChild(input);
        }
        input.value = data.token;
      })
      .catch(function () { /* a missing token fails at the server, with a clear message */ });
  }

  function buildRequest(form) {
    // Use multipart (FormData) when the form has file inputs, JSON otherwise.
    var hasFiles = !!form.querySelector('input[type="file"]');
    if (hasFiles) {
      return { method: "POST", headers: { Accept: "application/json" }, body: new FormData(form) };
    }
    var obj = {};
    new FormData(form).forEach(function (value, key) { obj[key] = value; });
    return {
      method: "POST",
      headers: { Accept: "application/json", "Content-Type": "application/json" },
      body: JSON.stringify(obj),
    };
  }

  function enhance(form) {
    if (form.dataset.xofp === "1") return; // already wired
    form.dataset.xofp = "1";

    // A success target referenced by data-success starts hidden until we need it.
    var successId = form.getAttribute("data-success");
    var successBox = successId ? document.getElementById(successId) : null;
    if (successBox) successBox.style.display = "none";

    var base = endpointBase(form);
    var key = accessKeyOf(form);
    var config = null;

    // Ask what this key needs, then set it up before anyone can submit.
    var tokenRequest = null;
    function requestToken() {
      tokenRequest = attachToken(form, base, key);
      return tokenRequest;
    }

    // The token is requested at page load, not on tick: the server rejects tokens younger
    // than three seconds, and someone who ticks and submits immediately would trip that.
    // Issued at load, the age check measures how long the page was open — which is the
    // signal it was meant to measure.
    var ready = (base && key)
      ? fetchConfig(base, key).then(function (cfg) {
          config = cfg;
          if (!cfg) return;
          if (cfg.captcha) injectCaptcha(form, cfg.captcha);
          if (cfg.humanCheck) injectHumanCheck(form);
          if (cfg.requireToken) return requestToken();
        })
      : Promise.resolve();

    form.addEventListener("submit", function (e) {
      e.preventDefault();
      var btn = form.querySelector('[type="submit"]');
      showStatus(form, "Sending…");
      if (btn) btn.disabled = true;

      ready
        .then(function () {
          // Ask for the tick before anything else — it is the cheapest thing to fix.
          if (config && config.humanCheck) {
            var box = form.querySelector('input[name="not_robot"]');
            if (!box || !box.checked) throw new Error("Please confirm you're not a robot.");
          }
          // Don't race the token request that started at page load.
          if (config && config.requireToken) return tokenRequest || requestToken();
        })
        .then(function () {
          // If a captcha is in play, don't post before it has produced an answer.
          if (config && config.captcha) {
            return waitForCaptcha(form, config.captcha.provider).then(function (solved) {
              if (!solved) throw new Error("Please complete the verification and try again.");
            });
          }
        })
        .then(function () {
          return fetch(form.action, buildRequest(form));
        })
        .then(function (res) { return res.json().catch(function () { return {}; }); })
        .then(function (data) {
          if (data && data.success) {
            if (data.redirect) { window.location.href = data.redirect; return; }
            form.reset();
            showStatus(form, "");
            if (successBox) {
              form.style.display = "none";
              successBox.style.display = "";
              successBox.scrollIntoView({ behavior: "smooth", block: "center" });
            } else {
              showStatus(form, data.message || "Thank you!");
            }
            // A token is single-use, so a second submission needs a fresh one, and the box
            // has to be ticked again.
            var box = form.querySelector('input[name="not_robot"]');
            if (box) box.checked = false;
            if (config && config.requireToken) requestToken();
          } else {
            showStatus(form, (data && data.message) || "Something went wrong. Please try again.");
          }
        })
        .catch(function (err) {
          showStatus(form, (err && err.message) || "Network error. Please try again.");
        })
        .then(function () {
          if (btn) btn.disabled = false;
        });
    });
  }

  function init() {
    var forms = document.querySelectorAll('form[data-success], form[action*="/submit"]');
    Array.prototype.forEach.call(forms, enhance);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
