import { describe, expect, it } from "vitest";
import { formatRecipeDuration } from "@/features/recipes/format-duration";

describe("formatRecipeDuration", () => {
  it("keeps already human-readable values unchanged", () => {
    expect(formatRecipeDuration("20 mins")).toBe("20 mins");
  });

  it("formats minute-only iso durations", () => {
    expect(formatRecipeDuration("PT15M")).toBe("15 min");
  });

  it("formats multi-part iso durations", () => {
    expect(formatRecipeDuration("PT1H30M")).toBe("1 hr 30 min");
    expect(formatRecipeDuration("P1DT2H")).toBe("1 day 2 hr");
  });

  it("returns nullish values as null", () => {
    expect(formatRecipeDuration(null)).toBeNull();
    expect(formatRecipeDuration(undefined)).toBeNull();
  });
});
