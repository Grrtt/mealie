import { BaseCRUDAPI } from "../base/base-clients";
import type { CreatePlanEntry, CreateRandomEntry, ReadPlanEntry, UpdatePlanEntry } from "~/lib/api/types/meal-plan";

const prefix = "/api";

const routes = {
  mealplan: `${prefix}/households/mealplans`,
  random: `${prefix}/households/mealplans/random`,
  fillDay: `${prefix}/households/mealplans/fill-day`,
  fillWeek: `${prefix}/households/mealplans/fill-week`,
  mealplanId: (id: string | number) => `${prefix}/households/mealplans/${id}`,
};

export class MealPlanAPI extends BaseCRUDAPI<CreatePlanEntry, ReadPlanEntry, UpdatePlanEntry> {
  baseRoute = routes.mealplan;
  itemRoute = routes.mealplanId;

  async setRandom(payload: CreateRandomEntry) {
    return await this.requests.post<ReadPlanEntry>(routes.random, payload);
  }

  async fillDay(payload: { date: string; entryTypes: string[] }) {
    return await this.requests.post<ReadPlanEntry[]>(routes.fillDay, payload);
  }

  async fillWeek(payload: { startDate: string; endDate: string; entryTypes: string[] }) {
    return await this.requests.post<ReadPlanEntry[]>(routes.fillWeek, payload);
  }
}
