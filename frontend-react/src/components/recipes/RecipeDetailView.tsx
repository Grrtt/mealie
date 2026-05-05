import { useEffect, useMemo, useRef, useState } from "react";
import AddRoundedIcon from "@mui/icons-material/AddRounded";
import EditRoundedIcon from "@mui/icons-material/EditRounded";
import FavoriteBorderRoundedIcon from "@mui/icons-material/FavoriteBorderRounded";
import FavoriteRoundedIcon from "@mui/icons-material/FavoriteRounded";
import HistoryRoundedIcon from "@mui/icons-material/HistoryRounded";
import LocalDiningRoundedIcon from "@mui/icons-material/LocalDiningRounded";
import MoreVertRoundedIcon from "@mui/icons-material/MoreVertRounded";
import RemoveRoundedIcon from "@mui/icons-material/RemoveRounded";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Checkbox from "@mui/material/Checkbox";
import Chip from "@mui/material/Chip";
import Divider from "@mui/material/Divider";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import FormControlLabel from "@mui/material/FormControlLabel";
import Grid from "@mui/material/Grid";
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemText from "@mui/material/ListItemText";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Rating from "@mui/material/Rating";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import Tooltip from "@mui/material/Tooltip";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import type { GroupRecipeActionOut, PlanEntryType, PrivateUser, Recipe, ShoppingListSummary } from "@/lib/api/contracts";
import { createMealPlanEntry, formatMealPlanDate, mealPlanEntryTypes } from "@/features/mealplan/actions";
import {
  createRecipeShareToken,
  deleteRecipe,
  duplicateRecipe,
  fetchSelfRatings,
  recipeImageUrl,
  recipeShareZipUrl,
  reimportRecipe,
  setRecipeRating,
} from "@/features/recipes/api";
import { fetchGroupRecipeActions, triggerGroupRecipeAction } from "@/features/settings/api";
import { addRecipeToShoppingList, createOrSelectShoppingList, fetchShoppingLists } from "@/features/shopping/fromRecipe";
import { apiClient } from "@/lib/api/client";

type Props = {
  currentUser: PrivateUser | null;
  groupSlug: string;
  onRecipeRefresh: () => Promise<void> | void;
  recipe: Recipe;
  onEdit: () => void;
};

type RecipeIngredient = NonNullable<Recipe["recipeIngredient"]>[number];
type RecipeStep = NonNullable<Recipe["recipeInstructions"]>[number];
type RecipeTool = NonNullable<Recipe["tools"]>[number];
type PrintPreferences = {
  showImage: boolean;
  showDescription: boolean;
  showNotes: boolean;
};

type WakeLockSentinelLike = {
  released: boolean;
  release: () => Promise<void>;
  addEventListener?: (type: "release", listener: () => void) => void;
};

type WakeLockManagerLike = {
  request: (type: "screen") => Promise<WakeLockSentinelLike>;
};

function wakeLockManager(): WakeLockManagerLike | undefined {
  return (navigator as Navigator & { wakeLock?: WakeLockManagerLike }).wakeLock;
}

function getErrorMessage(error: unknown) {
  if (error instanceof Error) return error.message;
  return "Unable to keep the screen awake on this device.";
}

function formatQuantity(quantity: number) {
  return new Intl.NumberFormat("en-US", {
    maximumFractionDigits: 2,
  }).format(Number(quantity.toFixed(2)));
}

function ingredientKey(ingredient: RecipeIngredient, index: number) {
  return ingredient.referenceId ?? ingredient.originalText ?? ingredient.food?.id ?? `${ingredient.title ?? "ingredient"}-${index}`;
}

function stepKey(step: RecipeStep, index: number) {
  return step.id ?? `${step.title ?? step.summary ?? "step"}-${index}`;
}

function toolKey(tool: RecipeTool, index: number) {
  return tool.id ?? `${tool.slug}-${index}`;
}

function ingredientPrimaryText(ingredient: RecipeIngredient, scale: number) {
  const quantity = ingredient.quantity != null ? formatQuantity(ingredient.quantity * scale) : null;
  const unit = ingredient.unit?.useAbbreviation
    ? ingredient.unit.abbreviation ?? ingredient.unit.name
    : ingredient.unit?.abbreviation ?? ingredient.unit?.name;
  const name = ingredient.referencedRecipe?.name ?? ingredient.food?.name;

  const parts = [quantity, unit, name].filter(Boolean);
  if (parts.length > 0) {
    return parts.join(" ");
  }

  return ingredient.originalText?.trim() || ingredient.note?.trim() || "Ingredient";
}

function ingredientSecondaryText(ingredient: RecipeIngredient) {
  if (ingredient.referencedRecipe || ingredient.food) {
    return ingredient.note?.trim() || null;
  }

  return null;
}

function normalizeScaleInput(value: string) {
  const parsed = Number(value);
  if (!Number.isFinite(parsed)) return 1;
  return Math.min(10, Math.max(0.25, parsed));
}

function infoMetricSx() {
  return {
    minWidth: 128,
    px: 2,
    py: 1.5,
    border: 1,
    borderColor: "divider",
    borderRadius: 2,
    textAlign: "center",
    bgcolor: "background.paper",
  } as const;
}

function buildShareUrl(groupSlug: string, tokenId: string) {
  return new URL(apiClient.resolvePath(`/g/${groupSlug}/shared/r/${tokenId}`), window.location.origin).toString();
}

function parseRecipeActionUrl(url: string, recipe: Recipe, recipeScale: number) {
  const recipeServings = (recipe.recipeServings || 1) * recipeScale;
  const recipeYieldQuantity = (recipe.recipeYieldQuantity || 1) * recipeScale;

  return url
    .replace("${url}", window.location.href)
    .replace("${id}", recipe.id || "")
    .replace("${slug}", recipe.slug || "")
    .replace("${servings}", recipeServings.toString())
    .replace("${yieldQuantity}", recipeYieldQuantity.toString())
    .replace("${yieldText}", recipe.recipeYield || "");
}

