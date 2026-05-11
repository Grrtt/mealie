import { createContext, useContext, useEffect, useMemo, useState } from "react";

export type ThemePreference = "system" | "light" | "dark";
export type ResolvedThemeMode = Exclude<ThemePreference, "system">;

const STORAGE_KEY = "mealie-theme-preference";
const DARK_MEDIA_QUERY = "(prefers-color-scheme: dark)";

type ThemePreferenceContextValue = {
  preference: ThemePreference;
  resolvedMode: ResolvedThemeMode;
  setPreference: (preference: ThemePreference) => void;
};

const ThemePreferenceContext = createContext<ThemePreferenceContextValue | null>(null);

function isThemePreference(value: string | null): value is ThemePreference {
  return value === "system" || value === "light" || value === "dark";
}

function readStoredPreference(): ThemePreference {
  if (typeof window === "undefined") {
    return "system";
  }

  const stored = window.localStorage.getItem(STORAGE_KEY);
  return isThemePreference(stored) ? stored : "system";
}

function detectSystemMode(): ResolvedThemeMode {
  if (typeof window === "undefined" || !("matchMedia" in window)) {
    return "light";
  }

  return window.matchMedia(DARK_MEDIA_QUERY).matches ? "dark" : "light";
}

export function ThemePreferenceProvider({ children }: { children: React.ReactNode }) {
  const [preference, setPreference] = useState<ThemePreference>(() => readStoredPreference());
  const [systemMode, setSystemMode] = useState<ResolvedThemeMode>(() => detectSystemMode());

  useEffect(() => {
    if (typeof window === "undefined" || !("matchMedia" in window)) {
      return;
    }

    const mediaQuery = window.matchMedia(DARK_MEDIA_QUERY);
    const updateMode = () => setSystemMode(mediaQuery.matches ? "dark" : "light");

    updateMode();
    mediaQuery.addEventListener("change", updateMode);
    return () => mediaQuery.removeEventListener("change", updateMode);
  }, []);

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    window.localStorage.setItem(STORAGE_KEY, preference);
  }, [preference]);

  const resolvedMode = preference === "system" ? systemMode : preference;

  useEffect(() => {
    document.documentElement.dataset.themePreference = preference;
    document.documentElement.style.colorScheme = resolvedMode;
  }, [preference, resolvedMode]);

  const value = useMemo<ThemePreferenceContextValue>(() => ({
    preference,
    resolvedMode,
    setPreference,
  }), [preference, resolvedMode]);

  return (
    <ThemePreferenceContext.Provider value={value}>
      {children}
    </ThemePreferenceContext.Provider>
  );
}

export function useThemePreference() {
  const value = useContext(ThemePreferenceContext);
  if (!value) {
    throw new Error("useThemePreference must be used within ThemePreferenceProvider");
  }

  return value;
}
