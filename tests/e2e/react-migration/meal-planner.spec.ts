import { expect, test } from "@playwright/test";
import { reactUrl } from "./fixtures";

const reactUser = process.env.PLAYWRIGHT_REACT_USER;
const reactPassword = process.env.PLAYWRIGHT_REACT_PASSWORD;
const reactGroupSlug = process.env.PLAYWRIGHT_REACT_GROUP_SLUG ?? "home";

async function signIn(page: import("@playwright/test").Page) {
  if (!reactUser || !reactPassword) {
    test.skip(true, "Set PLAYWRIGHT_REACT_USER and PLAYWRIGHT_REACT_PASSWORD to run authenticated meal planner flows.");
  }

  await page.goto(reactUrl("/login?direct=1"));
  await page.getByLabel(/email or username/i).fill(reactUser!);
  await page.getByLabel(/^password$/i).fill(reactPassword!);
  await page.getByRole("button", { name: /^login$/i }).click();
}

async function createRecipe(page: import("@playwright/test").Page, recipeName: string) {
  await page.goto(reactUrl(`/g/${reactGroupSlug}/r/create/new`));
  await page.getByLabel(/recipe name/i).fill(recipeName);
  await page.getByRole("button", { name: /create recipe/i }).click();
  await expect(page.getByRole("heading", { name: recipeName })).toBeVisible();
}

test("meal planner routes redirect signed-out users to login", async ({ page }) => {
  await page.goto(reactUrl("/household/mealplan/planner/view"));
  await expect(page).toHaveURL(/\/login/);

  await page.goto(reactUrl("/household/mealplan/settings"));
  await expect(page).toHaveURL(/\/login/);
});

test("authenticated users can manage meal planner routes and entries", async ({ page }) => {
  await signIn(page);

  const recipeName = `Meal Planner Recipe ${Date.now()}`;
  await createRecipe(page, recipeName);

  await page.goto(reactUrl("/household/mealplan/planner/edit"));
  await expect(page.getByRole("heading", { name: /meal planner/i })).toBeVisible();

  await page.getByRole("button", { name: /add meal/i }).first().click();
  await page.getByLabel(/^recipe$/i).fill(recipeName);
  await page.getByRole("option", { name: recipeName }).click();
  await page.getByRole("button", { name: /^save$/i }).click();

  await expect(page.getByText(recipeName)).toBeVisible();

  await page.getByRole("button", { name: /planner view/i }).click();
  await expect(page.getByText(recipeName)).toBeVisible();

  await page.goto(reactUrl("/household/mealplan/planner/edit"));
  await page.getByRole("button", { name: /^edit$/i }).first().click();
  await page.getByLabel(/meal type/i).click();
  await page.getByRole("option", { name: /lunch/i }).click();
  await page.getByRole("button", { name: /^save$/i }).click();

  await expect(page.getByText(/meal plan entry updated/i)).toBeVisible();

  await page.getByRole("button", { name: /move later/i }).first().click();
  await expect(page.getByText(/meal planner updated/i)).toBeVisible();

  await page.getByRole("button", { name: /^delete$/i }).first().click();
  await expect(page.getByText(recipeName)).toHaveCount(0);

  await page.goto(reactUrl("/household/mealplan/settings"));
  await expect(page.getByRole("heading", { name: /meal planning rules/i })).toBeVisible();
});
