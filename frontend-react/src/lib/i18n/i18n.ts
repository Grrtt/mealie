import i18next from "i18next";
import { initReactI18next } from "react-i18next";
import { fallbackLocale, detectLocale, persistLocale } from "@/lib/i18n/persistedLocale";
import { getLocaleDirection, loadMessages, supportedLocales } from "@/lib/i18n/locales";

export async function createI18n() {
  const instance = i18next.createInstance();
  const localeCodes = supportedLocales.map(locale => locale.code);
  const initialLocale = detectLocale(localeCodes);

  const [fallbackMessages, activeMessages] = await Promise.all([
    loadMessages(fallbackLocale),
    initialLocale === fallbackLocale ? Promise.resolve(null) : loadMessages(initialLocale),
  ]);

  await instance
    .use(initReactI18next)
    .init({
      lng: initialLocale,
      fallbackLng: fallbackLocale,
      interpolation: { escapeValue: false },
      resources: {
        [fallbackLocale]: { translation: fallbackMessages },
        ...(activeMessages ? { [initialLocale]: { translation: activeMessages } } : {}),
      },
    } as never);

  const applyLocaleMetadata = (locale: string) => {
    document.documentElement.lang = locale;
    document.documentElement.dir = getLocaleDirection(locale);
    persistLocale(locale);
  };

  applyLocaleMetadata(initialLocale);

  instance.on("languageChanged", async (locale: string) => {
    if (!instance.hasResourceBundle(locale, "translation")) {
      const messages = await loadMessages(locale);
      instance.addResourceBundle(locale, "translation", messages, true, true);
    }

    applyLocaleMetadata(locale);
  });

  return instance;
}
