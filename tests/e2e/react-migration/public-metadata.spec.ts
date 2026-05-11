import { expect, test } from "@playwright/test";
import { reactUrl } from "./fixtures";

const demoGroupSlug = "demo-group";
const demoTokenId = "demo-token";
const demoRecipeSlug = "demo-soup";
const demoRecipeName = "Demo Soup";
const demoRecipeDescription = "Bright lemon broth with herbs and tender vegetables.";

test("shared recipe routes publish meaningful title and social metadata", async ({ page }) => {
  await page.route(`**/api/recipes/shared/${demoTokenId}`, async route => {
    await route.fulfill({
      json: {
        id: demoTokenId,
        recipeId: "recipe-1",
        groupId: "group-1",
        createdAt: "2026-05-03T00:00:00Z",
      },
    });
  });

  await page.route(`**/api/explore/groups/${demoGroupSlug}/recipes?**`, async route => {
    await route.fulfill({
      json: {
        items: [
          {
            id: "recipe-1",
            slug: demoRecipeSlug,
            name: demoRecipeName,
          },
        ],
        total_pages: 1,
      },
    });
  });

  await page.route(`**/api/explore/groups/${demoGroupSlug}/recipes/${demoRecipeSlug}`, async route => {
    await route.fulfill({
      json: {
        id: "recipe-1",
        slug: demoRecipeSlug,
        name: demoRecipeName,
        description: demoRecipeDescription,
        recipeCategory: [],
        tags: [],
        recipeIngredient: [{ originalText: "1 lemon" }],
        recipeInstructions: [{ id: "step-1", text: "Simmer gently." }],
      },
    });
  });

  await page.goto(reactUrl(`/g/${demoGroupSlug}/shared/r/${demoTokenId}`));

  await expect(page.getByRole("heading", { name: demoRecipeName })).toBeVisible();
  await expect(page).toHaveTitle(new RegExp(demoRecipeName));
  await expect(page.locator('meta[name="description"]')).toHaveAttribute("content", demoRecipeDescription);
  await expect(page.locator('meta[property="og:title"]')).toHaveAttribute("content", new RegExp(demoRecipeName));
  await expect(page.locator('meta[property="og:description"]')).toHaveAttribute("content", demoRecipeDescription);
  await expect(page.locator('meta[property="twitter:title"]')).toHaveAttribute("content", new RegExp(demoRecipeName));
  await expect(page.locator('meta[property="twitter:description"]')).toHaveAttribute("content", demoRecipeDescription);
});

test("direct entry auth routes still expose a non-empty bookmark title", async ({ page }) => {
  await page.route("**/api/app/about", async route => {
    await route.fulfill({
      json: {
        allowSignup: true,
        allowPasswordLogin: true,
        enableOidc: false,
      },
    });
  });

  await page.goto(reactUrl("/login?direct=1"));

  await expect(page).toHaveTitle(/login/i);
});
