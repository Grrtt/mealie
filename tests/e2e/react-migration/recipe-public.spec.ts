import { expect, test } from "@playwright/test";
import { reactUrl } from "./fixtures";

const sharedGroupSlug = process.env.PLAYWRIGHT_SHARED_GROUP_SLUG;
const sharedRecipeId = process.env.PLAYWRIGHT_SHARED_RECIPE_ID;

test("shared recipe route renders public content and meaningful metadata", async ({ page }) => {
  test.skip(!sharedGroupSlug || !sharedRecipeId, "Set PLAYWRIGHT_SHARED_GROUP_SLUG and PLAYWRIGHT_SHARED_RECIPE_ID.");

  await page.goto(reactUrl(`/g/${sharedGroupSlug}/shared/r/${sharedRecipeId}`));

  const heading = page.getByRole("heading").first();
  await expect(heading).toBeVisible();

  const headingText = await heading.textContent();
  await expect(page).toHaveTitle(new RegExp(headingText?.trim()?.replace(/[.*+?^${}()|[\]\\]/g, "\\$&") ?? "Mealie"));

  const description = page.locator('meta[name="description"]');
  await expect(description).toHaveAttribute("content", /.+/);
});
