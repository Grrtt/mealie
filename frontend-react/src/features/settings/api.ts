import { apiClient } from "@/lib/api/client";
import type { PaginationData, RecipeSummary } from "@/lib/api/contracts";
import type {
  ChangePassword,
  GroupBase,
  GroupInDB,
  LongLiveTokenOut,
  PrivateUser,
  UserBase,
  UserIn,
  UserOut,
} from "../../../../frontend/app/lib/api/types/user";
import type {
  CreateWebhook,
  GroupEventNotifierCreate,
  GroupEventNotifierOut,
  GroupEventNotifierUpdate,
  GroupRecipeActionOut,
  HouseholdInDB,
  HouseholdStatistics,
  ReadHouseholdPreferences,
  ReadWebhook,
  SetPermissions,
} from "../../../../frontend/app/lib/api/types/household";
import type {
  GroupStorage,
  ReadGroupPreferences,
  SupportedMigrations,
} from "../../../../frontend/app/lib/api/types/group";
import type {
  CategoryIn,
  CreateIngredientFood,
  CreateIngredientUnit,
  IngredientFood,
  IngredientUnit,
  Recipe,
  RecipeCategoryResponse,
  RecipeTagResponse,
  RecipeToolResponse,
  TagIn,
  RecipeToolCreate,
} from "../../../../frontend/app/lib/api/types/recipe";
import type {
  MultiPurposeLabelCreate,
  MultiPurposeLabelOut,
  MultiPurposeLabelUpdate,
} from "../../../../frontend/app/lib/api/types/labels";
import type { ReportOut, ReportSummary } from "../../../../frontend/app/lib/api/types/reports";
import type { AllBackups, IndexInfo, MaintenanceStorageDetails, MaintenanceSummary } from "../../../../frontend/app/lib/api/types/admin";

type QueryValue = string | number | boolean | null | undefined;

export interface SiteSettingsResponse {
  defaultParser: string;
  defaultParserUnavailable: boolean;
  ingredientSystemPrompt: string | null;
  categorySystemPrompt: string | null;
  tagSystemPrompt: string | null;
}

export interface UpdateSiteSettingsRequest {
  defaultParser: string;
  ingredientSystemPrompt?: string | null;
  categorySystemPrompt?: string | null;
  tagSystemPrompt?: string | null;
}

export interface SeederConfig {
  locale: string;
}

export interface AiConfigurationResponse {
  id: string;
  name: string;
  providerType: string;
  hasApiKey: boolean;
  maskedApiKey: string | null;
  projectId: string | null;
  baseUrl: string | null;
  defaultModel: string | null;
  isActive: boolean;
  enableImageServices: boolean;
  enableTranscriptionServices: boolean;
  createdAt: string | null;
}

export interface CreateAiConfigurationRequest {
  name: string;
  providerType: string;
  apiKey?: string | null;
  projectId?: string | null;
  baseUrl?: string | null;
  defaultModel?: string | null;
  enableImageServices?: boolean;
  enableTranscriptionServices?: boolean;
}

export interface UpdateAiConfigurationRequest {
  name?: string | null;
  apiKey?: string | null;
  projectId?: string | null;
  baseUrl?: string | null;
  defaultModel?: string | null;
  enableImageServices?: boolean | null;
  enableTranscriptionServices?: boolean | null;
}

export type AdminLogMinimumLevel = "Warning" | "Error";

export interface AdminLogEntryResponse {
  timestamp: string;
  level: string;
  message: string;
  exception: string | null;
  sourceContext: string | null;
  requestMethod: string | null;
  requestPath: string | null;
  statusCode: number | null;
  correlationId: string | null;
}

export interface AdminLogsResponse {
  minimumLevel: AdminLogMinimumLevel;
  limit: number;
  totalCount: number;
  entries: AdminLogEntryResponse[];
}

export interface UnresolvedIngredient {
  rawText: string;
  count: number;
}

export interface IngredientAlias {
  id: string;
  name: string;
  foodId: string;
  foodName: string;
}

