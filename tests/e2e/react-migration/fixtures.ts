import type { APIRequestContext } from "@playwright/test";

export const hybridBaseUrl = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost";
export const reactBaseUrl = process.env.PLAYWRIGHT_REACT_BASE_URL ?? "http://localhost:8080";
export const rollbackBaseUrl = process.env.PLAYWRIGHT_ROLLBACK_BASE_URL ?? null;

export function hybridUrl(path: string) {
  return new URL(path, hybridBaseUrl).toString();
}

export function reactUrl(path: string) {
  return new URL(path, reactBaseUrl).toString();
}

export function rollbackUrl(path: string) {
  if (!rollbackBaseUrl) {
    throw new Error("PLAYWRIGHT_ROLLBACK_BASE_URL is not configured.");
  }

  return new URL(path, rollbackBaseUrl).toString();
}

export type ReleaseStatus = {
  frontend: string;
  variant: string;
  auth: string;
  publicRecipes: string;
  groupHome: string;
  recipeCreateImport: string;
  recipes: string;
  cookbooks: string;
  household: string;
  shoppingLists: string;
  userProfile: string;
  groupData: string;
  admin: string;
};

export async function readReleaseStatus(request: APIRequestContext, baseUrl: string = hybridBaseUrl) {
  const response = await request.get(new URL("/__release-variant", baseUrl).toString());
  if (!response.ok()) {
    throw new Error(`Expected /__release-variant to return 2xx from ${baseUrl}, received ${response.status()}.`);
  }

  return await response.json() as ReleaseStatus;
}
