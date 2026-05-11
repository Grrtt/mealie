export const reactBaseUrl = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost";

export function reactUrl(path: string) {
  return new URL(path, reactBaseUrl).toString();
}
