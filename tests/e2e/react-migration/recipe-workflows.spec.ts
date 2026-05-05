import { expect, test } from "@playwright/test";
import { reactUrl } from "./fixtures";

const reactUser = process.env.PLAYWRIGHT_REACT_USER;
const reactPassword = process.env.PLAYWRIGHT_REACT_PASSWORD;
const reactGroupSlug = process.env.PLAYWRIGHT_REACT_GROUP_SLUG ?? "home";
const importUrl = process.env.PLAYWRIGHT_RECIPE_IMPORT_URL;

async function signIn(page: import("@playwright/test").Page) {
  if (!reactUser || !reactPassword) {
    test.skip(true, "Set PLAYWRIGHT_REACT_USER and PLAYWRIGHT_REACT_PASSWORD to run authenticated recipe flows.");
  }

  await page.goto(reactUrl("/login?direct=1"));
  await page.getByLabel(/email or username/i).fill(reactUser!);
  await page.getByLabel(/^password$/i).fill(reactPassword!);
  await page.getByRole("button", { name: /^login$/i }).click();
}

test("recipe routes redirect signed-out users to login", async ({ page }) => {
  await page.goto(reactUrl(`/g/${reactGroupSlug}/recipes/categories`));
  await expect(page).toHaveURL(/\/login/);

  await page.goto(reactUrl(`/g/${reactGroupSlug}/r/example-recipe`));
  await expect(page).toHaveURL(/\/login/);

  await page.goto(reactUrl(`/g/${reactGroupSlug}/r/create/url?recipe_import_url=https%3A%2F%2Fexample.com`));
  await expect(page).toHaveURL(/\/login/);
});

test("authenticated users can create and browse recipe pages", async ({ page }) => {
  await signIn(page);

  const recipeName = `React Recipe ${Date.now()}`;

  await page.goto(reactUrl(`/g/${reactGroupSlug}/r/create/new`));
  await page.getByLabel(/recipe name/i).fill(recipeName);
  await page.getByRole("button", { name: /create recipe/i }).click();
  await expect(page.getByRole("heading", { name: recipeName })).toBeVisible();

  await page.goto(reactUrl(`/g/${reactGroupSlug}/recipes/categories`));
  await expect(page.getByRole("heading", { name: /recipe categories/i })).toBeVisible();

  await page.goto(reactUrl(`/g/${reactGroupSlug}/recipes/finder`));
  await expect(page.getByRole("heading", { name: /recipe finder/i })).toBeVisible();

  await page.goto(reactUrl(`/g/${reactGroupSlug}/recipes/timeline`));
  await expect(page.getByRole("heading", { name: /recipe timeline/i })).toBeVisible();
});

test("authenticated users can import a recipe by URL when a source URL is provided", async ({ page }) => {
  test.skip(!importUrl, "Set PLAYWRIGHT_RECIPE_IMPORT_URL to validate the import workflow.");

  await signIn(page);
  await page.goto(reactUrl(`/g/${reactGroupSlug}/r/create/url`));
  await page.getByLabel(/recipe url/i).fill(importUrl!);
  await page.getByRole("button", { name: /import recipe/i }).click();

  await expect(page).toHaveURL(new RegExp(`/g/${reactGroupSlug}/r/`));
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
});
