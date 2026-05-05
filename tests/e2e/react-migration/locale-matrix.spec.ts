import { expect, test } from "@playwright/test";
import { reactUrl } from "./fixtures";
import {
  listDateTimeLocaleCodes,
  listMessageLocaleCodes,
  readReleaseGatesContract,
  rtlLocales,
} from "./contracts";

const localeCodes = listMessageLocaleCodes();

test("locale assets remain aligned with the release gate contract", async () => {
  const releaseGates = readReleaseGatesContract();

  expect(releaseGates).toContain("locale-smoke:");
  expect(releaseGates).toContain("threshold: 100 percent locales; zero blocking RTL defects");
  expect(listDateTimeLocaleCodes()).toEqual(localeCodes);
  expect(localeCodes.length).toBeGreaterThanOrEqual(42);
});

test("locale smoke: login route remains pinned to en-US metadata regardless of stored locale", async ({ page, browserName }) => {
  test.skip(browserName !== "chromium", "Run the locale smoke on chromium to keep the regression suite bounded.");

  await page.addInitScript(selectedLocale => {
    window.localStorage.setItem("i18nextLng", selectedLocale);
    document.cookie = `i18n_redirected=${encodeURIComponent(selectedLocale)}; path=/; SameSite=Lax`;
  }, rtlLocales.has("ar-SA") ? "ar-SA" : localeCodes[0]);

  await page.goto(reactUrl("/login?direct=1"));

  await expect(page.locator("html")).toHaveAttribute("lang", "en-US");
  await expect(page.locator("html")).toHaveAttribute("dir", "ltr");
  await expect(page.getByRole("textbox").first()).toBeVisible();
  await expect(page.getByLabel("Locale")).toHaveCount(0);
});
