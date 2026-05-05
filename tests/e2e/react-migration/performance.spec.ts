import { expect, test } from "@playwright/test";
import { reactUrl } from "./fixtures";

const maxLoginWorkflowMs = Number(process.env.PLAYWRIGHT_MAX_LOGIN_WORKFLOW_MS ?? 3000);
const maxTopWorkflowMs = Number(process.env.PLAYWRIGHT_MAX_TOP_WORKFLOW_MS ?? 4000);

const currentUser = {
  id: "user-1",
  username: "demo",
  fullName: "Demo User",
  groupSlug: "home",
  canManage: true,
  canManageHousehold: true,
  canOrganize: true,
  admin: true,
};

async function measureRoute(
  page: import("@playwright/test").Page,
  path: string,
  ready: () => ReturnType<import("@playwright/test").Page["getByRole"]>,
) {
  const startedAt = Date.now();
  await page.goto(reactUrl(path), { waitUntil: "domcontentloaded" });
  await expect(ready()).toBeVisible();
  const elapsed = Date.now() - startedAt;
  const timing = await page.evaluate(() => JSON.parse(JSON.stringify(performance.getEntriesByType("navigation")[0] ?? {})) as {
    domContentLoadedEventEnd?: number;
  });

  return {
    elapsed,
    domContentLoaded: Number(timing.domContentLoadedEventEnd ?? 0),
  };
}

test("login cold-start stays within the regression budget", async ({ page }) => {
  await page.route("**/api/app/about", async route => {
    await route.fulfill({
      json: {
        allowSignup: true,
        allowPasswordLogin: true,
        enableOidc: false,
      },
    });
  });

  const metrics = await measureRoute(page, "/login?direct=1", () => page.getByRole("button", { name: /login/i }));

  expect(metrics.domContentLoaded).toBeLessThanOrEqual(maxLoginWorkflowMs);
  expect(metrics.elapsed).toBeLessThanOrEqual(maxLoginWorkflowMs);
});

test("group landing and shopping-list entry stay within the top-workflow regression budget", async ({ page }) => {
  await page.addInitScript(() => {
    document.cookie = "mealie.access_token=test-token; path=/; SameSite=Lax";
  });

  await page.route("**/api/users/self", async route => {
    await route.fulfill({ json: currentUser });
  });

  await page.route("**/api/households/shopping/lists?**", async route => {
    await route.fulfill({
      json: {
        items: [
          {
            id: "list-1",
            name: "Weekly shop",
            userId: currentUser.id,
            recipeReferences: [],
          },
        ],
        total_pages: 1,
      },
    });
  });

  const groupMetrics = await measureRoute(page, "/g/home", () => page.getByRole("heading", { name: currentUser.fullName }));
  expect(groupMetrics.domContentLoaded).toBeLessThanOrEqual(maxTopWorkflowMs);
  expect(groupMetrics.elapsed).toBeLessThanOrEqual(maxTopWorkflowMs);

  const shoppingMetrics = await measureRoute(page, "/shopping-lists?disableRedirect=true", () => page.locator("h4", { hasText: /shopping lists/i }));
  expect(shoppingMetrics.domContentLoaded).toBeLessThanOrEqual(maxTopWorkflowMs);
  expect(shoppingMetrics.elapsed).toBeLessThanOrEqual(maxTopWorkflowMs);
});