export interface CreateIngredientAliasPayload {
  rawText: string;
  foodId: string;
  backfillRecipes: boolean;
}

function toQuery(params: Record<string, QueryValue>) {
  const query = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value == null || value === "") continue;
    query.set(key, String(value));
  }

  const value = query.toString();
  return value ? `?${value}` : "";
}

export async function updateCurrentUser(userId: string, payload: Partial<UserBase>) {
  return await apiClient.put<PrivateUser>(`/api/users/${userId}`, payload);
}

export async function changeOwnPassword(payload: ChangePassword) {
  return await apiClient.put("/api/users/password", payload);
}

export async function createApiToken(name: string) {
  return await apiClient.post<LongLiveTokenOut & { token: string }>("/api/users/api-tokens", { name });
}

export async function deleteApiToken(tokenId: number) {
  return await apiClient.delete(`/api/users/api-tokens/${tokenId}`);
}

export async function fetchHousehold() {
  return await apiClient.get<HouseholdInDB>("/api/households/self");
}

export async function fetchHouseholdPreferences() {
  return normalizeHouseholdPreferences(await apiClient.get<ReadHouseholdPreferences>("/api/households/preferences"));
}

export async function updateHouseholdPreferences(payload: Partial<ReadHouseholdPreferences>) {
  const normalizedPayload: Record<string, string | boolean> = {};

  if (payload.privateHousehold != null) {
    normalizedPayload.privateHousehold = payload.privateHousehold;
  }

  if (payload.firstDayOfWeek != null) {
    normalizedPayload.firstDayOfWeek = String(payload.firstDayOfWeek);
  }

  for (const key of ["recipePublic", "recipeShowNutrition", "recipeShowAssets", "recipeLandscapeView", "recipeDisableComments"] as const) {
    const value = payload[key];
    if (value != null) {
      normalizedPayload[key] = String(value);
    }
  }

  return normalizeHouseholdPreferences(await apiClient.put<ReadHouseholdPreferences>("/api/households/preferences", normalizedPayload));
}

function parseNullableBoolean(value: unknown) {
  if (typeof value === "boolean") {
    return value;
  }

  if (typeof value === "string") {
    const normalized = value.trim().toLowerCase();
    if (["1", "true", "yes", "on"].includes(normalized)) {
      return true;
    }

    if (["0", "false", "no", "off", "none", ""].includes(normalized)) {
      return false;
    }
  }

  return undefined;
}

function normalizeHouseholdPreferences(payload: ReadHouseholdPreferences) {
  return {
    ...payload,
    firstDayOfWeek: payload.firstDayOfWeek == null ? undefined : Number(payload.firstDayOfWeek),
    recipePublic: parseNullableBoolean(payload.recipePublic) ?? false,
    recipeShowNutrition: parseNullableBoolean(payload.recipeShowNutrition) ?? false,
    recipeShowAssets: parseNullableBoolean(payload.recipeShowAssets) ?? false,
    recipeLandscapeView: parseNullableBoolean(payload.recipeLandscapeView) ?? false,
    recipeDisableComments: parseNullableBoolean(payload.recipeDisableComments) ?? false,
  } satisfies ReadHouseholdPreferences;
}

export async function fetchHouseholdMembers() {
  return await apiClient.get<PaginationData<UserOut>>("/api/households/members?page=1&perPage=-1");
}

export async function updateHouseholdPermissions(payload: SetPermissions) {
  return await apiClient.put<UserOut>("/api/households/permissions", payload);
}

export async function fetchHouseholdStatistics() {
  return await apiClient.get<HouseholdStatistics>("/api/households/statistics");
}

export async function createHouseholdInvite(uses = 1) {
  return await apiClient.post<{ token: string; usesLeft: number }>("/api/households/invitations", { uses });
}

export async function sendHouseholdInvitationEmail(payload: { email: string; token: string }) {
  return await apiClient.post<{ detail: string }>("/api/households/invitations/email", payload);
}

type HouseholdNotifier = GroupEventNotifierOut & { appriseUrl?: string | null };

