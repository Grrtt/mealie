import { queryOptions } from "@tanstack/react-query";
import { apiClient } from "@/lib/api/client";
import type {
  PaginationData,
  ReadPlanEntry,
  Recipe,
  ShoppingListCreate,
  ShoppingListItemCreate,
  ShoppingListItemOut,
  ShoppingListOut,
  ShoppingListSummary,
  ShoppingListUpdate,
} from "@/lib/api/contracts";

type DotnetShoppingListItemResponse = {
  id: string;
  note?: string | null;
  isFood: boolean;
  checked: boolean;
  disableAmount: boolean;
  quantity?: number | null;
  shoppingListId: string;
  unitId?: string | null;
  foodId?: string | null;
  labelId?: string | null;
  position: number;
  unitName?: string | null;
  foodName?: string | null;
  createdAt?: string | null;
  updateAt?: string | null;
};

type DotnetShoppingListResponse = {
  id: string;
  name?: string | null;
  groupId: string;
  householdId: string;
  createdAt?: string | null;
  updateAt?: string | null;
  items?: DotnetShoppingListItemResponse[];
};

type DotnetShoppingListSummaryResponse = {
  id: string;
  name?: string | null;
  groupId: string;
  householdId: string;
  userId: string;
  recipeReferenceCount?: number;
  createdAt?: string | null;
  updateAt?: string | null;
};

function toQuery(params: Record<string, string | number | boolean | undefined | null>) {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value == null || value === "") continue;
    search.set(key, String(value));
  }

  const query = search.toString();
  return query ? `?${query}` : "";
}

function isDotnetShoppingListResponse(raw: ShoppingListOut | DotnetShoppingListResponse): raw is DotnetShoppingListResponse {
  return "items" in raw || "updateAt" in raw;
}

function normalizeFood(raw: DotnetShoppingListItemResponse): ShoppingListItemOut["food"] {
  if (!raw.foodId || !raw.foodName) return null;

  return {
    id: raw.foodId,
    name: raw.foodName,
  };
}

function normalizeUnit(raw: DotnetShoppingListItemResponse): ShoppingListItemOut["unit"] {
  if (!raw.unitId || !raw.unitName) return null;

  return {
    id: raw.unitId,
    name: raw.unitName,
  };
}

function normalizeShoppingList(raw: ShoppingListOut | DotnetShoppingListResponse): ShoppingListOut {
  if (!isDotnetShoppingListResponse(raw)) {
    return raw;
  }

  return {
    id: raw.id,
    name: raw.name,
    groupId: raw.groupId,
    householdId: raw.householdId,
    userId: "",
    createdAt: raw.createdAt,
    updatedAt: raw.updateAt,
    listItems: (raw.items ?? []).map((item): ShoppingListItemOut => ({
      id: item.id,
      shoppingListId: item.shoppingListId,
      groupId: raw.groupId,
      householdId: raw.householdId,
      checked: item.checked,
      position: item.position,
      note: item.note,
      display: item.foodName ?? item.note ?? "Shopping list item",
      quantity: item.quantity ?? undefined,
      foodId: item.foodId,
      unitId: item.unitId,
      labelId: item.labelId,
      food: normalizeFood(item),
      unit: normalizeUnit(item),
      createdAt: item.createdAt,
      updatedAt: item.updateAt,
    })),
    recipeReferences: [],
    labelSettings: [],
  };
}

function isDotnetShoppingListSummary(raw: ShoppingListSummary | DotnetShoppingListSummaryResponse): raw is DotnetShoppingListSummaryResponse {
  return "recipeReferenceCount" in raw || "updateAt" in raw;
}

function normalizeShoppingListSummary(raw: ShoppingListSummary | DotnetShoppingListSummaryResponse): ShoppingListSummary {
  if (!isDotnetShoppingListSummary(raw)) {
    return raw;
  }

  return {
    id: raw.id,
    name: raw.name,
    groupId: raw.groupId,
    householdId: raw.householdId,
    userId: raw.userId,
    createdAt: raw.createdAt,
    updatedAt: raw.updateAt,
    recipeReferences: Array.from({ length: raw.recipeReferenceCount ?? 0 }, () => ({
      id: "",
      shoppingListId: raw.id,
      recipeId: "",
      recipeQuantity: 0,
      recipe: {},
    })),
    labelSettings: [],
  };
}

