import { expect, test } from "@playwright/test";
import { reactUrl } from "./fixtures";

test("protected routes redirect to login", async ({ page }) => {
  await page.goto(reactUrl("/g/home"));

  await expect(page).toHaveURL(/\/login/);
});
