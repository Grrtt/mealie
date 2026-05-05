import { apiClient } from "@/lib/api/client";
import type {
  IngredientFood,
  PaginationData,
  PrivateUser,
  ReadCookBook,
  Recipe,
  RecipeAsset,
  RecipeCategory,
  RecipeCategoryResponse,
  RecipeCommentOut,
  RecipeSettings,
  RecipeSuggestionResponse,
  RecipeSummary,
  RecipeTag,
  RecipeTagResponse,
  RecipeTimelineEventOut,
  RecipeTool,
  RecipeToolResponse,
  ScrapeRecipeData,
  UpdateImageResponse,
  UserRatingSummary,
} from "@/lib/api/contracts";

export type RecipeListParams = {
  search?: string;
  page?: number;
  perPage?: number;
  orderBy?: string;
  orderDirection?: "asc" | "desc";
  queryFilter?: string;
  categories?: string[];
  tags?: string[];
  tools?: string[];
  foods?: string[];
  cookbook?: string;
};

export type UpdateRecipePayload = {
  name?: string;
  description?: string;
  recipeYield?: string;
  totalTime?: string;
  prepTime?: string;
  cookTime?: string;
  orgURL?: string;
  settings?: Partial<RecipeSettings>;
  recipeIngredients?: Array<{
    position: number;
    originalText?: string | null;
    quantity?: number | null;
    title?: string | null;
    note?: string | null;
    disableAmount?: boolean;
  }>;
  recipeInstructions?: Array<{
    position: number;
    text: string;
    title?: string | null;
    summary?: string | null;
  }>;
  notes?: Array<{
    title: string;
    text: string;
  }>;
  tags?: string[];
  categories?: string[];
  tools?: string[];
};

type ShareTokenResponse = {
  id: string;
  recipeId: string;
  groupId: string;
  createdAt: string;
  expiresAt?: string | null;
};

function toQuery(params: Record<string, string | number | boolean | Array<string> | undefined | null>) {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value == null || value === "") continue;
    if (Array.isArray(value)) {
      value.forEach(item => {
        if (item) search.append(key, item);
      });
      continue;
    }

    search.set(key, String(value));
  }

  const query = search.toString();
  return query ? `?${query}` : "";
}

function mapListParams(params: RecipeListParams = {}) {
  return toQuery({
    search: params.search,
    page: params.page ?? 1,
    perPage: params.perPage ?? 24,
    orderBy: params.orderBy,
    orderDirection: params.orderDirection,
    queryFilter: params.queryFilter,
    categories: params.categories,
    tags: params.tags,
    tools: params.tools,
    foods: params.foods,
    cookbook: params.cookbook,
  });
}

export function recipeImageUrl(recipeId?: string | null, imageVersion?: string | null) {
  if (!recipeId) return null;
  return apiClient.resolvePath(`/api/media/recipes/${recipeId}/images/original.webp${imageVersion ? `?version=${encodeURIComponent(imageVersion)}` : ""}`);
}

export function recipeAssetUrl(recipeId: string, assetFileName: string) {
  return apiClient.resolvePath(`/api/media/recipes/${recipeId}/assets/${assetFileName}`);
}

export async function fetchRecipes(params?: RecipeListParams) {
  return await apiClient.get<PaginationData<RecipeSummary>>(`/api/recipes${mapListParams(params)}`);
}

export async function fetchRecipe(slug: string) {
  return await apiClient.get<Recipe>(`/api/recipes/${slug}`);
}

export async function fetchPublicRecipe(groupSlug: string, slug: string) {
  return await apiClient.get<Recipe>(`/api/explore/groups/${groupSlug}/recipes/${slug}`, {
    suppressAuthRedirect: true,
  });
}

export async function fetchPublicRecipes(groupSlug: string, params?: RecipeListParams) {
  return await apiClient.get<PaginationData<RecipeSummary>>(
    `/api/explore/groups/${groupSlug}/recipes${mapListParams(params)}`,
    { suppressAuthRedirect: true },
  );
}

export async function fetchRecipeTimelineEvents(page = 1, perPage = 24) {
  return await apiClient.get<PaginationData<RecipeTimelineEventOut>>(`/api/recipes/timeline/events${toQuery({ page, perPage })}`);
}

