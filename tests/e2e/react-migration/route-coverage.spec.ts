import { expect, test } from "@playwright/test";
import { readReleaseStatus, hybridBaseUrl } from "./fixtures";
import { readReleaseGatesContract, readRouteParityContract, routeCoverageSamples } from "./contracts";

test("release contracts keep route coverage at 100 percent for react-default cutover", async () => {
  const releaseGates = readReleaseGatesContract();
  const routeParity = readRouteParityContract();

  expect(releaseGates).toContain("route-coverage:");
  expect(releaseGates).toContain("threshold: 100 percent");
  expect(routeParity).toContain("default_release_variant: react-default");
  expect(routeParity).toContain("- /admin");
  expect(routeParity).not.toContain("legacy_fallback_routes:");
  expect(routeParity).not.toContain("legacy_fallback_paths:");
});

test("the react-default gateway serves every documented route slice example without dead ends", async ({ request }) => {
  let status: Awaited<ReturnType<typeof readReleaseStatus>> | null = null;
  try {
    status = await readReleaseStatus(request);
  }
  catch {
    test.skip(true, "Point PLAYWRIGHT_BASE_URL at the nginx gateway to exercise route-slice headers.");
  }

  test.skip(status?.variant === "legacy-only", "Point PLAYWRIGHT_BASE_URL at the shipped react-default or hybrid gateway.");

  for (const sample of routeCoverageSamples) {
    const response = await request.get(new URL(sample.path, hybridBaseUrl).toString());

    expect(response.ok(), `${sample.path} should resolve from the shipped SPA gateway`).toBeTruthy();
    expect(response.headers()["x-mealie-frontend-instance"], sample.path).toContain("react");
    expect(response.headers()["x-mealie-route-slice"], sample.path).toContain(sample.slice);
  }
});
