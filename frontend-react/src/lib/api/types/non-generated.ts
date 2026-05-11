export interface ApiRequestConfig {
  headers?: Record<string, string>;
  params?: Record<string, string | number | boolean | null | undefined>;
  [key: string]: unknown;
}

export interface ApiResponse<T> {
  data: T;
  status: number;
}

export type NoUndefinedField<T> = { [P in keyof T]-?: NoUndefinedField<NonNullable<T[P]>> };

export interface RequestResponse<T> {
  response: ApiResponse<T> | null;
  data: T | null;
  error: unknown;
}

export interface ApiRequestInstance {
  get<T>(url: string, data?: unknown, config?: ApiRequestConfig): Promise<RequestResponse<T>>;
  post<T>(url: string, data: unknown, config?: ApiRequestConfig): Promise<RequestResponse<T>>;
  put<T, U = T>(url: string, data: U, config?: ApiRequestConfig): Promise<RequestResponse<T>>;
  patch<T, U = Partial<T>>(url: string, data: U, config?: ApiRequestConfig): Promise<RequestResponse<T>>;
  delete<T>(url: string, config?: ApiRequestConfig): Promise<RequestResponse<T>>;
}

export interface PaginationData<T> {
  page: number;
  per_page: number;
  total: number;
  total_pages: number;
  items: T[];
}

export type RecipeOrganizer
  = | "categories"
    | "tags"
    | "tools"
    | "foods"
    | "households"
    | "users";

export enum Organizer {
  Category = "categories",
  Tag = "tags",
  Tool = "tools",
  Food = "foods",
  Household = "households",
  User = "users",
}

export enum SSEDataEventStatus {
  Progress = "progress",
  Done = "done",
  Error = "error",
}

export type PlaceholderKeyword = "$NOW";
export type RelationalKeyword = "IS" | "IS NOT" | "IN" | "NOT IN" | "CONTAINS ALL" | "LIKE" | "NOT LIKE";
export type LogicalOperator = "AND" | "OR";
export type RelationalOperator = "=" | "<>" | ">" | "<" | ">=" | "<=";

export interface QueryFilterJSON {
  parts?: QueryFilterJSONPart[];
}

export interface QueryFilterJSONPart {
  leftParenthesis?: string | null;
  rightParenthesis?: string | null;
  logicalOperator?: LogicalOperator | null;
  attributeName?: string | null;
  relationalOperator?: RelationalKeyword | RelationalOperator | null;
  value?: string | string[] | null;
}

export type StandardizedUnitType
  = | "fluid_ounce"
    | "cup"
    | "ounce"
    | "pound"
    | "milliliter"
    | "liter"
    | "gram"
    | "kilogram";