export async function fetchHouseholdNotifiers() {
  return await apiClient.get<PaginationData<HouseholdNotifier>>("/api/households/events/notifications?page=1&perPage=-1");
}

export async function createHouseholdNotifier(payload: GroupEventNotifierCreate) {
  return await apiClient.post<HouseholdNotifier>("/api/households/events/notifications", payload);
}

export async function updateHouseholdNotifier(id: string, payload: Partial<GroupEventNotifierUpdate>) {
  return await apiClient.put<HouseholdNotifier>(`/api/households/events/notifications/${id}`, payload);
}

export async function deleteHouseholdNotifier(id: string) {
  return await apiClient.delete(`/api/households/events/notifications/${id}`);
}

export async function testHouseholdNotifier(id: string) {
  return await apiClient.post(`/api/households/events/notifications/${id}/test`, {});
}

export async function fetchHouseholdWebhooks() {
  return await apiClient.get<PaginationData<ReadWebhook>>("/api/households/webhooks?page=1&perPage=-1");
}

export async function createHouseholdWebhook(payload: CreateWebhook) {
  return await apiClient.post<ReadWebhook>("/api/households/webhooks", payload);
}

export async function updateHouseholdWebhook(id: string, payload: Partial<ReadWebhook>) {
  return await apiClient.put<ReadWebhook>(`/api/households/webhooks/${id}`, payload);
}

export async function deleteHouseholdWebhook(id: string) {
  return await apiClient.delete(`/api/households/webhooks/${id}`);
}

export async function testHouseholdWebhook(id: string) {
  return await apiClient.post(`/api/households/webhooks/${id}/test`, {});
}

export async function fetchGroup() {
  return await apiClient.get<GroupInDB>("/api/groups/self");
}

export async function fetchGroupPreferences() {
  return await apiClient.get<ReadGroupPreferences>("/api/groups/preferences");
}

export async function updateGroupPreferences(payload: Partial<ReadGroupPreferences>) {
  return await apiClient.put<ReadGroupPreferences>("/api/groups/preferences", payload);
}

export async function fetchGroupStorage() {
  return await apiClient.get<GroupStorage>("/api/groups/storage");
}

export async function fetchGroupReports(category?: string | null) {
  return await apiClient.get<ReportSummary[]>(`/api/groups/reports${toQuery({ report_type: category ?? undefined })}`);
}

export async function fetchGroupReport(id: string) {
  return await apiClient.get<ReportOut>(`/api/groups/reports/${id}`);
}

export async function deleteGroupReport(id: string) {
  return await apiClient.delete(`/api/groups/reports/${id}`);
}

export async function startGroupMigration(migrationType: SupportedMigrations, archive: File, addMigrationTag: boolean) {
  const formData = new FormData();
  formData.append("migration_type", migrationType);
  formData.append("archive", archive);
  formData.append("add_migration_tag", String(addMigrationTag));
  return await apiClient.post<ReportSummary>("/api/groups/migrations", formData);
}

export async function fetchGroupRecipes(search?: string) {
  return await apiClient.get<PaginationData<RecipeSummary>>(`/api/recipes${toQuery({
    page: 1,
    perPage: 50,
    search,
  })}`);
}

export async function fetchRecipeBySlug(slug: string) {
  return await apiClient.get<Recipe>(`/api/recipes/${slug}`);
}

export async function fetchCategoriesPage() {
  return await apiClient.get<PaginationData<RecipeCategoryResponse>>("/api/organizers/categories?page=1&perPage=200");
}

export async function createCategory(payload: CategoryIn) {
  return await apiClient.post<RecipeCategoryResponse>("/api/organizers/categories", payload);
}

export async function updateCategory(id: string, payload: CategoryIn) {
  return await apiClient.put<RecipeCategoryResponse>(`/api/organizers/categories/${id}`, payload);
}

export async function deleteCategory(id: string) {
  return await apiClient.delete(`/api/organizers/categories/${id}`);
}

