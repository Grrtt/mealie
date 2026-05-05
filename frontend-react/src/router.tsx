import {
  createRootRoute,
  createRoute,
  createRouter,
  redirect,
} from "@tanstack/react-router";
import { getDefaultLandingRoute, getPublicLandingRoute } from "@/features/auth/defaultLanding";
import {
  redirectAuthenticatedUser,
  requireAdmin,
  requireAdvanced,
  requireAuth,
  requireGroupManager,
  requireHouseholdManager,
  requireOrganizer,
} from "@/features/auth/routeGuards";
import { ensureCurrentUser } from "@/features/auth/session";
import { RouteErrorBoundary } from "@/components/errors/RouteErrorBoundary";
import { IndexRouteComponent } from "@/routes/index";
import { LoginRouteComponent } from "@/routes/login";
import { RegisterRouteComponent } from "@/routes/register/index";
import { ForgotPasswordRouteComponent } from "@/routes/forgot-password";
import { ResetPasswordRouteComponent } from "@/routes/reset-password";
import { GroupLandingRouteComponent } from "@/routes/g/$groupSlug/index";
import { RecipeDetailRouteComponent } from "@/routes/g/$groupSlug/r/$slug/index";
import { RecipeTimelineRouteComponent } from "@/routes/g/$groupSlug/recipes/timeline";
import { RecipeFinderRouteComponent } from "@/routes/g/$groupSlug/recipes/finder/index";
import { RecipeCreateRouteComponent } from "@/routes/g/$groupSlug/r/create";
import { RecipeCreateNewRouteComponent } from "@/routes/g/$groupSlug/r/create/new";
import { RecipeCreateUrlRouteComponent } from "@/routes/g/$groupSlug/r/create/url";
import { RecipeCreateZipRouteComponent } from "@/routes/g/$groupSlug/r/create/zip";
import { RecipeCreateHtmlRouteComponent } from "@/routes/g/$groupSlug/r/create/html";
import { RecipeCreateImageRouteComponent } from "@/routes/g/$groupSlug/r/create/image";
import { RecipeCreateBulkRouteComponent } from "@/routes/g/$groupSlug/r/create/bulk";
import { RecipeCreateDebugRouteComponent } from "@/routes/g/$groupSlug/r/create/debug";
import { ShareTargetRecipeCreateUrlRouteComponent } from "@/routes/r/create/url";
import { RecipeCategoriesRouteComponent } from "@/routes/g/$groupSlug/recipes/categories/index";
import { RecipeTagsRouteComponent } from "@/routes/g/$groupSlug/recipes/tags/index";
import { RecipeToolsRouteComponent } from "@/routes/g/$groupSlug/recipes/tools/index";
import { CookbooksRouteComponent } from "@/routes/g/$groupSlug/cookbooks/index";
import { CookbookDetailRouteComponent } from "@/routes/g/$groupSlug/cookbooks/$slug";
import { SharedRecipeRouteComponent } from "@/routes/g/$groupSlug/shared/r/$id";
import { MealPlannerRedirectRouteComponent } from "@/routes/household/mealplan/planner";
import { MealPlannerViewRouteComponent } from "@/routes/household/mealplan/planner/view";
import { MealPlannerEditRouteComponent } from "@/routes/household/mealplan/planner/edit";
import { MealPlannerSettingsRouteComponent } from "@/routes/household/mealplan/settings";
import { ShoppingListsRouteComponent } from "@/routes/shopping-lists/index";
import { ShoppingListDetailRouteComponent } from "@/routes/shopping-lists/$id";
import { AdminSetupRouteComponent } from "@/routes/admin/setup";
import { UserProfileRouteComponent } from "@/routes/user/profile/index";
import { UserProfileEditRouteComponent } from "@/routes/user/profile/edit";
import { UserApiTokensRouteComponent } from "@/routes/user/profile/api-tokens";
import { UserFavoritesRouteComponent } from "@/routes/user/$id/favorites";
import { HouseholdRouteComponent } from "@/routes/household/index";
import { HouseholdMembersRouteComponent } from "@/routes/household/members";
import { HouseholdNotifiersRouteComponent } from "@/routes/household/notifiers";
import { HouseholdWebhooksRouteComponent } from "@/routes/household/webhooks";
import { GroupRouteComponent } from "@/routes/group/index";
import { GroupDataRouteComponent } from "@/routes/group/data";
import { GroupDataRecipesRouteComponent } from "@/routes/group/data/recipes";
import { GroupDataCategoriesRouteComponent } from "@/routes/group/data/categories";
import { GroupDataTagsRouteComponent } from "@/routes/group/data/tags";
import { GroupDataToolsRouteComponent } from "@/routes/group/data/tools";
import { GroupDataFoodsRouteComponent } from "@/routes/group/data/foods";
import { GroupDataUnitsRouteComponent } from "@/routes/group/data/units";
import { GroupDataLabelsRouteComponent } from "@/routes/group/data/labels";
import { GroupDataRecipeActionsRouteComponent } from "@/routes/group/data/recipe-actions";
import { GroupMigrationsRouteComponent } from "@/routes/group/migrations";
import { GroupReportDetailRouteComponent } from "@/routes/group/reports/$id";
import { AdminSiteSettingsRouteComponent } from "@/routes/admin/site-settings";
import { AdminBackupsRouteComponent } from "@/routes/admin/backups";
import { AdminAiConfigurationsRouteComponent } from "@/routes/admin/ai-configurations";
import { AdminDebugIndexesRouteComponent } from "@/routes/admin/debug/indexes";
import { AdminDebugOpenAiRouteComponent } from "@/routes/admin/debug/openai";
import { AdminDebugParserRouteComponent } from "@/routes/admin/debug/parser";
import { AdminMaintenanceRouteComponent } from "@/routes/admin/maintenance/index";
import { AdminManageUsersRouteComponent } from "@/routes/admin/manage/users/index";
import { AdminCreateUserRouteComponent } from "@/routes/admin/manage/users/create";
import { AdminManageUserDetailRouteComponent } from "@/routes/admin/manage/users/$id";
import { AdminManageGroupsRouteComponent } from "@/routes/admin/manage/groups/index";
import { AdminManageGroupDetailRouteComponent } from "@/routes/admin/manage/groups/$id";
import { AdminManageHouseholdsRouteComponent } from "@/routes/admin/manage/households/index";
import { AdminManageHouseholdDetailRouteComponent } from "@/routes/admin/manage/households/$id";
import { AdminManageIngredientAliasesRouteComponent } from "@/routes/admin/manage/ingredient-aliases";
import { LegacyFallbackRouteComponent, RootRouteComponent, RoutePendingComponent } from "@/routes/__root";

