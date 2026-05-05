import { loadDateTimeFormats } from "@/lib/i18n/locales";

const formatCache = new Map<string, Intl.DateTimeFormatOptions>();

export async function getDateTimeFormat(locale: string, key = "short") {
  const cacheKey = `${locale}:${key}`;

  if (!formatCache.has(cacheKey)) {
    const formats = (await loadDateTimeFormats(locale)) as Record<string, Intl.DateTimeFormatOptions>;
    formatCache.set(cacheKey, formats[key] ?? {});
  }

  return formatCache.get(cacheKey) ?? {};
}

export async function formatDateTime(value: string | number | Date, locale: string, key = "short") {
  const options = await getDateTimeFormat(locale, key);
  return new Intl.DateTimeFormat(locale, options).format(new Date(value));
}