export async function fetchTagsPage() {
  return await apiClient.get<PaginationData<RecipeTagResponse>>("/api/organizers/tags?page=1&perPage=200");
}

export async function createTag(payload: TagIn) {
  return await apiClient.post<RecipeTagResponse>("/api/organizers/tags", payload);
}

export async function updateTag(id: string, payload: TagIn) {
  return await apiClient.put<RecipeTagResponse>(`/api/organizers/tags/${id}`, payload);
}

export async function deleteTag(id: string) {
  return await apiClient.delete(`/api/organizers/tags/${id}`);
}

export async function fetchToolsPage() {
  return await apiClient.get<PaginationData<RecipeToolResponse>>("/api/organizers/tools?page=1&perPage=200");
}

export async function createTool(payload: RecipeToolCreate) {
  return await apiClient.post<RecipeToolResponse>("/api/organizers/tools", payload);
}

export async function updateTool(id: string, payload: RecipeToolCreate) {
  return await apiClient.put<RecipeToolResponse>(`/api/organizers/tools/${id}`, payload);
}

export async function deleteTool(id: string) {
  return await apiClient.delete(`/api/organizers/tools/${id}`);
}

export async function fetchFoodsPage() {
  return await apiClient.get<PaginationData<IngredientFood>>("/api/foods?page=1&perPage=200");
}

export async function createFood(payload: CreateIngredientFood) {
  return await apiClient.post<IngredientFood>("/api/foods", payload);
}

export async function updateFood(id: string, payload: Partial<IngredientFood>) {
  return await apiClient.put<IngredientFood>(`/api/foods/${id}`, payload);
}

export async function deleteFood(id: string) {
  return await apiClient.delete(`/api/foods/${id}`);
}

export async function fetchUnitsPage() {
  return await apiClient.get<PaginationData<IngredientUnit>>("/api/units?page=1&perPage=200");
}

export async function createUnit(payload: CreateIngredientUnit) {
  return await apiClient.post<IngredientUnit>("/api/units", payload);
}

export async function updateUnit(id: string, payload: Partial<IngredientUnit>) {
  return await apiClient.put<IngredientUnit>(`/api/units/${id}`, payload);
}

export async function deleteUnit(id: string) {
  return await apiClient.delete(`/api/units/${id}`);
}

export async function fetchLabelsPage() {
  return await apiClient.get<PaginationData<MultiPurposeLabelOut>>("/api/groups/labels?page=1&perPage=200");
}

export async function createLabel(payload: MultiPurposeLabelCreate) {
  return await apiClient.post<MultiPurposeLabelOut>("/api/groups/labels", payload);
}

export async function updateLabel(id: string, payload: MultiPurposeLabelUpdate) {
  return await apiClient.put<MultiPurposeLabelOut>(`/api/groups/labels/${id}`, payload);
}

export async function deleteLabel(id: string) {
  return await apiClient.delete(`/api/groups/labels/${id}`);
}

export async function fetchGroupRecipeActions() {
  return await apiClient.get<PaginationData<GroupRecipeActionOut>>("/api/households/recipe-actions?page=1&perPage=200");
}

export async function createGroupRecipeAction(payload: Omit<GroupRecipeActionOut, "id" | "groupId" | "householdId">) {
  return await apiClient.post<GroupRecipeActionOut>("/api/households/recipe-actions", payload);
}

export async function updateGroupRecipeAction(id: string, payload: Partial<GroupRecipeActionOut>) {
  return await apiClient.put<GroupRecipeActionOut>(`/api/households/recipe-actions/${id}`, payload);
}

export async function deleteGroupRecipeAction(id: string) {
  return await apiClient.delete(`/api/households/recipe-actions/${id}`);
}

export async function triggerGroupRecipeAction(id: string, recipeSlug: string, recipeScale: number) {
  return await apiClient.post(`/api/households/recipe-actions/${id}/trigger/${recipeSlug}`, {
    recipe_scale: recipeScale,
  });
}

export async function fetchAdminAbout() {
  return await apiClient.get<unknown>("/api/admin/about");
}