function useScreenWakeLock(enabled: boolean) {
  const manager = wakeLockManager();
  const sentinelRef = useRef<WakeLockSentinelLike | null>(null);
  const [isActive, setIsActive] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function releaseLock() {
      if (!sentinelRef.current) {
        setIsActive(false);
        return;
      }

      try {
        await sentinelRef.current.release();
      } catch (releaseError) {
        setError(getErrorMessage(releaseError));
      } finally {
        sentinelRef.current = null;
        setIsActive(false);
      }
    }

    if (!manager) {
      setIsActive(false);
      return;
    }

    let cancelled = false;

    async function requestLock() {
      if (!enabled || document.visibilityState === "hidden" || !manager) return;

      try {
        const sentinel = await manager.request("screen");
        if (cancelled) {
          await sentinel.release();
          return;
        }

        sentinelRef.current = sentinel;
        setIsActive(!sentinel.released);
        setError(null);
        sentinel.addEventListener?.("release", () => {
          sentinelRef.current = null;
          setIsActive(false);
        });
      } catch (requestError) {
        if (!cancelled) {
          setError(getErrorMessage(requestError));
          setIsActive(false);
        }
      }
    }

    const handleVisibilityChange = () => {
      if (document.visibilityState === "visible" && enabled && !sentinelRef.current) {
        void requestLock();
      }
    };

    if (enabled) {
      void requestLock();
    } else {
      void releaseLock();
    }

    document.addEventListener("visibilitychange", handleVisibilityChange);

    return () => {
      cancelled = true;
      document.removeEventListener("visibilitychange", handleVisibilityChange);
      void releaseLock();
    };
  }, [enabled, manager]);

  return {
    isSupported: Boolean(manager),
    isActive,
    error,
  };
}

