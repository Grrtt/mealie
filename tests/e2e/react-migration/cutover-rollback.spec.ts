import { expect, test } from "@playwright/test";
import { hybridBaseUrl, hybridUrl, readReleaseStatus, rollbackBaseUrl, rollbackUrl } from "./fixtures";

const cutoverBaseUrl = process.env.PLAYWRIGHT_CUTOVER_BASE_URL ?? hybridBaseUrl;

test("cutover status reports the active gateway variant and serves approved auth routes from React", async ({ page, request }) => {
  const status = await readReleaseStatus(request, cutoverBaseUrl);
  test.skip(
    !["hybrid", "react-default"].includes(status.variant) || status.auth !== "true",
    "Point PLAYWRIGHT_CUTOVER_BASE_URL at a hybrid or react-default gateway with the auth slice enabled.",
  );

  const response = await page.goto(new URL("/login", cutoverBaseUrl).toString());

  expect(response?.headers()["x-mealie-frontend-instance"]).toContain("react");
  expect(response?.headers()["x-mealie-release-target"]).toContain("react");
});

test("rollback gateways return the same route to the legacy frontend without account changes", async ({ page, request }) => {
  test.skip(!rollbackBaseUrl, "Set PLAYWRIGHT_ROLLBACK_BASE_URL to a legacy-only gateway to exercise rollback.");

  const status = await readReleaseStatus(request, rollbackBaseUrl!);
  expect(status.variant).toBe("legacy-only");

  const response = await page.goto(rollbackUrl("/login"));

  expect(response?.headers()["x-mealie-frontend-instance"]).toContain("legacy");
  expect(response?.headers()["x-mealie-release-target"]).toContain("legacy");
});
