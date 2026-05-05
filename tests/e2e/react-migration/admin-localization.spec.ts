import { expect, test } from "@playwright/test";
import { reactUrl } from "./fixtures";

const reactUser = process.env.PLAYWRIGHT_REACT_USER;
const reactPassword = process.env.PLAYWRIGHT_REACT_PASSWORD;

async function signIn(page: import("@playwright/test").Page) {
  if (!reactUser || !reactPassword) {
    test.skip(true, "Set PLAYWRIGHT_REACT_USER and PLAYWRIGHT_REACT_PASSWORD to run authenticated admin and localization flows.");
  }

  await page.goto(reactUrl("/login?direct=1"));
  await page.getByLabel(/email or username/i).fill(reactUser!);
  await page.getByLabel(/^password$/i).fill(reactPassword!);
  await page.getByRole("button", { name: /^login$/i }).click();
}

async function currentUser(page: import("@playwright/test").Page) {
  return await page.evaluate(async () => {
    const response = await fetch("/api/users/self", { credentials: "include" });
    return await response.json();
  }) as { admin?: boolean };
}

test("admin routes redirect signed-out users to login", async ({ page }) => {
  await page.goto(reactUrl("/admin/site-settings"));
  await expect(page).toHaveURL(/\/login/);
});

test("authenticated users can switch locales and verify RTL metadata", async ({ page }) => {
  await signIn(page);

  await page.goto(reactUrl("/user/profile"));
  await page.getByLabel(/locale/i).selectOption("ar-SA");
  await expect(page.locator("html")).toHaveAttribute("dir", "rtl");

  await page.getByLabel(/locale/i).selectOption("en-US");
  await expect(page.locator("html")).toHaveAttribute("dir", "ltr");
});

test("admin users can open admin management and settings surfaces", async ({ page }) => {
  await signIn(page);
  const user = await currentUser(page);
  test.skip(!user.admin, "The configured Playwright user is not an admin.");

  await page.goto(reactUrl("/admin/site-settings"));
  await expect(page.getByRole("heading", { name: /admin site settings/i })).toBeVisible();

  await page.goto(reactUrl("/admin/manage/users"));
  await expect(page.getByRole("heading", { name: /manage users/i })).toBeVisible();
});
