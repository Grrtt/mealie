import type { ReadPlanEntry } from "@/lib/api/contracts";
import { addRecipesToShoppingList, collectMealPlanRecipeRefs } from "@/features/shopping/fromRecipe";

export function collectPlannedRecipes(entries: ReadPlanEntry[]) {
  return collectMealPlanRecipeRefs(entries.filter(entry => Boolean(entry.recipeId)));
}

export async function addMealPlanToShoppingList(listId: string, entries: ReadPlanEntry[]) {
  const recipes = collectPlannedRecipes(entries);
  if (!recipes.length) {
    throw new Error("There are no recipe-backed meal plan entries in this range.");
  }

  return await addRecipesToShoppingList(listId, recipes);
}
