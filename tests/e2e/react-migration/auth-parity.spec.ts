import { expect, test } from "@playwright/test";
import { reactUrl } from "./fixtures";

test("login page renders the supported entry points", async ({ page }) => {
  await page.goto(reactUrl("/login"));

  await expect(page.getByText("Mealie")).toBeVisible();
  await expect(page.getByRole("button", { name: /login/i })).toBeVisible();
  await expect(page.getByRole("link", { name: /register/i })).toBeVisible();
  await expect(page.getByRole("link", { name: /reset password/i })).toBeVisible();
});
