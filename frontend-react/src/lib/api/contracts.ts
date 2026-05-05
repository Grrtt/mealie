export type {
  AppInfo,
  AppStartupInfo,
} from "../../../../frontend/app/lib/api/types/admin";
export type {
  ForgotPassword,
  PrivateUser,
  ResetPassword,
  Token,
} from "../../../../frontend/app/lib/api/types/user";
export type {
  UserRatingOut,
  UserRatingSummary,
} from "../../../../frontend/app/lib/api/types/user";
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
} from "../../../../frontend/app/lib/api/types/recipe";
export type {
  ReadCookBook,
} from "../../../../frontend/app/lib/api/types/cookbook";
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
} from "../../../../frontend/app/lib/api/types/meal-plan";
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
} from "../../../../frontend/app/lib/api/types/household";

export type RegistrationPayload = {
  email: string;
  username: string;
  fullName: string;
  password: string;
  groupToken?: string;
};

export type ActivityKey = "recipes" | "mealplanner" | "shopping_list";

export type PaginationData<T> = {
  page: number;
  per_page: number;
  total: number;
  total_pages: number;
  items: T[];
};
