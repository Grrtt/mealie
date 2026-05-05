import { expect, test } from "@playwright/test";
import { reactUrl } from "./fixtures";

const reactUser = process.env.PLAYWRIGHT_REACT_USER;
const reactPassword = process.env.PLAYWRIGHT_REACT_PASSWORD;

async function signIn(page: import("@playwright/test").Page) {
  if (!reactUser || !reactPassword) {
    test.skip(true, "Set PLAYWRIGHT_REACT_USER and PLAYWRIGHT_REACT_PASSWORD to run authenticated shopping list flows.");
  }

  await page.goto(reactUrl("/login?direct=1"));
  await page.getByLabel(/email or username/i).fill(reactUser!);
  await page.getByLabel(/^password$/i).fill(reactPassword!);
  await page.getByRole("button", { name: /^login$/i }).click();
}

test("shopping list routes redirect signed-out users to login", async ({ page }) => {
  await page.goto(reactUrl("/shopping-lists"));
  await expect(page).toHaveURL(/\/login/);

  await page.goto(reactUrl("/shopping-lists/example"));
  await expect(page).toHaveURL(/\/login/);
});

test("authenticated users can create and update shopping lists", async ({ page }) => {
  await signIn(page);

  const listName = `React Shopping ${Date.now()}`;
  const itemName = `Flour ${Date.now()}`;

  await page.goto(reactUrl("/shopping-lists?disableRedirect=true"));
  await expect(page.getByRole("heading", { name: /shopping lists/i })).toBeVisible();

  await page.getByLabel(/new shopping list/i).fill(listName);
  await page.getByRole("button", { name: /create list/i }).click();
  await expect(page.getByRole("heading", { name: listName })).toBeVisible();

  await page.getByLabel(/^item$/i).fill(itemName);
  await page.getByRole("button", { name: /add item/i }).click();
  await expect(page.getByText(itemName)).toBeVisible();

  await page.getByRole("button", { name: /^edit$/i }).first().click();
  await page.getByLabel(/^note$/i).fill("Weekly restock");
  await page.getByRole("button", { name: /^save$/i }).click();
  await expect(page.getByText(/weekly restock/i)).toBeVisible();

  await page.locator('input[type="checkbox"]').first().check();
  await expect(page.getByRole("heading", { name: /checked items/i })).toBeVisible();

  await page.getByRole("button", { name: /delete checked/i }).click();
  await expect(page.getByText(itemName)).toHaveCount(0);

  await page.goto(page.url());
  await expect(page.getByRole("heading", { name: listName })).toBeVisible();
});
