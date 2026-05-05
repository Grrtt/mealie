import { expect, test } from "@playwright/test";
import { reactUrl } from "./fixtures";

const reactUser = process.env.PLAYWRIGHT_REACT_USER;
const reactPassword = process.env.PLAYWRIGHT_REACT_PASSWORD;

async function signIn(page: import("@playwright/test").Page) {
  if (!reactUser || !reactPassword) {
    test.skip(true, "Set PLAYWRIGHT_REACT_USER and PLAYWRIGHT_REACT_PASSWORD to run authenticated profile and management flows.");
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
  }) as {
    id: string;
    canManage?: boolean;
    canManageHousehold?: boolean;
    canOrganize?: boolean;
  };
}

test("profile, household, and group routes redirect signed-out users to login or a guarded destination", async ({ page }) => {
  await page.goto(reactUrl("/user/profile"));
  await expect(page).toHaveURL(/\/login/);

  await page.goto(reactUrl("/household"));
  await expect(page).toHaveURL(/\/login/);

  await page.goto(reactUrl("/group/data"));
  await expect(page).toHaveURL(/\/login/);
});

test("authenticated users can open profile and permitted management surfaces", async ({ page }) => {
  await signIn(page);
  const user = await currentUser(page);

  await page.goto(reactUrl("/user/profile"));
  await expect(page.getByRole("heading", { name: /profile/i })).toBeVisible();

  await page.goto(reactUrl("/user/profile/edit"));
  await expect(page.getByRole("heading", { name: /edit profile/i })).toBeVisible();

  await page.goto(reactUrl(`/user/${user.id}/favorites`));
  await expect(page.getByRole("heading", { name: /favorites/i })).toBeVisible();

  await page.goto(reactUrl("/household"));
  if (user.canManageHousehold) {
    await expect(page.getByRole("heading", { name: /household settings/i })).toBeVisible();
  }
  else {
    await expect(page).not.toHaveURL(/\/household$/);
  }

  await page.goto(reactUrl("/household/members"));
  if (user.canManage) {
    await expect(page.getByRole("heading", { name: /household members/i })).toBeVisible();
  }
  else {
    await expect(page).not.toHaveURL(/\/household\/members$/);
  }

  await page.goto(reactUrl("/group/data"));
  if (user.canOrganize || user.canManage) {
    await expect(page.getByRole("heading", { name: /group data/i })).toBeVisible();

    await page.goto(reactUrl("/group/data/categories"));
    await expect(page.getByRole("heading", { name: /categories/i })).toBeVisible();
  }
  else {
    await expect(page).not.toHaveURL(/\/group\/data$/);
  }
});
