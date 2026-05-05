import { StrictMode, useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
import { QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { CacheProvider } from "@emotion/react";
import { ThemeProvider } from "@mui/material/styles";
import { I18nextProvider } from "react-i18next";
import { router } from "@/router";
import { queryClient } from "@/lib/query/queryClient";
import { createI18n } from "@/lib/i18n/i18n";
import { getLocaleDirection } from "@/lib/i18n/locales";
import { createDirectionalCache } from "@/theme/rtlCache";
import { createAppTheme } from "@/theme";
import { applyReleaseVariantMetadata, resolveLegacyFallbackUrl, shouldServeFromLegacy } from "@/config/releaseVariant";
import { hydrateSession } from "@/features/auth/session";

function AppProviders({ i18n }: { i18n: Awaited<ReturnType<typeof createI18n>> }) {
  const [locale, setLocale] = useState(i18n.language);

  useEffect(() => {
    const handleLanguageChange = (language: string) => setLocale(language);
    i18n.on("languageChanged", handleLanguageChange);
    return () => {
      i18n.off("languageChanged", handleLanguageChange);
    };
  }, [i18n]);

  const direction = getLocaleDirection(locale);
  const cache = useMemo(() => createDirectionalCache(direction), [direction]);
  const theme = useMemo(() => createAppTheme(direction), [direction]);

  return (
    <StrictMode>
      <CacheProvider value={cache}>
        <ThemeProvider theme={theme}>
          <I18nextProvider i18n={i18n}>
            <QueryClientProvider client={queryClient}>
              <RouterProvider router={router} />
            </QueryClientProvider>
          </I18nextProvider>
        </ThemeProvider>
      </CacheProvider>
    </StrictMode>
  );
}

async function bootstrap() {
  const rootElement = document.getElementById("root");
  if (!rootElement) {
    throw new Error("Missing #root element");
  }

  applyReleaseVariantMetadata();

  const currentPath = `${window.location.pathname}${window.location.search}${window.location.hash}`;
  if (shouldServeFromLegacy(currentPath)) {
    const fallbackUrl = resolveLegacyFallbackUrl(currentPath);
    if (fallbackUrl !== window.location.href) {
      window.location.replace(fallbackUrl);
      return;
    }
  }

  const i18n = await createI18n();
  await hydrateSession();

  createRoot(rootElement).render(<AppProviders i18n={i18n} />);
}

void bootstrap();