export async function fetchAdminChecks() {
  return await apiClient.get<unknown>("/api/admin/about/check");
}

export async function fetchAdminAnalytics() {
  return await apiClient.get<unknown>("/api/admin/analytics");
}

export async function fetchSiteSettings() {
  return await apiClient.get<SiteSettingsResponse>("/api/admin/site-settings");
}

export async function updateSiteSettings(payload: UpdateSiteSettingsRequest) {
  return await apiClient.put<SiteSettingsResponse>("/api/admin/site-settings", payload);
}

export async function seedFoods(payload: SeederConfig) {
  return await apiClient.post<{ detail?: string; message?: string }>("/api/groups/seeders/foods", payload);
}

export async function seedUnits(payload: SeederConfig) {
  return await apiClient.post<{ detail?: string; message?: string }>("/api/groups/seeders/units", payload);
}

export async function seedLabels(payload: SeederConfig) {
  return await apiClient.post<{ detail?: string; message?: string }>("/api/groups/seeders/labels", payload);
}

export async function fetchBackups() {
  return await apiClient.get<AllBackups>("/api/admin/backups");
}

export async function createBackup() {
  return await apiClient.post<{ error?: boolean; message?: string }>("/api/admin/backups", {});
}

export async function deleteBackup(fileName: string) {
  return await apiClient.delete(`/api/admin/backups/${fileName}`);
}

export async function restoreBackup(fileName: string) {
  return await apiClient.post(`/api/admin/backups/${fileName}/restore`, {});
}

export function backupDownloadPath(fileName: string) {
  return apiClient.resolvePath(`/api/admin/backups/${fileName}`);
}

export async function fetchAiConfigurations() {
  return await apiClient.get<AiConfigurationResponse[]>("/api/admin/ai-configurations");
}

export async function createAiConfiguration(payload: CreateAiConfigurationRequest) {
  return await apiClient.post<AiConfigurationResponse>("/api/admin/ai-configurations", payload);
}

export async function updateAiConfiguration(id: string, payload: UpdateAiConfigurationRequest) {
  return await apiClient.put<AiConfigurationResponse>(`/api/admin/ai-configurations/${id}`, payload);
}

export async function deleteAiConfiguration(id: string) {
  return await apiClient.delete(`/api/admin/ai-configurations/${id}`);
}

export async function activateAiConfiguration(id: string) {
  return await apiClient.put<AiConfigurationResponse>(`/api/admin/ai-configurations/${id}/activate`, {});
}

export async function fetchAdminLogs({
  minimumLevel = "Warning",
  limit = 100,
}: {
  minimumLevel?: AdminLogMinimumLevel;
  limit?: number;
} = {}) {
  return await apiClient.get<AdminLogsResponse>(`/api/admin/logs${toQuery({ minimumLevel, limit })}`);
}

export async function fetchIndexes() {
  return await apiClient.get<IndexInfo[]>("/api/admin/indexes");
}

export async function rebuildIndex(name: string) {
  return await apiClient.post<{ detail: string }>(`/api/admin/indexes/${name}/rebuild`, {});
}

export async function deleteIndex(name: string) {
  return await apiClient.delete<{ detail: string }>(`/api/admin/indexes/${name}`);
}

export async function searchIndex(name: string, query: string) {
  return await apiClient.post<{ results?: unknown[]; total?: number }>(`/api/admin/indexes/${name}/search`, {
    query,
  });
}

export async function debugOpenAi(file?: File | null) {
  const formData = new FormData();
  if (file) {
    formData.append("image", file);
    formData.append("extension", file.name.split(".").pop() ?? "");
  }
  return await apiClient.post<{ response?: string; success?: boolean }>("/api/admin/debug/openai", formData);
}

export async function parseIngredient(parser: "nlp" | "brute" | "openai", ingredient: string) {
  return await apiClient.post<unknown>("/api/parser/ingredient", { parser, ingredient });
}

