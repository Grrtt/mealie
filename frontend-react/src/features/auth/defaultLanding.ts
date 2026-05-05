import { apiClient } from "@/lib/api/client";
import type { ActivityKey, AppInfo, AppStartupInfo, PrivateUser } from "@/lib/api/contracts";

const DEFAULT_ACTIVITY_KEY = "activity-preferences";

type StoredActivity = {
  defaultActivity?: ActivityKey;
};

export function getStoredDefaultActivity() {
  if (typeof window === "undefined") return "recipes" as ActivityKey;

  try {
    const rawValue = window.localStorage.getItem(DEFAULT_ACTIVITY_KEY);
    if (!rawValue) return "recipes" as ActivityKey;

    return (JSON.parse(rawValue) as StoredActivity).defaultActivity ?? ("recipes" as ActivityKey);
  }
  catch {
    return "recipes" as ActivityKey;
  }
}

export function setStoredDefaultActivity(defaultActivity: ActivityKey) {
  if (typeof window === "undefined") return;

  window.localStorage.setItem(DEFAULT_ACTIVITY_KEY, JSON.stringify({ defaultActivity }));
}

export function getDefaultActivityRoute(activityKey?: ActivityKey, groupSlug?: string | null) {
  switch (activityKey) {
    case "mealplanner":
      return "/household/mealplan/planner/view";
    case "shopping_list":
      return "/shopping-lists";
    case "recipes":
    default:
      return groupSlug ? `/g/${groupSlug}` : "/g/home";
  }
}

export async function getDefaultLandingRoute(user: PrivateUser) {
  const startupInfo = await apiClient.get<AppStartupInfo>("/api/app/about/startup-info", {
    suppressAuthRedirect: true,
  });

  if (!startupInfo.isDemo && startupInfo.isFirstLogin && user.admin) {
    return "/admin/setup";
  }

  return getDefaultActivityRoute(getStoredDefaultActivity(), user.groupSlug);
}

export async function getPublicLandingRoute() {
  const appInfo = await apiClient.get<AppInfo>("/api/app/about", {
    suppressAuthRedirect: true,
  });

  if (appInfo.defaultGroupSlug) {
    return `/g/${appInfo.defaultGroupSlug}`;
  }

  return "/login";
}
