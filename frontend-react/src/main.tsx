import { StrictMode, useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
import { QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { CacheProvider } from "@emotion/react";
import CssBaseline from "@mui/material/CssBaseline";
import { ThemeProvider } from "@mui/material/styles";
import { I18nextProvider } from "react-i18next";
import { router } from "@/router";
import { queryClient } from "@/lib/query/queryClient";
import { createI18n } from "@/lib/i18n/i18n";
import { getLocaleDirection } from "@/lib/i18n/locales";
import { createDirectionalCache } from "@/theme/rtlCache";
import { createAppTheme } from "@/theme";
import { ThemePreferenceProvider, useThemePreference } from "@/theme/themePreference";
import { hydrateSession } from "@/features/auth/session";

function ThemedProviders({ i18n }: { i18n: Awaited<ReturnType<typeof createI18n>> }) {
  const [locale, setLocale] = useState(i18n.language);
  const { resolvedMode } = useThemePreference();

  useEffect(() => {
    const handleLanguageChange = (language: string) => setLocale(language);
    i18n.on("languageChanged", handleLanguageChange);
    return () => {
      i18n.off("languageChanged", handleLanguageChange);
    };
  }, [i18n]);

  const direction = getLocaleDirection(locale);
  const cache = useMemo(() => createDirectionalCache(direction), [direction]);
  const theme = useMemo(() => createAppTheme(direction, resolvedMode), [direction, resolvedMode]);

  return (
    <CacheProvider value={cache}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <I18nextProvider i18n={i18n}>
          <QueryClientProvider client={queryClient}>
            <RouterProvider router={router} />
          </QueryClientProvider>
        </I18nextProvider>
      </ThemeProvider>
    </CacheProvider>
  );
}

function AppProviders({ i18n }: { i18n: Awaited<ReturnType<typeof createI18n>> }) {
  return (
    <StrictMode>
      <ThemePreferenceProvider>
        <ThemedProviders i18n={i18n} />
      </ThemePreferenceProvider>
    </StrictMode>
  );
}

function normalizeBasePath() {
  return import.meta.env.BASE_URL === "/" ? "" : import.meta.env.BASE_URL.replace(/\/$/, "");
}

function stripBasePath(pathname: string, basePath: string) {
  if (!basePath) {
    return pathname || "/";
  }

  if (pathname === basePath) {
    return "/";
  }

  if (!pathname.startsWith(`${basePath}/`)) {
    return null;
  }

  return pathname.slice(basePath.length) || "/";
}

function installInternalNavigationInterceptor() {
  if (typeof window === "undefined") {
    return;
  }

  const basePath = normalizeBasePath();

  const clickHandler = (event: MouseEvent) => {
    if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
      return;
    }

    const target = event.target;
    if (!(target instanceof Element)) {
      return;
    }

    const anchor = target.closest("a");
    if (!(anchor instanceof HTMLAnchorElement)) {
      return;
    }

    if (anchor.target && anchor.target !== "_self") {
      return;
    }

    if (anchor.hasAttribute("download") || anchor.dataset.routerIgnore === "true") {
      return;
    }

    const rawHref = anchor.getAttribute("href");
    if (!rawHref || rawHref.startsWith("#")) {
      return;
    }

    const url = new URL(anchor.href, window.location.href);
    if (url.origin !== window.location.origin) {
      return;
    }

    const appPathname = stripBasePath(url.pathname, basePath);
    if (!appPathname) {
      return;
    }

    if (["/api", "/docs", "/healthz", "/swagger", "/mcp"].some(prefix => appPathname === prefix || appPathname.startsWith(`${prefix}/`))) {
      return;
    }

    event.preventDefault();
    void router.navigate({
      href: `${appPathname}${url.search}${url.hash}`,
    });
  };

  window.document.addEventListener("click", clickHandler);
}

async function clearExistingServiceWorkers() {
  if (typeof window === "undefined" || !("serviceWorker" in navigator)) {
    return false;
  }

  const registrations = await navigator.serviceWorker.getRegistrations();
  if (registrations.length === 0) {
    return false;
  }

  await Promise.all(registrations.map(async registration => {
    await registration.unregister();
  }));

  if ("caches" in window) {
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys.map(async cacheKey => {
      await caches.delete(cacheKey);
    }));
  }

  return navigator.serviceWorker.controller != null;
}

async function bootstrap() {
  const rootElement = document.getElementById("root");
  if (!rootElement) {
    throw new Error("Missing #root element");
  }

  if (await clearExistingServiceWorkers()) {
    window.location.reload();
    return;
  }

  installInternalNavigationInterceptor();

  const i18n = await createI18n();
  await hydrateSession();

  createRoot(rootElement).render(<AppProviders i18n={i18n} />);
}

void bootstrap();
