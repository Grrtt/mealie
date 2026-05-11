export type {
  AppInfo,
  AppStartupInfo,
} from "@/lib/api/types/admin";
export type {
  ForgotPassword,
  PrivateUser,
  ResetPassword,
  Token,
} from "@/lib/api/types/user";
export type {
  UserRatingOut,
  UserRatingSummary,
} from "@/lib/api/types/user";
export type {
  IngredientFood,
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
} from "@/lib/api/types/recipe";
export type {
  ReadCookBook,
} from "@/lib/api/types/cookbook";
export type {
  CreatePlanEntry,
  CreateRandomEntry,
  PlanEntryType,
  PlanRulesCreate,
  PlanRulesDay,
  PlanRulesOut,
  PlanRulesType,
  ReadPlanEntry,
  UpdatePlanEntry,
} from "@/lib/api/types/meal-plan";
export type {
  GroupRecipeActionOut,
  GroupRecipeActionType,
  ReadHouseholdPreferences,
  ShoppingListAddRecipeParamsBulk,
  ShoppingListCreate,
  ShoppingListItemCreate,
  ShoppingListItemOut,
  ShoppingListItemUpdateBulk,
  ShoppingListMultiPurposeLabelOut,
  ShoppingListMultiPurposeLabelUpdate,
  ShoppingListOut,
  ShoppingListSummary,
  ShoppingListUpdate,
} from "@/lib/api/types/household";

export type RegistrationPayload = {
  email: string;
  username: string;
  fullName: string;
  password: string;
  groupToken?: string;
  invite?: string;
};

export type RegistrationInvitePrefill = {
  email: string;
};

export type ActivityKey = "recipes" | "mealplanner" | "shopping_list";

export type PaginationData<T> = {
  page: number;
  per_page: number;
  total: number;
  total_pages: number;
  items: T[];
};
