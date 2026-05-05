const LOCALE_STORAGE_KEY = "i18nextLng";
const LOCALE_COOKIE_KEY = "i18n_redirected";
const FALLBACK_LOCALE = "en-US";

export function persistLocale(locale: string) {
  if (typeof window === "undefined") return;

  void locale;
  window.localStorage.setItem(LOCALE_STORAGE_KEY, FALLBACK_LOCALE);
  document.cookie = `${LOCALE_COOKIE_KEY}=${encodeURIComponent(FALLBACK_LOCALE)}; path=/; SameSite=Lax`;
}

export function getPersistedLocale() {
  return FALLBACK_LOCALE;
}

export function detectLocale(supportedLocales: string[]) {
  void supportedLocales;
  return FALLBACK_LOCALE;
}

export const fallbackLocale = FALLBACK_LOCALE;
export const localeStorageKey = LOCALE_STORAGE_KEY;
