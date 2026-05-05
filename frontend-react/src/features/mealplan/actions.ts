import { addDays, format, parseISO } from "date-fns";
import { apiClient } from "@/lib/api/client";
import type {
  CreatePlanEntry,
  PaginationData,
  PlanEntryType,
  PlanRulesCreate,
  PlanRulesOut,
  ReadHouseholdPreferences,
  ReadPlanEntry,
  UpdatePlanEntry,
} from "@/lib/api/contracts";
import { fetchRecipes } from "@/features/recipes/api";

export const mealPlanEntryTypes: PlanEntryType[] = [
  "breakfast",
  "lunch",
  "dinner",
  "side",
  "snack",
  "drink",
  "dessert",
];

function toQuery(params: Record<string, string | number | boolean | undefined | null>) {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value == null || value === "") continue;
    search.set(key, String(value));
  }

  const query = search.toString();
  return query ? `?${query}` : "";
}

export function formatMealPlanDate(value: Date | string) {
  return format(typeof value === "string" ? parseISO(value) : value, "yyyy-MM-dd");
}

export function shiftMealPlanDate(value: string, days: number) {
  return format(addDays(parseISO(value), days), "yyyy-MM-dd");
}

export function defaultMealPlanRange() {
  const today = new Date();
  return {
    start: format(today, "yyyy-MM-dd"),
    end: format(addDays(today, 6), "yyyy-MM-dd"),
  };
}

export async function fetchMealPlans(start: string, end: string) {
  const response = await apiClient.get<PaginationData<ReadPlanEntry>>(
    `/api/households/mealplans${toQuery({
      page: 1,
      perPage: -1,
      start_date: start,
      end_date: end,
    })}`,
  );

  return response.items ?? [];
}

export async function createMealPlanEntry(payload: CreatePlanEntry) {
  return await apiClient.post<ReadPlanEntry>("/api/households/mealplans", payload);
}

export async function updateMealPlanEntry(payload: UpdatePlanEntry) {
  return await apiClient.put<ReadPlanEntry>(`/api/households/mealplans/${payload.id}`, payload);
}

export async function deleteMealPlanEntry(id: number) {
  return await apiClient.delete<void>(`/api/households/mealplans/${id}`);
}

export async function randomMealPlanEntry(payload: { date: string; entryType: PlanEntryType }) {
  return await apiClient.post<ReadPlanEntry>("/api/households/mealplans/random", payload);
}

export async function fillMealPlanDay(payload: { date: string; entryTypes: string[] }) {
  return await apiClient.post<ReadPlanEntry[]>("/api/households/mealplans/fill-day", payload);
}

export async function fillMealPlanWeek(payload: { startDate: string; endDate: string }) {
  return await apiClient.post<ReadPlanEntry[]>("/api/households/mealplans/fill-week", payload);
}

export async function fetchPlanningRules() {
  const response = await apiClient.get<PaginationData<PlanRulesOut>>(
    `/api/households/mealplans/rules${toQuery({ page: 1, perPage: -1 })}`,
  );

  return response.items ?? [];
}

export async function createPlanningRule(payload: PlanRulesCreate) {
  return await apiClient.post<PlanRulesOut>("/api/households/mealplans/rules", payload);
}

export async function updatePlanningRule(payload: PlanRulesOut) {
  return await apiClient.put<PlanRulesOut>(`/api/households/mealplans/rules/${payload.id}`, payload);
}

export async function deletePlanningRule(id: string) {
  return await apiClient.delete<void>(`/api/households/mealplans/rules/${id}`);
}

export async function searchPlannerRecipes(search: string) {
  return await fetchRecipes({
    search,
    page: 1,
    perPage: 25,
    orderBy: "name",
    orderDirection: "asc",
  });
}

export async function fetchHouseholdPreferences() {
  return await apiClient.get<ReadHouseholdPreferences>("/api/households/preferences");
}
