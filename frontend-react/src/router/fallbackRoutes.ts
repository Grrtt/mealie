export type RouteSliceId =
  | "auth"
  | "public-recipes"
  | "group-home"
  | "recipe-create-import"
  | "recipes"
  | "cookbooks"
  | "household"
  | "shopping-lists"
  | "user-profile"
  | "group-data"
  | "admin";

type RouteSliceDefinition = {
  id: RouteSliceId;
  description: string;
  patterns: RegExp[];
};

type LegacyFallbackRule = {
  id: string;
  description: string;
  patterns: RegExp[];
  fallbackPath?: (pathname: string) => string;
};

export const routeSliceDefinitions: RouteSliceDefinition[] = [
  {
    id: "auth",
    description: "Authentication entry points and root redirects.",
    patterns: [/^\/$/, /^\/login\/?$/, /^\/register\/?$/, /^\/forgot-password\/?$/, /^\/reset-password\/?$/],
  },
  {
    id: "public-recipes",
    description: "Publicly shared recipe pages.",
    patterns: [/^\/g\/[^/]+\/shared\/r\/[^/]+\/?$/],
  },
  {
    id: "group-home",
    description: "Authenticated group landing routes.",
    patterns: [/^\/g\/[^/]+\/?$/],
  },
  {
    id: "recipe-create-import",
    description: "Recipe creation and import entry points.",
    patterns: [
      /^\/r\/create\/url\/?$/,
      /^\/g\/[^/]+\/r\/create\/?$/,
      /^\/g\/[^/]+\/r\/create\/(?:new|url|zip|html|image|bulk|debug)\/?$/,
    ],
  },
  {
    id: "recipes",
    description: "Recipe browse, detail, and finder routes.",
    patterns: [
      /^\/g\/[^/]+\/r\/[^/]+\/?$/,
      /^\/g\/[^/]+\/recipes\/(?:timeline|finder|categories|tags|tools)\/?$/,
    ],
  },
  {
    id: "cookbooks",
    description: "Cookbook browse and detail routes.",
    patterns: [/^\/g\/[^/]+\/cookbooks\/?$/, /^\/g\/[^/]+\/cookbooks\/[^/]+\/?$/],
  },
  {
    id: "household",
    description: "Household settings, members, meal plans, and related tools.",
    patterns: [
      /^\/household\/?$/,
      /^\/household\/members\/?$/,
      /^\/household\/notifiers\/?$/,
      /^\/household\/webhooks\/?$/,
      /^\/household\/mealplan\/planner\/?$/,
      /^\/household\/mealplan\/planner\/(?:view|edit)\/?$/,
      /^\/household\/mealplan\/settings\/?$/,
    ],
  },
  {
    id: "shopping-lists",
    description: "Shopping list index and detail routes.",
    patterns: [/^\/shopping-lists\/?$/, /^\/shopping-lists\/[^/]+\/?$/],
  },
  {
    id: "user-profile",
    description: "Profile, favorites, and API token routes.",
    patterns: [
      /^\/user\/profile\/?$/,
      /^\/user\/profile\/edit\/?$/,
      /^\/user\/profile\/api-tokens\/?$/,
      /^\/user\/[^/]+\/favorites\/?$/,
    ],
  },
  {
    id: "group-data",
    description: "Organizer, group-data, reports, and migration helper routes.",
    patterns: [
      /^\/group\/?$/,
      /^\/group\/data\/?$/,
      /^\/group\/data\/(?:recipes|categories|tags|tools|foods|units|labels|recipe-actions)\/?$/,
      /^\/group\/reports\/[^/]+\/?$/,
      /^\/group\/migrations\/?$/,
    ],
  },
  {
    id: "admin",
    description: "Approved administrative settings and management routes.",
    patterns: [
      /^\/admin\/?$/,
      /^\/admin\/setup\/?$/,
      /^\/admin\/site-settings\/?$/,
      /^\/admin\/backups\/?$/,
      /^\/admin\/ai-configurations\/?$/,
      /^\/admin\/debug\/(?:indexes|openai|parser)\/?$/,
      /^\/admin\/manage\/users\/?$/,
      /^\/admin\/manage\/users\/create\/?$/,
      /^\/admin\/manage\/users\/[^/]+\/?$/,
      /^\/admin\/manage\/groups\/?$/,
      /^\/admin\/manage\/groups\/[^/]+\/?$/,
      /^\/admin\/manage\/households\/?$/,
      /^\/admin\/manage\/households\/[^/]+\/?$/,
      /^\/admin\/manage\/ingredient-aliases\/?$/,
      /^\/admin\/maintenance\/?$/,
    ],
  },
];

const legacyFallbackRules: LegacyFallbackRule[] = [];

function normalizePathname(pathname: string) {
  const normalized = pathname.trim() || "/";
  const withoutQuery = normalized.split("?")[0]?.split("#")[0] ?? normalized;
  if (withoutQuery === "/") {
    return "/";
  }

  return withoutQuery.replace(/\/+$/, "");
}

export function matchPath(pathname: string, patterns: RegExp[]) {
  const normalizedPathname = normalizePathname(pathname);
  return patterns.some(pattern => pattern.test(normalizedPathname));
}

export function getRouteSliceForPath(pathname: string) {
  return routeSliceDefinitions.find(definition => matchPath(pathname, definition.patterns))?.id ?? null;
}

export function getLegacyFallbackRule(pathname: string) {
  return legacyFallbackRules.find(rule => matchPath(pathname, rule.patterns)) ?? null;
}

export function resolveLegacyFallbackPath(pathname: string) {
  const normalizedPathname = normalizePathname(pathname);
  const rule = getLegacyFallbackRule(normalizedPathname);
  return rule?.fallbackPath?.(normalizedPathname) ?? normalizedPathname;
}