export async function fetchRecipeSuggestions(params: {
  foods?: string[];
  tools?: string[];
  maxMissingFoods?: number;
  maxMissingTools?: number;
  includeFoodsOnHand?: boolean;
  includeToolsOnHand?: boolean;
  limit?: number;
}) {
  return await apiClient.get<RecipeSuggestionResponse>(`/api/recipes/suggestions${toQuery({
    foods: params.foods,
    tools: params.tools,
    maxMissingFoods: params.maxMissingFoods ?? 3,
    maxMissingTools: params.maxMissingTools ?? 2,
    includeFoodsOnHand: params.includeFoodsOnHand ?? false,
    includeToolsOnHand: params.includeToolsOnHand ?? false,
    limit: params.limit ?? 12,
  })}`);
}

export async function fetchCategories() {
  return await apiClient.get<PaginationData<RecipeCategory>>(`/api/organizers/categories${toQuery({ page: 1, perPage: 200 })}`);
}

export async function fetchCategoryBySlug(slug: string) {
  return await apiClient.get<RecipeCategoryResponse>(`/api/organizers/categories/slug/${slug}`);
}

export async function fetchTags() {
  return await apiClient.get<PaginationData<RecipeTag>>(`/api/organizers/tags${toQuery({ page: 1, perPage: 200 })}`);
}

export async function fetchTagBySlug(slug: string) {
  return await apiClient.get<RecipeTagResponse>(`/api/organizers/tags/slug/${slug}`);
}

export async function fetchTools() {
  return await apiClient.get<PaginationData<RecipeTool>>(`/api/organizers/tools${toQuery({ page: 1, perPage: 200 })}`);
}

export async function fetchToolBySlug(slug: string) {
  return await apiClient.get<RecipeToolResponse>(`/api/organizers/tools/slug/${slug}`);
}

export async function fetchFoods() {
  return await apiClient.get<PaginationData<IngredientFood>>(`/api/foods${toQuery({ page: 1, perPage: 200 })}`);
}

export async function fetchCookbooks() {
  return await apiClient.get<PaginationData<ReadCookBook>>(`/api/households/cookbooks${toQuery({ page: 1, perPage: 200 })}`);
}

export async function fetchCookbook(id: string) {
  return await apiClient.get<ReadCookBook>(`/api/households/cookbooks/${id}`);
}

export async function createRecipe(name: string) {
  return await apiClient.post<Recipe>("/api/recipes", { name });
}

export async function updateRecipe(slug: string, payload: UpdateRecipePayload) {
  return await apiClient.patch<Recipe>(`/api/recipes/${slug}`, payload);
}

export async function createRecipeFromUrl(url: string, includeTags = true, includeCategories = true) {
  return await apiClient.post<string>("/api/recipes/create-url", {
    url,
    includeTags,
    includeCategories,
  });
}

export async function createRecipesFromUrls(urls: string[], includeTags = true, includeCategories = true) {
  return await apiClient.post<Array<{ url: string; success: boolean; slug?: string; detail?: string }>>(
    "/api/recipes/create/url/bulk",
    { urls, includeTags, includeCategories },
  );
}

