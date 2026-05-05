import { getLegacyFallbackRule, getRouteSliceForPath, resolveLegacyFallbackPath, routeSliceDefinitions, type RouteSliceId } from "@/router/fallbackRoutes";

export type ReleaseVariant = "legacy-only" | "hybrid" | "react-default";

const defaultEnabledSlices: Record<RouteSliceId, boolean> = {
  auth: true,
  "public-recipes": true,
  "group-home": true,
  "recipe-create-import": true,
  recipes: true,
  cookbooks: true,
  household: true,
  "shopping-lists": true,
  "user-profile": true,
  "group-data": true,
  admin: true,
};

const routeSliceFlags: Record<RouteSliceId, string | undefined> = {
  auth: import.meta.env.VITE_ROUTE_SLICE_AUTH,
  "public-recipes": import.meta.env.VITE_ROUTE_SLICE_PUBLIC_RECIPES,
  "group-home": import.meta.env.VITE_ROUTE_SLICE_GROUP_HOME,
  "recipe-create-import": import.meta.env.VITE_ROUTE_SLICE_RECIPE_CREATE_IMPORT,
  recipes: import.meta.env.VITE_ROUTE_SLICE_RECIPES,
  cookbooks: import.meta.env.VITE_ROUTE_SLICE_COOKBOOKS,
  household: import.meta.env.VITE_ROUTE_SLICE_HOUSEHOLD,
  "shopping-lists": import.meta.env.VITE_ROUTE_SLICE_SHOPPING_LISTS,
  "user-profile": import.meta.env.VITE_ROUTE_SLICE_USER_PROFILE,
  "group-data": import.meta.env.VITE_ROUTE_SLICE_GROUP_DATA,
  admin: import.meta.env.VITE_ROUTE_SLICE_ADMIN,
};

function normalizeBasePath() {
  if (import.meta.env.BASE_URL === "/") {
    return "";
  }

  return import.meta.env.BASE_URL.replace(/\/$/, "");
}

function parseBoolean(value: string | undefined, fallback: boolean) {
  if (value == null || value.trim() === "") {
    return fallback;
  }

  return ["1", "true", "yes", "on"].includes(value.trim().toLowerCase());
}

function parseReleaseVariant(value: string | undefined): ReleaseVariant {
  switch (value) {
    case "legacy-only":
    case "hybrid":
    case "react-default":
      return value;
    default:
      return "react-default";
  }
}

function normalizeLocationPath(path: string) {
  const url = new URL(path, "http://localhost");
  const pathname = url.pathname.replace(/\/+$/, "") || "/";
  return {
    pathname,
    search: url.search,
    hash: url.hash,
  };
}

function stripBasePath(pathname: string) {
  if (!basePath || !pathname.startsWith(basePath)) {
    return pathname || "/";
  }

  const trimmed = pathname.slice(basePath.length);
  return trimmed.startsWith("/") ? trimmed || "/" : `/${trimmed}`;
}

function withBasePath(pathname: string) {
  if (!basePath) {
    return pathname;
  }

  return pathname === "/" ? `${basePath}/` : `${basePath}${pathname}`;
}

export const basePath = normalizeBasePath();
export const releaseVariant = parseReleaseVariant(import.meta.env.VITE_RELEASE_VARIANT);
export const enabledRouteSlices = routeSliceDefinitions
  .filter(definition => parseBoolean(routeSliceFlags[definition.id], defaultEnabledSlices[definition.id]))
  .map(definition => definition.id);

const enabledRouteSliceSet = new Set(enabledRouteSlices);

export function getRoutePathForLocation(path: string) {
  const { pathname, search, hash } = normalizeLocationPath(path);
  return {
    pathname: stripBasePath(pathname),
    search,
    hash,
  };
}

export function getApprovedRouteSlice(path: string) {
  const { pathname } = getRoutePathForLocation(path);
  const routeSlice = getRouteSliceForPath(pathname);
  return routeSlice && enabledRouteSliceSet.has(routeSlice) ? routeSlice : null;
}

export function shouldServeFromLegacy(path: string) {
  const { pathname } = getRoutePathForLocation(path);

  if (releaseVariant === "legacy-only") {
    return true;
  }

  if (getLegacyFallbackRule(pathname)) {
    return true;
  }

  const routeSlice = getRouteSliceForPath(pathname);
  if (!routeSlice) {
    return true;
  }

  return !enabledRouteSliceSet.has(routeSlice);
}

export function resolveLegacyFallbackUrl(path: string) {
  const url = new URL(path, typeof window === "undefined" ? "http://localhost" : window.location.origin);
  const normalizedPathname = stripBasePath(url.pathname);
  const fallbackPathname = resolveLegacyFallbackPath(normalizedPathname);
  const nextPathname = withBasePath(fallbackPathname);
  const origin = import.meta.env.VITE_LEGACY_FALLBACK_ORIGIN?.replace(/\/$/, "") || url.origin;
  return new URL(`${nextPathname}${url.search}${url.hash}`, `${origin}/`).toString();
}

export function applyReleaseVariantMetadata(target: HTMLElement = document.documentElement) {
  target.dataset.mealieReleaseVariant = releaseVariant;
  target.dataset.mealieApprovedRouteSlices = enabledRouteSlices.join(",");
}
