import { useCallback, useMemo } from "react";
import Alert from "@mui/material/Alert";
import Link from "@mui/material/Link";
import { useLocation } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";

const EXPERIMENTAL_PATH_PATTERNS = [
  "/admin/debug/indexes",
  "/admin/debug/openai",
  "/admin/debug/parser",
  "/g/$groupSlug/r/create/debug",
] as const;

const SESSION_STORAGE_KEY = "mealie-experimental-banner-dismissed";

function normalizePath(path: string): string {
  return path.replace(/\/+$/, "") || "/";
}

export function ExperimentalBanner() {
  const { t } = useTranslation();
  const location = useLocation();

  const isExperimentalRoute = useMemo(() => {
    const currentPath = normalizePath(location.pathname);

    // Replace dynamic segments ($groupSlug, etc.) with a wildcard pattern for matching
    const segments = currentPath.split("/").filter(Boolean);

    return EXPERIMENTAL_PATH_PATTERNS.some((pattern) => {
      const patternSegments = pattern.split("/").filter(Boolean);
      if (segments.length !== patternSegments.length) return false;

      return patternSegments.every(
        (seg, i) => seg.startsWith("$") || seg === segments[i],
      );
    });
  }, [location.pathname]);

  const isDismissed =
    typeof sessionStorage !== "undefined" &&
    sessionStorage.getItem(SESSION_STORAGE_KEY) === "true";

  const handleDismiss = useCallback(() => {
    try {
      sessionStorage.setItem(SESSION_STORAGE_KEY, "true");
    } catch {
      // sessionStorage may be unavailable (private browsing, etc.)
    }
  }, []);

  if (!isExperimentalRoute || isDismissed) {
    return null;
  }

  return (
    <Alert
      severity="info"
      variant="outlined"
      onClose={handleDismiss}
      sx={{
        borderRadius: 0,
        borderLeft: "none",
        borderRight: "none",
        borderTop: "none",
      }}
    >
      <strong>{t("banner-experimental.title")}</strong>
      &nbsp;
      {t("banner-experimental.description")}
      &nbsp;
      <Link
        href="https://github.com/mealie-recipes/mealie"
        target="_blank"
        rel="noopener noreferrer"
        underline="always"
      >
        {t("banner-experimental.issue-link-text")}
      </Link>
    </Alert>
  );
}
