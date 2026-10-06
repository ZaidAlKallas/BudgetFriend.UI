// BudgetFriend client-side helpers: theme, direction, system-color watching.
window.bf = (() => {
  const root = () => document.documentElement;

  const applyTheme = (isDark) => {
    root().setAttribute("data-theme", isDark ? "dark" : "light");
  };

  const applyLanguage = (languageCode) => {
    const isRtl = (languageCode || "").toLowerCase().startsWith("ar");
    root().setAttribute("dir", isRtl ? "rtl" : "ltr");
    root().setAttribute("lang", languageCode === "ar" ? "ar" : "en");
  };

  let mediaQuery = null;
  let systemHandler = null;

  const watchSystemTheme = (callback) => {
    systemHandler = callback;
    if (!mediaQuery) {
      mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");
      const handler = (e) => {
        if (systemHandler) systemHandler(e.matches);
      };
      if (mediaQuery.addEventListener) mediaQuery.addEventListener("change", handler);
      else if (mediaQuery.addListener) mediaQuery.addListener(handler);
    }
    return mediaQuery.matches;
  };

  const watchSystemThemeForNet = (dotNetRef, methodName) => {
    const isDark = watchSystemTheme((isDark) =>
      dotNetRef.invokeMethodAsync(methodName, isDark)
    );
    dotNetRef.invokeMethodAsync(methodName, isDark);
  };

  const prefersDark = () =>
    window.matchMedia("(prefers-color-scheme: dark)").matches;

  // Session bridge: tokens stay server-side; these helpers manage the opaque
  // httpOnly "bf.session" cookie through same-origin endpoints.
  const sessionGet = async () => {
    try {
      const resp = await fetch("/session-bridge", {
        method: "GET",
        credentials: "same-origin",
        cache: "no-store",
      });
      if (!resp.ok) return null;
      const data = await resp.json();
      return data && data.sessionId ? data.sessionId : null;
    } catch {
      return null;
    }
  };

  const sessionSet = async (sessionId) => {
    if (!sessionId) return;
    try {
      await fetch("/session-bridge", {
        method: "POST",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sessionId }),
      });
    } catch {
      // Non-fatal: the next SetAsync retries.
    }
  };

  const sessionClear = async () => {
    try {
      await fetch("/session-bridge/clear", {
        method: "POST",
        credentials: "same-origin",
      });
    } catch {
      // Non-fatal.
    }
  };

  // ---- Google Identity Services --------------------------------------------
  //
  // The account chooser must come from Google's own button (`renderButton`).
  //
  // One Tap (`google.accounts.id.prompt()`) is deliberately NOT used: it only
  // surfaces as a passive corner prompt, and Google refuses to display it when
  // the call does not originate from a browser user gesture. With Blazor
  // Server the click is replayed over the SignalR circuit, so the gesture is
  // already gone by the time the JS runs and nothing is ever displayed. Worse,
  // when Google declines to show the prompt it never calls `callback`, which
  // left the awaiting .NET call suspended forever and the button stuck disabled.
  //
  // `renderButton` opens a real account-chooser popup and always invokes the
  // `initialize` callback on success, which is the supported flow.
  const gsi = (() => {
    const SCRIPT_SRC = "https://accounts.google.com/gsi/client";
    let loader = null;

    const load = () => {
      if (window.google && window.google.accounts && window.google.accounts.id) {
        return Promise.resolve(window.google.accounts.id);
      }
      if (loader) {
        return loader;
      }

      loader = new Promise((resolve, reject) => {
        const s = document.createElement("script");
        s.src = SCRIPT_SRC;
        s.async = true;
        s.defer = true;
        s.onload = () =>
          window.google && window.google.accounts && window.google.accounts.id
            ? resolve(window.google.accounts.id)
            : reject(new Error("gsi-unavailable"));
        s.onerror = () => reject(new Error("gsi-load-failed"));
        document.head.appendChild(s);
      });

      // Let a later attempt retry after a transient failure (offline, blocked
      // by an extension, ad blocker, ...) instead of replaying the rejection.
      loader.catch(() => {
        loader = null;
      });
      return loader;
    };

    // Renders the official button into `container`. The resulting ID token is
    // pushed back to .NET through `dotNetRef.invokeMethodAsync`.
    // Resolves false when Google could not be loaded, so the caller can report
    // it instead of silently doing nothing.
    const render = async (container, clientId, dotNetRef, options) => {
      if (!container || !clientId || !dotNetRef) {
        return false;
      }

      const id = await load();

      id.initialize({
        client_id: clientId,
        callback: (resp) => {
          const credential = resp && resp.credential ? resp.credential : null;
          // Fire and forget: the circuit may already be gone.
          dotNetRef.invokeMethodAsync("OnGoogleCredentialAsync", credential).catch(() => {});
        },
      });

      container.innerHTML = "";
      id.renderButton(container, {
        type: "standard",
        theme: options && options.dark ? "outline_dark" : "outline",
        size: "large",
        shape: "rectangular",
        text: "continue_with",
        logo_alignment: "left",
        width: Math.max(140, Math.min(container.clientWidth || 320, 400)),
        ...(options && options.locale ? { locale: options.locale } : {}),
      });
      return true;
    };

    const cancel = () => {
      if (window.google && window.google.accounts && window.google.accounts.id) {
        window.google.accounts.id.cancel();
      }
    };

    return { render, cancel };
  })();

  const positionPopover = (trigger, popover) => {
    if (!trigger || !popover) return;

    const margin = 8;
    const vw = window.innerWidth;
    const vh = window.innerHeight;

    // Teleport the popover to <body> so no ancestor (modal/form) can clip it.
    if (popover.parentElement !== document.body) {
      document.body.appendChild(popover);
    }

    const trig = trigger.getBoundingClientRect();
    const popW = popover.offsetWidth;
    const popH = popover.offsetHeight;

    let left = trig.left;
    if (left + popW + margin > vw - margin) left = vw - popW - margin;
    left = Math.max(margin, left);

    let top = trig.top - popH - 6;
    if (top < margin) top = trig.bottom + 6;
    if (top + popH > vh - margin) top = Math.max(margin, vh - popH - margin);

    popover.style.position = "fixed";
    popover.style.left = `${left}px`;
    popover.style.top = `${top}px`;
    popover.style.bottom = "auto";
    popover.style.maxHeight = `${vh - 2 * margin}px`;
    popover.style.overflowY = "auto";
  };

  const initFromAttributes = () => {
    if (!root().hasAttribute("data-theme")) {
      const theme = root().getAttribute("data-bf-theme");
      if (theme === "dark" || theme === "light") applyTheme(theme === "dark");
    }
    const lang = root().getAttribute("data-bf-lang");
    if (lang) applyLanguage(lang);
  };

  return {
    applyTheme,
    applyLanguage,
    watchSystemTheme,
    watchSystemThemeForNet,
    prefersDark,
    sessionGet,
    sessionSet, 
    sessionClear,
    google: gsi,
    positionPopover,
    initFromAttributes,
  };
})();

document.addEventListener("DOMContentLoaded", () => window.bf.initFromAttributes());