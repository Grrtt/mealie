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

function toQuery(params: Record<string, string | number | boolean | undefined | null>) {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value == null || value === "") continue;
    search.set(key, String(value));
  }

  const query = search.toString();
  return query ? `?${query}` : "";
}

export async function fetchShoppingLists() {
  return await apiClient.get<PaginationData<ShoppingListSummary>>(
    `/api/households/shopping/lists${toQuery({
      page: 1,
      perPage: -1,
      orderBy: "name",
      orderDirection: "asc",
    })}`,
  );
}

export async function fetchShoppingList(id: string) {
  return await apiClient.get<ShoppingListOut>(`/api/households/shopping/lists/${id}`);
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
  return await apiClient.post<ShoppingListOut>("/api/households/shopping/lists", payload);
}

export async function updateShoppingList(id: string, payload: ShoppingListUpdate) {
  return await apiClient.put<ShoppingListOut>(`/api/households/shopping/lists/${id}`, payload);
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
  return await apiClient.post<ShoppingListOut>(
    `/api/households/shopping/lists/${listId}/recipe`,
    recipes.map(recipe => ({
      recipeId: recipe.recipeId,
      recipeIncrementQuantity: recipe.recipeScale ?? 1,
    })),
  );
}

export async function removeRecipeFromShoppingList(listId: string, recipeId: string, recipeDecrementQuantity = 1) {
  return await apiClient.post<ShoppingListOut>(
    `/api/households/shopping/lists/${listId}/recipe/${recipeId}/delete`,
    { recipeDecrementQuantity },
  );
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