export async function fetchMaintenanceSummary() {
  return await apiClient.get<MaintenanceSummary>("/api/admin/maintenance");
}

export async function fetchMaintenanceStorage() {
  return await apiClient.get<MaintenanceStorageDetails>("/api/admin/maintenance/storage");
}

export async function runMaintenanceAction(action: "temp" | "images" | "recipe-folders" | "logs") {
  const endpoint = {
    temp: "/api/admin/maintenance/clean/temp",
    images: "/api/admin/maintenance/clean/images",
    "recipe-folders": "/api/admin/maintenance/clean/recipe-folders",
    logs: "/api/admin/maintenance/clean/logs",
  }[action];

  return await apiClient.post(endpoint, {});
}

export async function fetchAdminUsers() {
  return await apiClient.get<PaginationData<UserOut>>("/api/admin/users?page=1&perPage=200");
}

export async function fetchAdminUser(id: string) {
  return await apiClient.get<UserOut>(`/api/admin/users/${id}`);
}

export async function createAdminUser(payload: UserIn) {
  return await apiClient.post<UserOut>("/api/admin/users", payload);
}

export async function updateAdminUser(id: string, payload: Partial<UserOut>) {
  return await apiClient.put<UserOut>(`/api/admin/users/${id}`, payload);
}

export async function deleteAdminUser(id: string) {
  return await apiClient.delete(`/api/admin/users/${id}`);
}

export async function unlockAllUsers() {
  return await apiClient.post<{ unlocked?: number }>("/api/admin/users/unlock?force=true", {});
}

export async function generatePasswordResetToken(email: string) {
  return await apiClient.post<{ token: string }>("/api/admin/users/password-reset-token", { email });
}

export async function fetchAdminGroups() {
  return await apiClient.get<PaginationData<GroupInDB>>("/api/admin/groups?page=1&perPage=200");
}

export async function fetchAdminGroup(id: string) {
  return await apiClient.get<GroupInDB>(`/api/admin/groups/${id}`);
}

export async function updateAdminGroup(id: string, payload: Partial<GroupInDB>) {
  return await apiClient.put<GroupInDB>(`/api/admin/groups/${id}`, payload);
}

export async function createAdminGroup(payload: GroupBase) {
  return await apiClient.post<GroupInDB>("/api/admin/groups", payload);
}

export async function deleteAdminGroup(id: string) {
  return await apiClient.delete(`/api/admin/groups/${id}`);
}

export async function fetchAdminHouseholds() {
  return await apiClient.get<PaginationData<HouseholdInDB>>("/api/admin/households?page=1&perPage=200");
}

export async function fetchAdminHousehold(id: string) {
  return await apiClient.get<HouseholdInDB>(`/api/admin/households/${id}`);
}

export async function updateAdminHousehold(id: string, payload: Partial<HouseholdInDB>) {
  return await apiClient.put<HouseholdInDB>(`/api/admin/households/${id}`, payload);
}

export async function createAdminHousehold(payload: { name: string; groupId?: string | null }) {
  return await apiClient.post<HouseholdInDB>("/api/admin/households", payload);
}

export async function deleteAdminHousehold(id: string) {
  return await apiClient.delete(`/api/admin/households/${id}`);
}

export async function fetchIngredientAliases(search?: string) {
  return await apiClient.get<PaginationData<IngredientAlias>>(`/api/admin/ingredient-aliases${toQuery({
    page: 1,
    perPage: 200,
    search,
  })}`);
}

export async function fetchUnresolvedIngredientAliases(search?: string) {
  return await apiClient.get<PaginationData<UnresolvedIngredient>>(`/api/admin/ingredient-aliases/unresolved${toQuery({
    page: 1,
    perPage: 200,
    search,
  })}`);
}

export async function createIngredientAlias(payload: CreateIngredientAliasPayload) {
  return await apiClient.post<IngredientAlias>("/api/admin/ingredient-aliases", payload);
}

export async function deleteIngredientAlias(id: string) {
  return await apiClient.delete(`/api/admin/ingredient-aliases/${id}`);
}
