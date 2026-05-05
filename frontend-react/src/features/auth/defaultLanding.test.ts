import { beforeEach, describe, expect, it, vi } from "vitest";
import { getDefaultActivityRoute } from "@/features/auth/defaultLanding";

describe("getDefaultActivityRoute", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("returns the group route by default", () => {
    expect(getDefaultActivityRoute(undefined, "home")).toBe("/g/home");
  });

  it("maps shopping lists correctly", () => {
    expect(getDefaultActivityRoute("shopping_list", "home")).toBe("/shopping-lists");
  });
});
