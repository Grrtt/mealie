import { BaseAPI } from "../base/base-clients";
import type { PaginationData } from "~/lib/api/types/non-generated";

const prefix = "/api";

const routes = {
  base: `${prefix}/admin/ingredient-aliases`,
  unresolved: `${prefix}/admin/ingredient-aliases/unresolved`,
  item: (id: string) => `${prefix}/admin/ingredient-aliases/${id}`,
};

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

export class AdminIngredientAliasesApi extends BaseAPI {
  async getUnresolved(page = 1, perPage = 50, search?: string) {
    const params: Record<string, string | number> = { page, perPage };
    if (search) {
      params.search = search;
    }
    return await this.requests.get<PaginationData<UnresolvedIngredient>>(routes.unresolved, params);
  }

  async getAll(page = 1, perPage = 50) {
    return await this.requests.get<PaginationData<IngredientAlias>>(routes.base, { page, perPage });
  }

  async create(rawText: string, foodId: string, backfillRecipes = true) {
    const payload: CreateIngredientAliasPayload = { rawText, foodId, backfillRecipes };
    return await this.requests.post<IngredientAlias>(routes.base, payload);
  }

  async delete(id: string) {
    return await this.requests.delete<void>(routes.item(id));
  }
}
