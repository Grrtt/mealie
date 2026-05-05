export type SupportedLocale = {
  code: string;
  dir: "ltr" | "rtl";
};

export const supportedLocales: SupportedLocale[] = [
  { code: "af-ZA", dir: "ltr" },
  { code: "ar-SA", dir: "rtl" },
  { code: "bg-BG", dir: "ltr" },
  { code: "ca-ES", dir: "ltr" },
  { code: "cs-CZ", dir: "ltr" },
  { code: "da-DK", dir: "ltr" },
  { code: "de-DE", dir: "ltr" },
  { code: "el-GR", dir: "ltr" },
  { code: "en-GB", dir: "ltr" },
  { code: "en-US", dir: "ltr" },
  { code: "es-ES", dir: "ltr" },
  { code: "et-EE", dir: "ltr" },
  { code: "fi-FI", dir: "ltr" },
  { code: "fr-BE", dir: "ltr" },
  { code: "fr-CA", dir: "ltr" },
  { code: "fr-FR", dir: "ltr" },
  { code: "gl-ES", dir: "ltr" },
  { code: "he-IL", dir: "rtl" },
  { code: "hr-HR", dir: "ltr" },
  { code: "hu-HU", dir: "ltr" },
  { code: "is-IS", dir: "ltr" },
  { code: "it-IT", dir: "ltr" },
  { code: "ja-JP", dir: "ltr" },
  { code: "ko-KR", dir: "ltr" },
  { code: "lt-LT", dir: "ltr" },
  { code: "lv-LV", dir: "ltr" },
  { code: "nl-NL", dir: "ltr" },
  { code: "no-NO", dir: "ltr" },
  { code: "pl-PL", dir: "ltr" },
  { code: "pt-BR", dir: "ltr" },
  { code: "pt-PT", dir: "ltr" },
  { code: "ro-RO", dir: "ltr" },
  { code: "ru-RU", dir: "ltr" },
  { code: "sk-SK", dir: "ltr" },
  { code: "sl-SI", dir: "ltr" },
  { code: "sr-SP", dir: "ltr" },
  { code: "sv-SE", dir: "ltr" },
  { code: "tr-TR", dir: "ltr" },
  { code: "uk-UA", dir: "ltr" },
  { code: "vi-VN", dir: "ltr" },
  { code: "zh-CN", dir: "ltr" },
  { code: "zh-TW", dir: "ltr" },
];

const messageModules = import.meta.glob("../../../../frontend/app/lang/messages/*.json", {
  import: "default",
});

const dateTimeModules = import.meta.glob("../../../../frontend/app/lang/dateTimeFormats/*.json", {
  import: "default",
});

function resolveLocaleModule<T>(modules: Record<string, () => Promise<T>>, locale: string) {
  const suffix = `/${locale}.json`;
  const match = Object.entries(modules).find(([modulePath]) => modulePath.endsWith(suffix));
  return match?.[1];
}

export function getLocaleDirection(locale: string) {
  return supportedLocales.find(entry => entry.code === locale)?.dir ?? "ltr";
}

export async function loadMessages(locale: string) {
  const loader = resolveLocaleModule(messageModules, locale);
  if (!loader) {
    throw new Error(`Missing translation bundle for ${locale}`);
  }

  return await loader();
}

export async function loadDateTimeFormats(locale: string) {
  const loader = resolveLocaleModule(dateTimeModules, locale);
  if (!loader) {
    throw new Error(`Missing date-time bundle for ${locale}`);
  }

  return await loader();
}
