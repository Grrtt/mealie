function formatPart(value: number, singular: string, plural = singular) {
  return `${value} ${value === 1 ? singular : plural}`;
}

export function formatRecipeDuration(value?: string | null) {
  if (!value) return null;

  const trimmed = value.trim();
  const match = /^P(?:(\d+)D)?(?:T(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?)?$/i.exec(trimmed);

  if (!match) {
    return trimmed;
  }

  const [, daysText, hoursText, minutesText, secondsText] = match;
  const days = Number(daysText ?? 0);
  const hours = Number(hoursText ?? 0);
  const minutes = Number(minutesText ?? 0);
  const seconds = Number(secondsText ?? 0);

  const parts = [
    days > 0 ? formatPart(days, "day", "days") : null,
    hours > 0 ? formatPart(hours, "hr", "hr") : null,
    minutes > 0 ? formatPart(minutes, "min", "min") : null,
    seconds > 0 ? formatPart(seconds, "sec", "sec") : null,
  ].filter(Boolean);

  return parts.length > 0 ? parts.join(" ") : trimmed;
}