export function RecipeDetailView({ currentUser, groupSlug, onRecipeRefresh, recipe, onEdit }: Props) {
  const [scaleInput, setScaleInput] = useState("1");
  const [isCookMode, setIsCookMode] = useState(false);
  const [ingredientChecks, setIngredientChecks] = useState<Record<string, boolean>>({});
  const [stepChecks, setStepChecks] = useState<Record<string, boolean>>({});
  const [toolChecks, setToolChecks] = useState<Record<string, boolean>>({});
  const [keepScreenAwake, setKeepScreenAwake] = useState(() => Boolean(wakeLockManager()));
  const [favorite, setFavorite] = useState(false);
  const [userRating, setUserRating] = useState<number | null>(null);
  const [actionStatus, setActionStatus] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [moreAnchor, setMoreAnchor] = useState<HTMLElement | null>(null);
  const [shareDialogOpen, setShareDialogOpen] = useState(false);
  const [shareLink, setShareLink] = useState<string | null>(null);
  const [shareLoading, setShareLoading] = useState(false);
  const [duplicateDialogOpen, setDuplicateDialogOpen] = useState(false);
  const [duplicateName, setDuplicateName] = useState(recipe.name ?? "");
  const [duplicateLoading, setDuplicateLoading] = useState(false);
  const [planDialogOpen, setPlanDialogOpen] = useState(false);
  const [planDate, setPlanDate] = useState(formatMealPlanDate(new Date()));
  const [planType, setPlanType] = useState<PlanEntryType>("dinner");
  const [planLoading, setPlanLoading] = useState(false);
  const [shoppingDialogOpen, setShoppingDialogOpen] = useState(false);
  const [shoppingLists, setShoppingLists] = useState<ShoppingListSummary[]>([]);
  const [selectedListId, setSelectedListId] = useState("");
  const [newListName, setNewListName] = useState("");
  const [shoppingLoading, setShoppingLoading] = useState(false);
  const [printPreferencesOpen, setPrintPreferencesOpen] = useState(false);
  const [printPreferences, setPrintPreferences] = useState<PrintPreferences>({
    showImage: true,
    showDescription: true,
    showNotes: true,
  });
  const [reimportDialogOpen, setReimportDialogOpen] = useState(false);
  const [reimportLoading, setReimportLoading] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteLoading, setDeleteLoading] = useState(false);
  const { isSupported: wakeLockSupported, isActive: wakeLockActive, error: wakeLockError } = useScreenWakeLock(keepScreenAwake);
  const ratingsQuery = useQuery({
    queryKey: ["self-ratings"],
    queryFn: fetchSelfRatings,
    enabled: Boolean(currentUser?.id && recipe.id),
  });
  const recipeActionsQuery = useQuery({
    queryKey: ["group-recipe-actions"],
    queryFn: async () => {
      const response = await fetchGroupRecipeActions();
      return response.items ?? [];
    },
    enabled: Boolean(currentUser?.id),
  });

  useEffect(() => {
    setScaleInput("1");
    setIsCookMode(false);
    setIngredientChecks({});
    setStepChecks({});
    setToolChecks(
      Object.fromEntries((recipe.tools ?? []).map((tool, index) => [toolKey(tool, index), false])),
    );
    setKeepScreenAwake(Boolean(wakeLockManager()));
    setDuplicateName(recipe.name ?? "");
    setShareLink(null);
    setPlanDate(formatMealPlanDate(new Date()));
    setPlanType("dinner");
    setSelectedListId("");
    setNewListName("");
    setPrintPreferences({
      showImage: true,
      showDescription: true,
      showNotes: true,
    });
  }, [recipe.id, recipe.name, recipe.slug, recipe.tools]);

  useEffect(() => {
    const existing = ratingsQuery.data?.find(entry => entry.recipeId === recipe.id);
    setFavorite(Boolean(existing?.isFavorite));
    setUserRating(existing?.rating ?? null);
  }, [ratingsQuery.data, recipe.id]);

  const scale = useMemo(() => normalizeScaleInput(scaleInput), [scaleInput]);
  const imageUrl = recipeImageUrl(recipe.id, typeof recipe.image === "string" ? recipe.image : null);
  const canDelete = Boolean(currentUser && (currentUser.admin || currentUser.id === recipe.userId));
  const canReimport = Boolean(currentUser?.admin && recipe.orgURL);

  const ingredientRows = recipe.recipeIngredient ?? [];
  const steps = recipe.recipeInstructions ?? [];
  const notes = recipe.notes ?? [];
  const categories = recipe.recipeCategory ?? [];
  const tags = recipe.tags ?? [];
  const tools = recipe.tools ?? [];
  const groupRecipeActions = recipeActionsQuery.data ?? [];
  const hasCheckedItems = useMemo(() => {
    return [...Object.values(ingredientChecks), ...Object.values(stepChecks), ...Object.values(toolChecks)].some(Boolean);
  }, [ingredientChecks, stepChecks, toolChecks]);
  const favoriteMutation = useMutation({
    mutationFn: async (nextFavorite: boolean) => {
      if (!currentUser || !recipe.slug) return;
      await setRecipeRating(currentUser, recipe.slug, userRating, nextFavorite);
    },
    onSuccess: async (_, nextFavorite) => {
      setFavorite(nextFavorite);
      setActionStatus(nextFavorite ? "Recipe added to favorites" : "Recipe removed from favorites");
      setActionError(null);
      await ratingsQuery.refetch();
      await onRecipeRefresh();
    },
    onError: favoriteError => {
      setActionError(favoriteError instanceof Error ? favoriteError.message : "Unable to update favorite");
    },
  });

  function resetChecks() {
    setIngredientChecks({});
    setStepChecks({});
    setToolChecks(Object.fromEntries(tools.map((tool, index) => [toolKey(tool, index), false])));
  }

  function closeMoreMenu() {
    setMoreAnchor(null);
  }

  async function createShareLink() {
    if (!recipe.slug) return;

    setShareLoading(true);
    setActionError(null);
    try {
      const token = await createRecipeShareToken(recipe.slug);
      const nextShareLink = buildShareUrl(groupSlug, token.id);
      setShareLink(nextShareLink);
      setActionStatus("Share link created");
    } catch (shareError) {
      setActionError(shareError instanceof Error ? shareError.message : "Unable to create share link");
    } finally {
      setShareLoading(false);
    }
  }

  async function copyShareLink() {
    if (!shareLink || !navigator.clipboard) return;

    try {
      await navigator.clipboard.writeText(shareLink);
      setActionStatus("Share link copied");
      setActionError(null);
    } catch (copyError) {
      setActionError(copyError instanceof Error ? copyError.message : "Unable to copy share link");
    }
  }

  async function startRecipeDownload() {
    if (!recipe.slug) return;

    closeMoreMenu();
    setActionError(null);
    try {
      const token = await createRecipeShareToken(recipe.slug);
      window.location.assign(recipeShareZipUrl(token.id));
      setActionStatus("Recipe download started");
    } catch (downloadError) {
      setActionError(downloadError instanceof Error ? downloadError.message : "Unable to download recipe");
    }
  }

  async function openShoppingListDialog() {
    closeMoreMenu();
    setActionError(null);
    setShoppingLoading(true);
    try {
      const response = await fetchShoppingLists();
      setShoppingLists(response.items ?? []);
      setShoppingDialogOpen(true);
    } catch (shoppingError) {
      setActionError(shoppingError instanceof Error ? shoppingError.message : "Unable to load shopping lists");
    } finally {
      setShoppingLoading(false);
    }
  }

  async function submitShoppingListAction() {
    setShoppingLoading(true);
    setActionError(null);
    try {
      const listId = await createOrSelectShoppingList(selectedListId || null, newListName);

      if (!listId) {
        throw new Error("Choose an existing shopping list or provide a new list name.");
      }

      await addRecipeToShoppingList(listId, recipe, scale);
      setActionStatus("Recipe added to shopping list");
      setShoppingDialogOpen(false);
      setSelectedListId("");
      setNewListName("");
    } catch (shoppingError) {
      setActionError(shoppingError instanceof Error ? shoppingError.message : "Unable to add recipe to shopping list");
    } finally {
      setShoppingLoading(false);
    }
  }

  async function submitMealPlanAction() {
    if (!recipe.id) {
      setActionError("Recipe details are unavailable.");
      return;
    }

    setPlanLoading(true);
    setActionError(null);
    try {
      await createMealPlanEntry({
        date: planDate,
        entryType: planType,
        recipeId: recipe.id,
        title: "",
        text: "",
      });
      setActionStatus("Recipe added to meal plan");
      setPlanDialogOpen(false);
    } catch (planError) {
      setActionError(planError instanceof Error ? planError.message : "Unable to add recipe to meal plan");
    } finally {
      setPlanLoading(false);
    }
  }

  async function submitDuplicateAction() {
    if (!recipe.slug) return;

    setDuplicateLoading(true);
    setActionError(null);
    try {
      const duplicate = await duplicateRecipe(recipe.slug, duplicateName.trim() || undefined);
      if (duplicate.slug) {
        window.location.assign(apiClient.resolvePath(`/g/${groupSlug}/r/${duplicate.slug}`));
      }
    } catch (duplicateError) {
      setActionError(duplicateError instanceof Error ? duplicateError.message : "Unable to duplicate recipe");
    } finally {
      setDuplicateLoading(false);
    }
  }

  async function submitReimportAction() {
    if (!recipe.slug) return;

    setReimportLoading(true);
    setActionError(null);
    try {
      await reimportRecipe(recipe.slug);
      setActionStatus("Recipe reimported");
      setReimportDialogOpen(false);
      await onRecipeRefresh();
    } catch (reimportError) {
      setActionError(reimportError instanceof Error ? reimportError.message : "Unable to reimport recipe");
    } finally {
      setReimportLoading(false);
    }
  }

  async function submitDeleteAction() {
    if (!recipe.slug) return;

    setDeleteLoading(true);
    setActionError(null);
    try {
      await deleteRecipe(recipe.slug);
      window.location.assign(apiClient.resolvePath(`/g/${groupSlug}`));
    } catch (deleteError) {
      setActionError(deleteError instanceof Error ? deleteError.message : "Unable to delete recipe");
    } finally {
      setDeleteLoading(false);
    }
  }

  function printRecipeWithPreferences() {
    const style = document.createElement("style");
    style.setAttribute("data-recipe-print-preferences", "true");
    style.textContent = `
      @media print {
        ${printPreferences.showImage ? "" : '[data-print-role="recipe-image"] { display: none !important; }'}
        ${printPreferences.showDescription ? "" : '[data-print-role="recipe-description"] { display: none !important; }'}
        ${printPreferences.showNotes ? "" : '[data-print-role="recipe-notes"] { display: none !important; }'}
      }
    `;
    document.head.append(style);

    const cleanup = () => {
      style.remove();
    };

    window.addEventListener("afterprint", cleanup, { once: true });
    window.print();
    setPrintPreferencesOpen(false);
  }

  async function executeRecipeAction(action: GroupRecipeActionOut) {
    closeMoreMenu();
    setActionError(null);

    try {
      if (action.actionType === "link") {
        window.open(parseRecipeActionUrl(action.url, recipe, scale), "_blank", "noopener,noreferrer");
        return;
      }

      if (!recipe.slug) {
        throw new Error("Recipe details are unavailable.");
      }

      await triggerGroupRecipeAction(action.id, recipe.slug, scale);
      setActionStatus("Recipe action sent");
    } catch (recipeActionError) {
      setActionError(recipeActionError instanceof Error ? recipeActionError.message : "Unable to execute recipe action");
    }
  }

  return (
    <Stack spacing={2.5}>
      {!isCookMode ? (
        <Card variant="outlined" sx={{ overflow: "hidden" }}>
          <Box sx={{ position: "relative" }}>
            <Stack
              direction="row"
              spacing={0.75}
              sx={{
                position: { xs: "static", md: "absolute" },
                top: { md: 12 },
                right: { md: 12 },
                zIndex: 2,
                justifyContent: { xs: "flex-end" },
                p: { xs: 1, md: 0.75 },
                bgcolor: { md: "rgba(255,255,255,0.94)" },
                borderRadius: { md: 999 },
                boxShadow: { md: 2 },
              }}
            >
              {currentUser ? (
                <Tooltip title={favorite ? "Remove favorite" : "Add favorite"}>
                  <span>
                    <IconButton
                      color={favorite ? "error" : "default"}
                      onClick={() => {
                        setActionStatus(null);
                        setActionError(null);
                        favoriteMutation.mutate(!favorite);
                      }}
                      size="small"
                    >
                      {favorite ? <FavoriteRoundedIcon /> : <FavoriteBorderRoundedIcon />}
                    </IconButton>
                  </span>
                </Tooltip>
              ) : null}

              <Tooltip title="Timeline">
                <IconButton
                  component="a"
                  href={apiClient.resolvePath(`/g/${groupSlug}/recipes/timeline`)}
                  size="small"
                >
                  <HistoryRoundedIcon />
                </IconButton>
              </Tooltip>

              <Tooltip title="Edit recipe">
                <IconButton onClick={onEdit} size="small">
                  <EditRoundedIcon />
                </IconButton>
              </Tooltip>

              <Tooltip title="Cooking mode">
                <IconButton onClick={() => setIsCookMode(true)} size="small">
                  <LocalDiningRoundedIcon />
                </IconButton>
              </Tooltip>

              <Tooltip title="More actions">
                <IconButton onClick={event => setMoreAnchor(event.currentTarget)} size="small">
                  <MoreVertRoundedIcon />
                </IconButton>
              </Tooltip>
            </Stack>

            <Menu
              anchorEl={moreAnchor}
              onClose={closeMoreMenu}
              open={Boolean(moreAnchor)}
            >
              {currentUser ? (
                <MenuItem
                  onClick={() => {
                    void startRecipeDownload();
                  }}
                >
                  Download
                </MenuItem>
              ) : null}
              {currentUser ? (
                <MenuItem
                  onClick={() => {
                    closeMoreMenu();
                    setDuplicateDialogOpen(true);
                  }}
                >
                  Duplicate
                </MenuItem>
              ) : null}
              {currentUser ? (
                <MenuItem
                  onClick={() => {
                    closeMoreMenu();
                    setPlanDialogOpen(true);
                  }}
                >
                  Add to meal plan
                </MenuItem>
              ) : null}
              {currentUser ? (
                <MenuItem onClick={() => void openShoppingListDialog()}>
                  Add to shopping list
                </MenuItem>
              ) : null}
              <MenuItem
                onClick={() => {
                  closeMoreMenu();
                  window.print();
                }}
              >
                Print
              </MenuItem>
              <MenuItem
                onClick={() => {
                  closeMoreMenu();
                  setPrintPreferencesOpen(true);
                }}
              >
                Print preferences
              </MenuItem>
              {currentUser ? (
                <MenuItem
                  onClick={() => {
                    closeMoreMenu();
                    setShareDialogOpen(true);
                  }}
                >
                  Share
                </MenuItem>
              ) : null}
              {groupRecipeActions.length > 0 ? <Divider /> : null}
              {groupRecipeActions.map(action => (
                <MenuItem
                  key={action.id}
                  onClick={() => {
                    void executeRecipeAction(action);
                  }}
                >
                  {action.title}
                </MenuItem>
              ))}
              {canReimport ? (
                <MenuItem
                  onClick={() => {
                    closeMoreMenu();
                    setReimportDialogOpen(true);
                  }}
                >
                  Reimport
                </MenuItem>
              ) : null}
              {canDelete ? (
                <MenuItem
                  onClick={() => {
                    closeMoreMenu();
                    setDeleteDialogOpen(true);
                  }}
                >
                  Delete
                </MenuItem>
              ) : null}
              {hasCheckedItems ? <Divider /> : null}
              {hasCheckedItems ? (
                <MenuItem
                  onClick={() => {
                    resetChecks();
                    closeMoreMenu();
                  }}
                >
                  Clear checks
                </MenuItem>
              ) : null}
            </Menu>

            <Grid container>
            <Grid size={{ xs: 12, md: imageUrl ? 6 : 12 }}>
              <CardContent sx={{ p: { xs: 3, md: 4 } }}>
                <Stack spacing={2} sx={{ alignItems: "center", justifyContent: "center", minHeight: "100%" }}>
                  <Stack spacing={0.75} sx={{ alignItems: "center", textAlign: "center" }}>
                    <Typography sx={{ opacity: 0.8 }} variant="h5">
                      {recipe.name}
                    </Typography>
                    <Rating precision={0.5} readOnly value={recipe.rating ?? 0} />
                  </Stack>

                  <Divider flexItem />

                  {recipe.description ? (
                    <Typography color="text.secondary" data-print-role="recipe-description" sx={{ maxWidth: 520, textAlign: "center" }}>
                      {recipe.description}
                    </Typography>
                  ) : null}

                  {recipe.description ? <Divider flexItem /> : null}

                  <Stack direction="row" spacing={4} flexWrap="wrap" useFlexGap justifyContent="center">
                    <Stack spacing={2} sx={{ alignItems: "center" }}>
                      {recipe.recipeYield ? (
                        <Box sx={infoMetricSx()}>
                          <Typography color="text.secondary" variant="caption">Yield</Typography>
                          <Typography variant="body2">{recipe.recipeYield}</Typography>
                        </Box>
                      ) : null}
                      {recipe.lastMade ? (
                        <Box sx={infoMetricSx()}>
                          <Typography color="text.secondary" variant="caption">Last made</Typography>
                          <Typography variant="body2">{new Date(recipe.lastMade).toLocaleDateString("en-US")}</Typography>
                        </Box>
                      ) : null}
                    </Stack>

                    {recipe.prepTime || recipe.totalTime || recipe.cookTime || recipe.performTime ? (
                      <Box sx={{ ...infoMetricSx(), minWidth: 240, alignSelf: "center" }}>
                        <Typography color="text.secondary" variant="caption">Time</Typography>
                        <Stack spacing={0.5} sx={{ mt: 0.75, alignItems: "center" }}>
                          {recipe.prepTime ? <Typography variant="body2">Prep {recipe.prepTime}</Typography> : null}
                          {recipe.cookTime ? <Typography variant="body2">Cook {recipe.cookTime}</Typography> : null}
                          {recipe.totalTime ? <Typography variant="body2">Total {recipe.totalTime}</Typography> : null}
                          {recipe.performTime ? <Typography variant="body2">Perform {recipe.performTime}</Typography> : null}
                        </Stack>
                      </Box>
                    ) : null}
                  </Stack>
                </Stack>
              </CardContent>
            </Grid>
            {imageUrl ? (
              <Grid size={{ xs: 12, md: 6 }}>
                <Box
                  component="img"
                  data-print-role="recipe-image"
                  src={imageUrl}
                  alt={recipe.name ?? "Recipe image"}
                  sx={{
                    width: "100%",
                    height: "100%",
                    minHeight: { xs: 240, md: 360 },
                    objectFit: "cover",
                    display: "block",
                  }}
                />
              </Grid>
            ) : null}
            </Grid>
          </Box>

          <Divider />

          <CardContent sx={{ p: { xs: 2.5, md: 3 } }}>
            <Stack spacing={3}>
              <Box>
                <Stack
                  direction="row"
                  spacing={1}
                  alignItems="center"
                  sx={{
                    px: 1.5,
                    py: 1,
                    border: 1,
                    borderColor: "divider",
                    borderRadius: 999,
                    width: "fit-content",
                  }}
                >
                  <Typography variant="body2" color="text.secondary">Scale</Typography>
                  <Button
                    aria-label="Decrease ingredient scale"
                    onClick={() => setScaleInput(current => String(Math.max(0.25, normalizeScaleInput(current) - 0.25)))}
                    size="small"
                    variant="text"
                  >
                    <RemoveRoundedIcon fontSize="small" />
                  </Button>
                  <TextField
                    aria-label="Ingredient scale"
                    inputProps={{ inputMode: "decimal" }}
                    onBlur={() => setScaleInput(String(scale))}
                    onChange={event => setScaleInput(event.target.value)}
                    size="small"
                    sx={{ width: 76 }}
                    value={scaleInput}
                  />
                  <Button
                    aria-label="Increase ingredient scale"
                    onClick={() => setScaleInput(current => String(Math.min(10, normalizeScaleInput(current) + 0.25)))}
                    size="small"
                    variant="text"
                  >
                    <AddRoundedIcon fontSize="small" />
                  </Button>
                </Stack>
              </Box>

              <Grid container>
                <Grid
                  size={{ xs: 12, md: 4 }}
                  sx={{
                    borderRight: { md: 1 },
                    borderColor: "divider",
                    pr: { md: 3 },
                    mb: { xs: 3, md: 0 },
                  }}
                >
                  <Stack spacing={3}>
                    <Stack spacing={0.5}>
                      <Typography variant="h5">Ingredients</Typography>
                      <Typography color="text.secondary" variant="body2">{ingredientRows.length} items</Typography>
                    </Stack>

                    <List disablePadding sx={{ mx: -1 }}>
                      {ingredientRows.map((ingredient, index) => {
                        const key = ingredientKey(ingredient, index);
                        const checked = ingredientChecks[key] ?? false;
                        return (
                          <Box key={key}>
                            {ingredient.title?.trim() ? (
                              <Typography sx={{ px: 2, pt: index === 0 ? 0 : 1.5, pb: 0.5 }} variant="subtitle2">
                                {ingredient.title}
                              </Typography>
                            ) : null}
                            <ListItemButton
                              onClick={() => setIngredientChecks(current => ({ ...current, [key]: !checked }))}
                              sx={{ borderRadius: 2, alignItems: "flex-start", px: 1 }}
                            >
                              <Checkbox
                                checked={checked}
                                inputProps={{ "aria-label": ingredientPrimaryText(ingredient, scale) }}
                                tabIndex={-1}
                              />
                              <ListItemText
                                primary={ingredientPrimaryText(ingredient, scale)}
                                primaryTypographyProps={{
                                  sx: {
                                    fontWeight: 500,
                                    textDecoration: checked ? "line-through" : undefined,
                                  },
                                }}
                                secondary={ingredientSecondaryText(ingredient)}
                                secondaryTypographyProps={{
                                  sx: {
                                    textDecoration: checked ? "line-through" : undefined,
                                  },
                                }}
                              />
                            </ListItemButton>
                          </Box>
                        );
                      })}
                    </List>

                    {tools.length > 0 ? (
                      <>
                        <Divider />
                        <Stack spacing={1}>
                          <Typography variant="h6">Required tools</Typography>
                          <List disablePadding sx={{ mx: -1 }}>
                            {tools.map((tool, index) => {
                              const key = toolKey(tool, index);
                              const checked = toolChecks[key] ?? false;
                              return (
                                <ListItemButton
                                  key={key}
                                  onClick={() => setToolChecks(current => ({ ...current, [key]: !checked }))}
                                  sx={{ borderRadius: 2, px: 1 }}
                                >
                                  <Checkbox checked={checked} inputProps={{ "aria-label": tool.name }} tabIndex={-1} />
                                  <ListItemText
                                    primary={tool.name}
                                    primaryTypographyProps={{
                                      sx: { textDecoration: checked ? "line-through" : undefined },
                                    }}
                                  />
                                </ListItemButton>
                              );
                            })}
                          </List>
                        </Stack>
                      </>
                    ) : null}

                    {categories.length > 0 ? (
                      <Card variant="outlined">
                        <CardContent sx={{ p: 2 }}>
                          <Stack spacing={1.5}>
                            <Typography variant="subtitle1">Categories</Typography>
                            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                              {categories.map(category => <Chip key={category.id ?? category.slug} label={category.name} size="small" />)}
                            </Stack>
                          </Stack>
                        </CardContent>
                      </Card>
                    ) : null}

                    {tags.length > 0 ? (
                      <Card variant="outlined">
                        <CardContent sx={{ p: 2 }}>
                          <Stack spacing={1.5}>
                            <Typography variant="subtitle1">Tags</Typography>
                            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                              {tags.map(tag => <Chip key={tag.id ?? tag.slug} label={tag.name} size="small" />)}
                            </Stack>
                          </Stack>
                        </CardContent>
                      </Card>
                    ) : null}
                  </Stack>
                </Grid>

                <Grid size={{ xs: 12, md: 8 }} sx={{ pl: { md: 3 } }}>
                  <Stack spacing={3}>
                    <Stack spacing={0.5}>
                      <Typography variant="h5">Instructions</Typography>
                      <Typography color="text.secondary" variant="body2">{steps.length} steps</Typography>
                    </Stack>

                    <Stack spacing={1.5}>
                      {steps.map((step, index) => {
                        const key = stepKey(step, index);
                        const checked = stepChecks[key] ?? false;
                        const heading = step.summary?.trim() || step.title?.trim() || `Step ${index + 1}`;
                        return (
                          <Box
                            key={key}
                            sx={{
                              border: 1,
                              borderColor: checked ? "success.main" : "divider",
                              borderRadius: 2,
                              bgcolor: checked ? "action.selected" : "background.paper",
                            }}
                          >
                            <ListItemButton
                              onClick={() => setStepChecks(current => ({ ...current, [key]: !checked }))}
                              sx={{ borderRadius: 2, alignItems: "flex-start", px: 2, py: 1.5 }}
                            >
                              <Checkbox
                                checked={checked}
                                inputProps={{ "aria-label": `Step ${index + 1}: ${heading}` }}
                                tabIndex={-1}
                                sx={{ mt: 0.25 }}
                              />
                              <Box sx={{ flex: 1 }}>
                                <Stack direction={{ xs: "column", sm: "row" }} spacing={1} alignItems={{ sm: "center" }}>
                                  <Chip color={checked ? "success" : "default"} label={`Step ${index + 1}`} size="small" />
                                  {step.title && step.summary ? (
                                    <Typography color="text.secondary" variant="body2">
                                      {step.title}
                                    </Typography>
                                  ) : null}
                                </Stack>
                                <Typography sx={{ mt: 1, textDecoration: checked ? "line-through" : undefined }} variant="h6">
                                  {heading}
                                </Typography>
                                <Typography color="text.secondary" sx={{ mt: 0.75, textDecoration: checked ? "line-through" : undefined }}>
                                  {step.text}
                                </Typography>
                              </Box>
                            </ListItemButton>
                          </Box>
                        );
                      })}
                    </Stack>

                    {notes.length > 0 ? (
                      <>
                        <Divider />
                        <Stack data-print-role="recipe-notes" spacing={1.5}>
                          <Typography variant="h6">Notes</Typography>
                          {notes.map((note, index) => (
                            <Box key={`${note.title}-${index}`}>
                              <Typography variant="subtitle2">{note.title}</Typography>
                              <Typography color="text.secondary">{note.text}</Typography>
                              {index < notes.length - 1 ? <Divider sx={{ mt: 2 }} /> : null}
                            </Box>
                          ))}
                        </Stack>
                      </>
                    ) : null}

                    {recipe.orgURL ? (
                      <>
                        <Divider />
                        <Stack direction="row" justifyContent="flex-end">
                          <Button href={recipe.orgURL} size="small" target="_blank" variant="text">
                            Original URL
                          </Button>
                        </Stack>
                      </>
                    ) : null}
                  </Stack>
                </Grid>
              </Grid>
            </Stack>
          </CardContent>
        </Card>
      ) : (
        <Card variant="outlined" sx={{ overflow: "hidden" }}>
          <Grid container>
            <Grid
              size={{ xs: 12, md: 5 }}
              sx={{
                borderRight: { md: 1 },
                borderColor: "divider",
              }}
            >
              <Box sx={{ p: { xs: 2.5, md: 3 } }}>
                <Stack spacing={3}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center">
                    <Typography variant="h5">Ingredients</Typography>
                    <Button
                      onClick={() => setIsCookMode(false)}
                      startIcon={<LocalDiningRoundedIcon />}
                      variant="contained"
                    >
                      Exit cooking mode
                    </Button>
                  </Stack>

                  <Stack
                    direction="row"
                    spacing={1}
                    alignItems="center"
                    sx={{
                      px: 1.5,
                      py: 1,
                      border: 1,
                      borderColor: "divider",
                      borderRadius: 999,
                      width: "fit-content",
                    }}
                  >
                    <Typography variant="body2" color="text.secondary">Scale</Typography>
                    <Button
                      aria-label="Decrease ingredient scale"
                      onClick={() => setScaleInput(current => String(Math.max(0.25, normalizeScaleInput(current) - 0.25)))}
                      size="small"
                      variant="text"
                    >
                      <RemoveRoundedIcon fontSize="small" />
                    </Button>
                    <TextField
                      aria-label="Ingredient scale"
                      inputProps={{ inputMode: "decimal" }}
                      onBlur={() => setScaleInput(String(scale))}
                      onChange={event => setScaleInput(event.target.value)}
                      size="small"
                      sx={{ width: 76 }}
                      value={scaleInput}
                    />
                    <Button
                      aria-label="Increase ingredient scale"
                      onClick={() => setScaleInput(current => String(Math.min(10, normalizeScaleInput(current) + 0.25)))}
                      size="small"
                      variant="text"
                    >
                      <AddRoundedIcon fontSize="small" />
                    </Button>
                  </Stack>

                  <List disablePadding sx={{ mx: -1 }}>
                    {ingredientRows.map((ingredient, index) => {
                      const key = ingredientKey(ingredient, index);
                      const checked = ingredientChecks[key] ?? false;
                      return (
                        <Box key={key}>
                          {ingredient.title?.trim() ? (
                            <Typography sx={{ px: 2, pt: index === 0 ? 0 : 1.5, pb: 0.5 }} variant="subtitle2">
                              {ingredient.title}
                            </Typography>
                          ) : null}
                          <ListItemButton
                            onClick={() => setIngredientChecks(current => ({ ...current, [key]: !checked }))}
                            sx={{ borderRadius: 2, alignItems: "flex-start", px: 1 }}
                          >
                            <Checkbox
                              checked={checked}
                              inputProps={{ "aria-label": ingredientPrimaryText(ingredient, scale) }}
                              tabIndex={-1}
                            />
                            <ListItemText
                              primary={ingredientPrimaryText(ingredient, scale)}
                              primaryTypographyProps={{
                                sx: {
                                  fontWeight: 500,
                                  textDecoration: checked ? "line-through" : undefined,
                                },
                              }}
                              secondary={ingredientSecondaryText(ingredient)}
                              secondaryTypographyProps={{
                                sx: {
                                  textDecoration: checked ? "line-through" : undefined,
                                },
                              }}
                            />
                          </ListItemButton>
                        </Box>
                      );
                    })}
                  </List>

                  {tools.length > 0 ? (
                    <>
                      <Divider />
                      <Stack spacing={1}>
                        <Typography variant="h6">Required tools</Typography>
                        <List disablePadding sx={{ mx: -1 }}>
                          {tools.map((tool, index) => {
                            const key = toolKey(tool, index);
                            const checked = toolChecks[key] ?? false;
                            return (
                              <ListItemButton
                                key={key}
                                onClick={() => setToolChecks(current => ({ ...current, [key]: !checked }))}
                                sx={{ borderRadius: 2, px: 1 }}
                              >
                                <Checkbox checked={checked} inputProps={{ "aria-label": tool.name }} tabIndex={-1} />
                                <ListItemText
                                  primary={tool.name}
                                  primaryTypographyProps={{
                                    sx: { textDecoration: checked ? "line-through" : undefined },
                                  }}
                                />
                              </ListItemButton>
                            );
                          })}
                        </List>
                      </Stack>
                    </>
                  ) : null}
                </Stack>
              </Box>
            </Grid>

            <Grid size={{ xs: 12, md: 7 }}>
              <Box sx={{ p: { xs: 2.5, md: 3 } }}>
                <Stack spacing={3}>
                  <Stack spacing={0.5}>
                    <Typography variant="h5">Instructions</Typography>
                    <Typography color="text.secondary" variant="body2">{steps.length} steps</Typography>
                  </Stack>

                  <Stack spacing={1.5}>
                    {steps.map((step, index) => {
                      const key = stepKey(step, index);
                      const checked = stepChecks[key] ?? false;
                      const heading = step.summary?.trim() || step.title?.trim() || `Step ${index + 1}`;
                      return (
                        <Box
                          key={key}
                          sx={{
                            border: 1,
                            borderColor: checked ? "success.main" : "divider",
                            borderRadius: 2,
                            bgcolor: checked ? "action.selected" : "background.paper",
                          }}
                        >
                          <ListItemButton
                            onClick={() => setStepChecks(current => ({ ...current, [key]: !checked }))}
                            sx={{ borderRadius: 2, alignItems: "flex-start", px: 2, py: 1.5 }}
                          >
                            <Checkbox
                              checked={checked}
                              inputProps={{ "aria-label": `Step ${index + 1}: ${heading}` }}
                              tabIndex={-1}
                              sx={{ mt: 0.25 }}
                            />
                            <Box sx={{ flex: 1 }}>
                              <Stack direction={{ xs: "column", sm: "row" }} spacing={1} alignItems={{ sm: "center" }}>
                                <Chip color={checked ? "success" : "default"} label={`Step ${index + 1}`} size="small" />
                                {step.title && step.summary ? (
                                  <Typography color="text.secondary" variant="body2">
                                    {step.title}
                                  </Typography>
                                ) : null}
                              </Stack>
                              <Typography sx={{ mt: 1, textDecoration: checked ? "line-through" : undefined }} variant="h6">
                                {heading}
                              </Typography>
                              <Typography color="text.secondary" sx={{ mt: 0.75, textDecoration: checked ? "line-through" : undefined }}>
                                {step.text}
                              </Typography>
                            </Box>
                          </ListItemButton>
                        </Box>
                      );
                    })}
                  </Stack>
                </Stack>
              </Box>
            </Grid>
          </Grid>
        </Card>
      )}

      <Dialog open={duplicateDialogOpen} onClose={() => setDuplicateDialogOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Duplicate recipe</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              autoFocus
              label="Recipe name"
              value={duplicateName}
              onChange={event => setDuplicateName(event.target.value)}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDuplicateDialogOpen(false)}>Cancel</Button>
          <Button onClick={() => void submitDuplicateAction()} variant="contained" disabled={duplicateLoading}>
            Duplicate
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={planDialogOpen} onClose={() => setPlanDialogOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Add recipe to meal plan</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              label="Date"
              type="date"
              value={planDate}
              onChange={event => setPlanDate(event.target.value)}
              InputLabelProps={{ shrink: true }}
            />
            <TextField
              select
              label="Meal type"
              value={planType}
              onChange={event => setPlanType(event.target.value as PlanEntryType)}
            >
              {mealPlanEntryTypes.map(type => (
                <MenuItem key={type} value={type}>{type}</MenuItem>
              ))}
            </TextField>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPlanDialogOpen(false)}>Cancel</Button>
          <Button onClick={() => void submitMealPlanAction()} variant="contained" disabled={planLoading}>
            Save
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={shoppingDialogOpen} onClose={() => setShoppingDialogOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Add recipe to shopping list</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              select
              label="Existing shopping list"
              value={selectedListId}
              onChange={event => setSelectedListId(event.target.value)}
            >
              <MenuItem value="">Create a new list instead</MenuItem>
              {shoppingLists.map(list => (
                <MenuItem key={list.id} value={list.id}>{list.name ?? "Untitled list"}</MenuItem>
              ))}
            </TextField>
            {!selectedListId ? (
              <TextField
                label="New shopping list name"
                value={newListName}
                onChange={event => setNewListName(event.target.value)}
              />
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShoppingDialogOpen(false)}>Cancel</Button>
          <Button onClick={() => void submitShoppingListAction()} variant="contained" disabled={shoppingLoading}>
            Save
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={shareDialogOpen} onClose={() => setShareDialogOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Share recipe</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Typography color="text.secondary" variant="body2">
              Create a share link for this recipe.
            </Typography>
            {shareLink ? (
              <TextField
                label="Share link"
                value={shareLink}
                inputProps={{ readOnly: true }}
              />
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShareDialogOpen(false)}>Close</Button>
          {shareLink ? (
            <Button onClick={() => void copyShareLink()} disabled={!navigator.clipboard}>
              Copy link
            </Button>
          ) : null}
          <Button onClick={() => void createShareLink()} variant="contained" disabled={shareLoading}>
            {shareLink ? "Create another link" : "Create share link"}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={printPreferencesOpen} onClose={() => setPrintPreferencesOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Print preferences</DialogTitle>
        <DialogContent>
          <Stack spacing={1.5} sx={{ pt: 1 }}>
            <FormControlLabel
              control={(
                <Switch
                  checked={printPreferences.showImage}
                  onChange={(_, checked) => setPrintPreferences(current => ({ ...current, showImage: checked }))}
                />
              )}
              label="Show recipe image"
            />
            <FormControlLabel
              control={(
                <Switch
                  checked={printPreferences.showDescription}
                  onChange={(_, checked) => setPrintPreferences(current => ({ ...current, showDescription: checked }))}
                />
              )}
              label="Show description"
            />
            <FormControlLabel
              control={(
                <Switch
                  checked={printPreferences.showNotes}
                  onChange={(_, checked) => setPrintPreferences(current => ({ ...current, showNotes: checked }))}
                />
              )}
              label="Show notes"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPrintPreferencesOpen(false)}>Cancel</Button>
          <Button onClick={printRecipeWithPreferences} variant="contained">
            Print
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={reimportDialogOpen} onClose={() => setReimportDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Reimport recipe</DialogTitle>
        <DialogContent>
          <Typography color="text.secondary" sx={{ pt: 1 }}>
            Reimport this recipe from its original source URL.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setReimportDialogOpen(false)}>Cancel</Button>
          <Button onClick={() => void submitReimportAction()} variant="contained" disabled={reimportLoading}>
            Reimport
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Delete recipe</DialogTitle>
        <DialogContent>
          <Typography color="text.secondary" sx={{ pt: 1 }}>
            {currentUser?.admin && currentUser.id !== recipe.userId
              ? "Delete this recipe as an admin."
              : "Delete this recipe permanently."}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button color="error" onClick={() => void submitDeleteAction()} variant="contained" disabled={deleteLoading}>
            Delete
          </Button>
        </DialogActions>
      </Dialog>

      {wakeLockSupported ? (
        <Box sx={{ display: "flex", justifyContent: { xs: "center", md: "flex-end" }, px: { xs: 0.5, md: 1 } }}>
          <FormControlLabel
            control={
              <Switch
                checked={keepScreenAwake}
                onChange={(_, checked) => setKeepScreenAwake(checked)}
              />
            }
            label="Keep screen awake"
            sx={{ mr: 0 }}
          />
        </Box>
      ) : null}

      {actionStatus ? <Alert severity="success" onClose={() => setActionStatus(null)}>{actionStatus}</Alert> : null}
      {actionError ? <Alert severity="error" onClose={() => setActionError(null)}>{actionError}</Alert> : null}
      {wakeLockError ? <Alert severity="warning">{wakeLockError}</Alert> : null}
      {wakeLockSupported && wakeLockActive ? (
        <Typography color="text.secondary" sx={{ px: { xs: 0.5, md: 1 } }} variant="caption">
          Screen wake lock is active while this recipe is open.
        </Typography>
      ) : null}
    </Stack>
  );
}