async function readSseSlug(path: string, payload: ScrapeRecipeData | { url: string; includeTags?: boolean; includeCategories?: boolean }, onProgress?: (message: string) => void) {
  const response = await fetch(apiClient.resolvePath(path), {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(typeof document !== "undefined" && document.cookie.includes("mealie.access_token=")
        ? {
            Authorization: `Bearer ${decodeURIComponent(document.cookie.split("; ").find(entry => entry.startsWith("mealie.access_token="))?.split("=").slice(1).join("=") ?? "")}`,
          }
        : {}),
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok || !response.body) {
    throw new Error(await response.text() || "Import failed");
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";
  let slug: string | null = null;

  while (true) {
    const result = await reader.read();
    if (result.done) break;
    buffer += decoder.decode(result.value, { stream: true });

    const chunks = buffer.split("\n\n");
    buffer = chunks.pop() ?? "";

    for (const chunk of chunks) {
      const event = chunk.match(/^event: (.+)$/m)?.[1];
      const data = chunk.match(/^data: (.+)$/m)?.[1];
      if (!event || !data) continue;
      const parsed = JSON.parse(data) as { message?: string; slug?: string };

      if (event === "progress" && parsed.message) {
        onProgress?.(parsed.message);
      }

      if (event === "done" && parsed.slug) {
        slug = parsed.slug;
      }

      if (event === "error") {
        throw new Error(parsed.message || "Import failed");
      }
    }
  }

  if (!slug) {
    throw new Error("Import finished without a recipe slug");
  }

  return slug;
}

export async function createRecipeFromHtmlOrJson(payload: ScrapeRecipeData, onProgress?: (message: string) => void) {
  return await readSseSlug("/api/recipes/create/html-or-json/stream", payload, onProgress);
}

export async function importRecipesZip(file: File) {
  const formData = new FormData();
  formData.append("file", file);
  return await apiClient.post<{ imported: number }>("/api/recipes/create/zip", formData);
}

export async function attemptImageImport(file: File) {
  const formData = new FormData();
  formData.append("image", file);
  return await apiClient.post<{ detail: string }>("/api/recipes/create-image-ocr", formData);
}

export async function testRecipeScrapeUrl(url: string) {
  return await apiClient.get<Partial<Recipe>>(`/api/recipes/test-scrape-url${toQuery({ url })}`);
}

export async function fetchRecipeComments(slug: string) {
  return await apiClient.get<RecipeCommentOut[]>(`/api/recipes/${slug}/comments`);
}

export async function addRecipeComment(slug: string, text: string) {
  return await apiClient.post<RecipeCommentOut>(`/api/recipes/${slug}/comments`, { text });
}

export async function uploadRecipeImage(slug: string, file: File) {
  const formData = new FormData();
  formData.append("image", file);
  return await apiClient.put<UpdateImageResponse>(`/api/recipes/${slug}/image`, formData);
}

export async function uploadRecipeAsset(slug: string, file: File) {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("name", file.name.replace(/\.[^.]+$/, ""));
  formData.append("icon", "mdi-file");
  return await apiClient.post<RecipeAsset>(`/api/recipes/${slug}/assets`, formData);
}

export async function fetchRecipeShareTokens(slug: string) {
  return await apiClient.get<ShareTokenResponse[]>(`/api/recipes/${slug}/share`);
}

export async function createRecipeShareToken(slug: string) {
  return await apiClient.post<ShareTokenResponse>(`/api/recipes/${slug}/share`, {});
}

export function recipeShareZipUrl(tokenId: string) {
  return apiClient.resolvePath(`/api/recipes/shared/${tokenId}/zip`);
}

export async function duplicateRecipe(slug: string, name?: string) {
  return await apiClient.post<Recipe>(`/api/recipes/${slug}/duplicate`, { name });
}

export async function reimportRecipe(slug: string) {
  return await apiClient.post<Recipe>(`/api/recipes/${slug}/reimport`, {});
}

export async function deleteRecipe(slug: string) {
  return await apiClient.delete<Recipe>(`/api/recipes/${slug}`);
}

export async function fetchSelfRatings() {
  return await apiClient.get<UserRatingSummary[]>("/api/users/self/ratings");
}

export async function setRecipeRating(user: PrivateUser, slug: string, rating: number | null, isFavorite: boolean | null) {
  return await apiClient.post<void>(`/api/users/${user.id}/ratings/${slug}`, { rating, isFavorite });
}

export async function updateLastMade(slug: string, timestamp: string) {
  return await apiClient.patch(`/api/recipes/${slug}/last-made`, { timestamp });
}

export async function fetchSharedToken(tokenId: string) {
  return await apiClient.get<ShareTokenResponse>(`/api/recipes/shared/${tokenId}`, {
    suppressAuthRedirect: true,
  });
}

export async function resolveSharedRecipe(groupSlug: string, tokenId: string) {
  const token = await fetchSharedToken(tokenId);
  const firstPage = await fetchPublicRecipes(groupSlug, { page: 1, perPage: 200, orderBy: "name", orderDirection: "asc" });

  const pages = firstPage.total_pages ?? 1;
  const pagesToSearch = [firstPage];
  for (let page = 2; page <= pages; page += 1) {
    pagesToSearch.push(await fetchPublicRecipes(groupSlug, { page, perPage: 200, orderBy: "name", orderDirection: "asc" }));
  }

  const matched = pagesToSearch.flatMap(page => page.items).find(recipe => recipe.id === token.recipeId);
  if (!matched?.slug) {
    throw new Error("Unable to resolve shared recipe");
  }

  return await fetchPublicRecipe(groupSlug, matched.slug);
}
