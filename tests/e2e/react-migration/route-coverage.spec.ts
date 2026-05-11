import { expect, test } from "@playwright/test";
import { reactUrl } from "./fixtures";
import { routeCoverageSamples } from "./contracts";

test("route coverage samples still exercise core React entry points", async () => {
  expect(routeCoverageSamples.some(sample => sample.path === "/login")).toBeTruthy();
  expect(routeCoverageSamples.some(sample => sample.path === "/admin")).toBeTruthy();
});

test("the React frontend serves every documented route sample without dead ends", async ({ request }) => {
  for (const sample of routeCoverageSamples) {
    const response = await request.get(reactUrl(sample.path));

    expect(response.ok(), `${sample.path} should resolve from the shipped React SPA`).toBeTruthy();
  }
});