export async function fetchShoppingLists() {
  const response = await apiClient.get<PaginationData<ShoppingListSummary | DotnetShoppingListSummaryResponse>>(
    `/api/households/shopping/lists${toQuery({
      page: 1,
      perPage: -1,
      orderBy: "name",
      orderDirection: "asc",
    })}`,
  );

  return {
    ...response,
    items: response.items.map(normalizeShoppingListSummary),
  };
}

export async function fetchShoppingList(id: string) {
  const response = await apiClient.get<ShoppingListOut | DotnetShoppingListResponse>(`/api/households/shopping/lists/${id}`);
  return normalizeShoppingList(response);
}

export const shoppingListsQueryOptions = queryOptions({
  queryKey: ["shopping-lists"],
  queryFn: fetchShoppingLists,
});

export function shoppingListQueryOptions(id: string) {
  return queryOptions({
    queryKey: ["shopping-list", id],
    queryFn: async () => await fetchShoppingList(id),
  });
}

export async function createShoppingList(payload: ShoppingListCreate) {
  const response = await apiClient.post<ShoppingListOut | DotnetShoppingListResponse>("/api/households/shopping/lists", payload);
  return normalizeShoppingList(response);
}

export async function createShoppingListWithRecipe(payload: { name: string; recipeId: string; recipeScale?: number }) {
  const response = await apiClient.post<ShoppingListOut | DotnetShoppingListResponse>("/api/households/shopping/lists/with-recipe", {
    name: payload.name,
    recipeId: payload.recipeId,
    recipeIncrementQuantity: payload.recipeScale ?? 1,
  });
  return normalizeShoppingList(response);
}

export async function updateShoppingList(id: string, payload: ShoppingListUpdate) {
  const response = await apiClient.put<ShoppingListOut | DotnetShoppingListResponse>(`/api/households/shopping/lists/${id}`, payload);
  return normalizeShoppingList(response);
}

export async function deleteShoppingList(id: string) {
  return await apiClient.delete<void>(`/api/households/shopping/lists/${id}`);
}

export async function createShoppingListItems(items: ShoppingListItemCreate[]) {
  return await apiClient.post<ShoppingListItemOut[]>("/api/households/shopping/items/create-bulk", items);
}

export async function updateShoppingListItems(items: ShoppingListItemOut[]) {
  return await apiClient.put<ShoppingListItemOut[]>("/api/households/shopping/items", items);
}

export async function deleteShoppingListItems(items: Array<Pick<ShoppingListItemOut, "id">>) {
  const ids = items.map(item => `ids=${encodeURIComponent(item.id)}`).join("&");
  return await apiClient.delete<void>(`/api/households/shopping/items?${ids}`);
}

export async function addRecipesToShoppingList(listId: string, recipes: Array<{ recipeId: string; recipeScale?: number }>) {
  const response = await apiClient.post<ShoppingListOut | DotnetShoppingListResponse>(
    `/api/households/shopping/lists/${listId}/recipe`,
    recipes.map(recipe => ({
      recipeId: recipe.recipeId,
      recipeIncrementQuantity: recipe.recipeScale ?? 1,
    })),
  );
  return normalizeShoppingList(response);
}

export async function removeRecipeFromShoppingList(listId: string, recipeId: string, recipeDecrementQuantity = 1) {
  const response = await apiClient.post<ShoppingListOut | DotnetShoppingListResponse>(
    `/api/households/shopping/lists/${listId}/recipe/${recipeId}/delete`,
    { recipeDecrementQuantity },
  );
  return normalizeShoppingList(response);
}

export async function createOrSelectShoppingList(listId: string | null, newListName: string) {
  if (listId) return listId;

  const created = await createShoppingList({ name: newListName.trim() });
  return created.id;
}

export async function addRecipeToShoppingList(listId: string, recipe: Pick<Recipe, "id">, recipeScale = 1) {
  if (!recipe.id) {
    throw new Error("Recipe is missing an id");
  }

  return await addRecipesToShoppingList(listId, [{ recipeId: recipe.id, recipeScale }]);
}

export function collectMealPlanRecipeRefs(entries: ReadPlanEntry[]) {
  const counts = new Map<string, number>();

  entries.forEach((entry) => {
    if (!entry.recipeId) return;
    counts.set(entry.recipeId, (counts.get(entry.recipeId) ?? 0) + 1);
  });

  return Array.from(counts.entries()).map(([recipeId, recipeScale]) => ({ recipeId, recipeScale }));
}