const rootRoute = createRootRoute({
  component: RootRouteComponent,
  pendingComponent: RoutePendingComponent,
  errorComponent: RouteErrorBoundary,
  notFoundComponent: LegacyFallbackRouteComponent,
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  beforeLoad: async () => {
    const user = await ensureCurrentUser();
    throw redirect({
      href: user ? await getDefaultLandingRoute(user) : await getPublicLandingRoute(),
    });
  },
  component: IndexRouteComponent,
});

const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/login",
  beforeLoad: async () => {
    await redirectAuthenticatedUser();
  },
  component: LoginRouteComponent,
  validateSearch: search => search as { redirect?: string; direct?: string; code?: string; error?: string },
});

const registerRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/register",
  beforeLoad: async () => {
    await redirectAuthenticatedUser();
  },
  component: RegisterRouteComponent,
});

const forgotPasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/forgot-password",
  beforeLoad: async () => {
    await redirectAuthenticatedUser();
  },
  component: ForgotPasswordRouteComponent,
});

const resetPasswordRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/reset-password",
  beforeLoad: async () => {
    await redirectAuthenticatedUser();
  },
  component: ResetPasswordRouteComponent,
  validateSearch: search => search as { token?: string },
});

const groupLandingRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: GroupLandingRouteComponent,
});

const recipeDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/r/$slug",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeDetailRouteComponent,
  validateSearch: search => search as { edit?: string | boolean },
});

const recipeTimelineRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/recipes/timeline",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeTimelineRouteComponent,
});

const recipeFinderRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/recipes/finder",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeFinderRouteComponent,
});

const recipeCreateRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/r/create",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeCreateRouteComponent,
});

const recipeCreateNewRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/r/create/new",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeCreateNewRouteComponent,
});

const recipeCreateUrlRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/r/create/url",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeCreateUrlRouteComponent,
  validateSearch: search => search as { recipe_import_url?: string },
});

const recipeCreateZipRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/r/create/zip",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeCreateZipRouteComponent,
});

const recipeCreateHtmlRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/r/create/html",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeCreateHtmlRouteComponent,
});

const recipeCreateImageRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/r/create/image",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeCreateImageRouteComponent,
});

const recipeCreateBulkRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/r/create/bulk",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeCreateBulkRouteComponent,
});

const recipeCreateDebugRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/r/create/debug",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeCreateDebugRouteComponent,
});

const shareTargetRecipeCreateUrlRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/r/create/url",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: ShareTargetRecipeCreateUrlRouteComponent,
  validateSearch: search => search as Record<string, string | undefined>,
});

const recipeCategoriesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/recipes/categories",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeCategoriesRouteComponent,
  validateSearch: search => search as { id?: string },
});

const recipeTagsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/recipes/tags",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeTagsRouteComponent,
  validateSearch: search => search as { id?: string },
});

const recipeToolsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/recipes/tools",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: RecipeToolsRouteComponent,
  validateSearch: search => search as { id?: string },
});

const cookbooksRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/cookbooks",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: CookbooksRouteComponent,
});

const cookbookDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/cookbooks/$slug",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: CookbookDetailRouteComponent,
});

const sharedRecipeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/g/$groupSlug/shared/r/$id",
  component: SharedRecipeRouteComponent,
});

const mealPlannerRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/household/mealplan/planner",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
    throw redirect({
      href: `/household/mealplan/planner/view${location.searchStr ?? ""}`,
    });
  },
  component: MealPlannerRedirectRouteComponent,
});

const mealPlannerViewRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/household/mealplan/planner/view",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: MealPlannerViewRouteComponent,
  validateSearch: search => search as { start?: string; end?: string },
});

const mealPlannerEditRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/household/mealplan/planner/edit",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: MealPlannerEditRouteComponent,
  validateSearch: search => search as { start?: string; end?: string },
});

const mealPlannerSettingsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/household/mealplan/settings",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: MealPlannerSettingsRouteComponent,
});

const shoppingListsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/shopping-lists",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: ShoppingListsRouteComponent,
  validateSearch: search => search as { disableRedirect?: string | boolean },
});

const shoppingListDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/shopping-lists/$id",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: ShoppingListDetailRouteComponent,
});

const adminSetupRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/setup",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminSetupRouteComponent,
});

const adminRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
    throw redirect({
      href: "/admin/site-settings",
    });
  },
  component: RoutePendingComponent,
});

const userProfileRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/user/profile",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: UserProfileRouteComponent,
});

const userProfileEditRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/user/profile/edit",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: UserProfileEditRouteComponent,
});

const userApiTokensRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/user/profile/api-tokens",
  beforeLoad: async ({ location }) => {
    await requireAdvanced(location);
  },
  component: UserApiTokensRouteComponent,
});

const userFavoritesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/user/$id/favorites",
  beforeLoad: async ({ location }) => {
    await requireAuth(location);
  },
  component: UserFavoritesRouteComponent,
});

const householdRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/household",
  beforeLoad: async ({ location }) => {
    await requireHouseholdManager(location);
  },
  component: HouseholdRouteComponent,
});

const householdMembersRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/household/members",
  beforeLoad: async ({ location }) => {
    await requireGroupManager(location);
  },
  component: HouseholdMembersRouteComponent,
});

const householdNotifiersRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/household/notifiers",
  beforeLoad: async ({ location }) => {
    await requireAdvanced(location);
  },
  component: HouseholdNotifiersRouteComponent,
});

const householdWebhooksRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/household/webhooks",
  beforeLoad: async ({ location }) => {
    await requireAdvanced(location);
  },
  component: HouseholdWebhooksRouteComponent,
});

const groupRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/group",
  beforeLoad: async ({ location }) => {
    await requireGroupManager(location);
  },
  component: GroupRouteComponent,
});

const groupDataRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/group/data",
  beforeLoad: async ({ location }) => {
    await requireOrganizer(location);
  },
  component: GroupDataRouteComponent,
});

const groupDataRecipesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/group/data/recipes",
  beforeLoad: async ({ location }) => {
    await requireOrganizer(location);
  },
  component: GroupDataRecipesRouteComponent,
});

const groupDataCategoriesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/group/data/categories",
  beforeLoad: async ({ location }) => {
    await requireOrganizer(location);
  },
  component: GroupDataCategoriesRouteComponent,
});

const groupDataTagsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/group/data/tags",
  beforeLoad: async ({ location }) => {
    await requireOrganizer(location);
  },
  component: GroupDataTagsRouteComponent,
});

const groupDataToolsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/group/data/tools",
  beforeLoad: async ({ location }) => {
    await requireOrganizer(location);
  },
  component: GroupDataToolsRouteComponent,
});

const groupDataFoodsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/group/data/foods",
  beforeLoad: async ({ location }) => {
    await requireOrganizer(location);
  },
  component: GroupDataFoodsRouteComponent,
});

const groupDataUnitsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/group/data/units",
  beforeLoad: async ({ location }) => {
    await requireOrganizer(location);
  },
  component: GroupDataUnitsRouteComponent,
});

const groupDataLabelsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/group/data/labels",
  beforeLoad: async ({ location }) => {
    await requireOrganizer(location);
  },
  component: GroupDataLabelsRouteComponent,
});

const groupDataRecipeActionsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/group/data/recipe-actions",
  beforeLoad: async ({ location }) => {
    await requireOrganizer(location);
  },
  component: GroupDataRecipeActionsRouteComponent,
});

const groupMigrationsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/group/migrations",
  beforeLoad: async ({ location }) => {
    await requireAdvanced(location);
  },
  component: GroupMigrationsRouteComponent,
});

const groupReportDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/group/reports/$id",
  beforeLoad: async ({ location }) => {
    await requireOrganizer(location);
  },
  component: GroupReportDetailRouteComponent,
});

const adminSiteSettingsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/site-settings",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminSiteSettingsRouteComponent,
});

const adminBackupsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/backups",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminBackupsRouteComponent,
});

const adminAiConfigurationsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/ai-configurations",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminAiConfigurationsRouteComponent,
});

const adminDebugIndexesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/debug/indexes",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminDebugIndexesRouteComponent,
});

const adminDebugOpenAiRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/debug/openai",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminDebugOpenAiRouteComponent,
});

const adminDebugParserRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/debug/parser",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminDebugParserRouteComponent,
});

const adminMaintenanceRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/maintenance",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminMaintenanceRouteComponent,
});

const adminManageUsersRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/manage/users",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminManageUsersRouteComponent,
});

const adminCreateUserRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/manage/users/create",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminCreateUserRouteComponent,
});

const adminManageUserDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/manage/users/$id",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminManageUserDetailRouteComponent,
});

const adminManageGroupsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/manage/groups",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminManageGroupsRouteComponent,
});

const adminManageGroupDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/manage/groups/$id",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminManageGroupDetailRouteComponent,
});

const adminManageHouseholdsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/manage/households",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminManageHouseholdsRouteComponent,
});

const adminManageHouseholdDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/manage/households/$id",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminManageHouseholdDetailRouteComponent,
});

const adminManageIngredientAliasesRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/admin/manage/ingredient-aliases",
  beforeLoad: async ({ location }) => {
    await requireAdmin(location);
  },
  component: AdminManageIngredientAliasesRouteComponent,
});

const routeTree = rootRoute.addChildren([
  indexRoute,
  loginRoute,
  registerRoute,
  forgotPasswordRoute,
  resetPasswordRoute,
  shareTargetRecipeCreateUrlRoute,
  groupLandingRoute,
  recipeDetailRoute,
  recipeTimelineRoute,
  recipeFinderRoute,
  recipeCreateRoute,
  recipeCreateNewRoute,
  recipeCreateUrlRoute,
  recipeCreateZipRoute,
  recipeCreateHtmlRoute,
  recipeCreateImageRoute,
  recipeCreateBulkRoute,
  recipeCreateDebugRoute,
  recipeCategoriesRoute,
  recipeTagsRoute,
  recipeToolsRoute,
  cookbooksRoute,
  cookbookDetailRoute,
  sharedRecipeRoute,
  mealPlannerRoute,
  mealPlannerViewRoute,
  mealPlannerEditRoute,
  mealPlannerSettingsRoute,
  shoppingListsRoute,
  shoppingListDetailRoute,
  userProfileRoute,
  userProfileEditRoute,
  userApiTokensRoute,
  userFavoritesRoute,
  householdRoute,
  householdMembersRoute,
  householdNotifiersRoute,
  householdWebhooksRoute,
  groupRoute,
  groupDataRoute,
  groupDataRecipesRoute,
  groupDataCategoriesRoute,
  groupDataTagsRoute,
  groupDataToolsRoute,
  groupDataFoodsRoute,
  groupDataUnitsRoute,
  groupDataLabelsRoute,
  groupDataRecipeActionsRoute,
  groupMigrationsRoute,
  groupReportDetailRoute,
  adminRoute,
  adminSetupRoute,
  adminSiteSettingsRoute,
  adminBackupsRoute,
  adminAiConfigurationsRoute,
  adminDebugIndexesRoute,
  adminDebugOpenAiRoute,
  adminDebugParserRoute,
  adminMaintenanceRoute,
  adminManageUsersRoute,
  adminCreateUserRoute,
  adminManageUserDetailRoute,
  adminManageGroupsRoute,
  adminManageGroupDetailRoute,
  adminManageHouseholdsRoute,
  adminManageHouseholdDetailRoute,
  adminManageIngredientAliasesRoute,
]);

export const router = createRouter({
  routeTree,
  defaultPreload: "intent",
  scrollRestoration: true,
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
