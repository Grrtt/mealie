import fs from "node:fs";
import path from "node:path";

const repoRoot = path.resolve(__dirname, "../../..");

const localeMessagesDir = path.join(repoRoot, "frontend-react/src/lib/i18n/messages");
const localeDateTimeDir = path.join(repoRoot, "frontend-react/src/lib/i18n/dateTimeFormats");

export type RouteCoverageSample = {
  path: string;
  slice: string;
};

export const routeCoverageSamples: RouteCoverageSample[] = [
  { path: "/", slice: "auth" },
  { path: "/login", slice: "auth" },
  { path: "/register", slice: "auth" },
  { path: "/forgot-password", slice: "auth" },
  { path: "/reset-password", slice: "auth" },
  { path: "/g/demo-group/shared/r/demo-token", slice: "public-recipes" },
  { path: "/g/home", slice: "group-home" },
  { path: "/r/create/url", slice: "recipe-create-import" },
  { path: "/g/home/r/create", slice: "recipe-create-import" },
  { path: "/g/home/r/create/new", slice: "recipe-create-import" },
  { path: "/g/home/r/create/url", slice: "recipe-create-import" },
  { path: "/g/home/r/create/zip", slice: "recipe-create-import" },
  { path: "/g/home/r/create/html", slice: "recipe-create-import" },
  { path: "/g/home/r/create/image", slice: "recipe-create-import" },
  { path: "/g/home/r/create/bulk", slice: "recipe-create-import" },
  { path: "/g/home/r/create/debug", slice: "recipe-create-import" },
  { path: "/g/home/r/demo-recipe", slice: "recipes" },
  { path: "/g/home/recipes/timeline", slice: "recipes" },
  { path: "/g/home/recipes/categories", slice: "recipes" },
  { path: "/g/home/recipes/tags", slice: "recipes" },
  { path: "/g/home/recipes/tools", slice: "recipes" },
  { path: "/g/home/recipes/finder", slice: "recipes" },
  { path: "/g/home/cookbooks", slice: "cookbooks" },
  { path: "/g/home/cookbooks/demo-cookbook", slice: "cookbooks" },
  { path: "/household", slice: "household" },
  { path: "/household/members", slice: "household" },
  { path: "/household/notifiers", slice: "household" },
  { path: "/household/webhooks", slice: "household" },
  { path: "/household/mealplan/planner", slice: "household" },
  { path: "/household/mealplan/planner/view", slice: "household" },
  { path: "/household/mealplan/planner/edit", slice: "household" },
  { path: "/household/mealplan/settings", slice: "household" },
  { path: "/shopping-lists", slice: "shopping-lists" },
  { path: "/shopping-lists/demo-list", slice: "shopping-lists" },
  { path: "/user/demo-user/favorites", slice: "user-profile" },
  { path: "/user/profile", slice: "user-profile" },
  { path: "/user/profile/edit", slice: "user-profile" },
  { path: "/user/profile/api-tokens", slice: "user-profile" },
  { path: "/group", slice: "group-data" },
  { path: "/group/data", slice: "group-data" },
  { path: "/group/data/recipes", slice: "group-data" },
  { path: "/group/data/categories", slice: "group-data" },
  { path: "/group/data/tags", slice: "group-data" },
  { path: "/group/data/tools", slice: "group-data" },
  { path: "/group/data/foods", slice: "group-data" },
  { path: "/group/data/units", slice: "group-data" },
  { path: "/group/data/labels", slice: "group-data" },
  { path: "/group/data/recipe-actions", slice: "group-data" },
  { path: "/group/reports/demo-report", slice: "group-data" },
  { path: "/group/migrations", slice: "group-data" },
  { path: "/admin", slice: "admin" },
  { path: "/admin/setup", slice: "admin" },
  { path: "/admin/site-settings", slice: "admin" },
  { path: "/admin/backups", slice: "admin" },
  { path: "/admin/ai-configurations", slice: "admin" },
  { path: "/admin/debug/indexes", slice: "admin" },
  { path: "/admin/debug/openai", slice: "admin" },
  { path: "/admin/debug/parser", slice: "admin" },
  { path: "/admin/manage/users", slice: "admin" },
  { path: "/admin/manage/users/create", slice: "admin" },
  { path: "/admin/manage/users/demo-user", slice: "admin" },
  { path: "/admin/manage/groups", slice: "admin" },
  { path: "/admin/manage/groups/demo-group", slice: "admin" },
  { path: "/admin/manage/households", slice: "admin" },
  { path: "/admin/manage/households/demo-household", slice: "admin" },
  { path: "/admin/manage/ingredient-aliases", slice: "admin" },
  { path: "/admin/maintenance", slice: "admin" },
];

export const rtlLocales = new Set(["ar-SA", "he-IL"]);

export function listLocaleCodes(directory: string) {
  return fs.readdirSync(directory)
    .filter(fileName => fileName.endsWith(".json"))
    .map(fileName => fileName.replace(/\.json$/, ""))
    .sort((left, right) => left.localeCompare(right));
}

export function listMessageLocaleCodes() {
  return listLocaleCodes(localeMessagesDir);
}

export function listDateTimeLocaleCodes() {
  return listLocaleCodes(localeDateTimeDir);
}
