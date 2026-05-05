import { describe, expect, it } from "vitest";
import {
  consumeIntendedDestination,
  readIntendedDestination,
  saveIntendedDestination,
} from "@/features/auth/redirectStore";

describe("redirectStore", () => {
  it("stores and consumes destinations", () => {
    saveIntendedDestination("/g/home");

    expect(readIntendedDestination()).toBe("/g/home");
    expect(consumeIntendedDestination()).toBe("/g/home");
    expect(readIntendedDestination()).toBeNull();
  });
});
