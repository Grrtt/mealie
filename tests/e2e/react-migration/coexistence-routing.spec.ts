import { expect, test } from "@playwright/test";
import { hybridUrl, readReleaseStatus } from "./fixtures";

test("approved auth routes can stay on React while coexistence is enabled", async ({ page, request }) => {
  const status = await readReleaseStatus(request);
  test.skip(
    status.variant === "legacy-only" || status.auth !== "true",
    "Enable the hybrid or react-default gateway variant with the auth route slice to verify coexistence routing.",
  );

  const response = await page.goto(hybridUrl("/login"));

  expect(response?.headers()["x-mealie-frontend-instance"]).toContain("react");
  expect(response?.headers()["x-mealie-route-slice"]).toContain("auth");
  await expect(page).toHaveURL(/\/login$/);
});

test("legacy-only gaps stay on the working legacy fallback", async ({ page, request }) => {
  const response = await request.get(hybridUrl("/admin"));

  expect(response.ok()).toBeTruthy();
  expect(response.headers()["x-mealie-release-target"]).toContain("legacy");
  expect(response.headers()["x-mealie-route-slice"]).toContain("legacy-fallback");

  await page.goto(hybridUrl("/admin"));
  await expect(page).not.toHaveURL(/:8080/);
});
