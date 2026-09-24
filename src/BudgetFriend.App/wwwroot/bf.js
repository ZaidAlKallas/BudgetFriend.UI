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

  // Sign in with Google (Google Identity Services). Resolves with the Google
  // ID token credential, or null if the flow was dismissed/unavailable.
  const googleSignIn = (clientId) =>
    new Promise((resolve) => {
      if (!clientId) {
        resolve(null);
        return;
      }

      const start = () => {
        google.accounts.id.initialize({
          client_id: clientId,
          callback: (resp) => resolve(resp && resp.credential ? resp.credential : null),
        });
        google.accounts.id.prompt();
      };

      if (window.google && google.accounts) {
        start();
      } else {
        const s = document.createElement("script");
        s.src = "https://accounts.google.com/gsi/client";
        s.async = true;
        s.onload = start;
        s.onerror = () => resolve(null);
        document.head.appendChild(s);
      }
    });

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
    googleSignIn,
    positionPopover,
    initFromAttributes,
  };
})();

document.addEventListener("DOMContentLoaded", () => window.bf.initFromAttributes());