import { useMemo, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useNavigate } from "@tanstack/react-router";
import { Dialog, DialogActions, DialogContent, DialogTitle } from "@/components/dialogs";
import { addRecipeToShoppingList, createOrSelectShoppingList, fetchShoppingLists } from "@/features/shopping/fromRecipe";
import { addMealPlanToShoppingList } from "@/features/mealplan/toShoppingList";
import { createMealPlanEntry, formatMealPlanDate } from "@/features/mealplan/actions";
import type { PlanEntryType, ReadPlanEntry, Recipe, ShoppingListSummary } from "@/lib/api/contracts";

type Props = {
  groupSlug: string;
  recipe?: Recipe | null;
  mealPlanEntries?: ReadPlanEntry[];
};

type ShoppingDialogMode = "recipe" | "planner" | null;

const defaultPlanType: PlanEntryType = "dinner";

export function WorkflowLinks({ groupSlug, recipe, mealPlanEntries = [] }: Props) {
  const navigate = useNavigate();
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [shoppingDialogMode, setShoppingDialogMode] = useState<ShoppingDialogMode>(null);
  const [planDialogOpen, setPlanDialogOpen] = useState(false);
  const [shoppingLists, setShoppingLists] = useState<ShoppingListSummary[]>([]);
  const [selectedListId, setSelectedListId] = useState("");
  const [newListName, setNewListName] = useState("");
  const [planDate, setPlanDate] = useState(formatMealPlanDate(new Date()));
  const [planType, setPlanType] = useState<PlanEntryType>(defaultPlanType);
  const [loading, setLoading] = useState(false);

  const hasMealPlanRecipes = useMemo(
    () => mealPlanEntries.some(entry => Boolean(entry.recipeId)),
    [mealPlanEntries],
  );

  async function openShoppingDialog(mode: Exclude<ShoppingDialogMode, null>) {
    setError(null);
    setStatus(null);
    setLoading(true);
    try {
      const nextLists = await fetchShoppingLists();
      setShoppingLists(nextLists.items ?? []);
      setShoppingDialogMode(mode);
    }
    catch (dialogError) {
      setError(dialogError instanceof Error ? dialogError.message : "Unable to load shopping lists");
    }
    finally {
      setLoading(false);
    }
  }

  async function submitShoppingAction() {
    setLoading(true);
    setError(null);

    try {
      const listId = await createOrSelectShoppingList(selectedListId || null, newListName);

      if (!listId) {
        throw new Error("Choose an existing shopping list or provide a new list name.");
      }

      if (shoppingDialogMode === "recipe") {
        if (!recipe) throw new Error("Recipe details are unavailable.");
        await addRecipeToShoppingList(listId, recipe);
        setStatus("Recipe added to shopping list");
      }
      else if (shoppingDialogMode === "planner") {
        await addMealPlanToShoppingList(listId, mealPlanEntries);
        setStatus("Planned meals added to shopping list");
      }

      setShoppingDialogMode(null);
      setSelectedListId("");
      setNewListName("");
    }
    catch (shoppingError) {
      setError(shoppingError instanceof Error ? shoppingError.message : "Unable to complete shopping list action");
    }
    finally {
      setLoading(false);
    }
  }

  async function submitPlanAction() {
    if (!recipe?.id) {
      setError("Recipe details are unavailable.");
      return;
    }

    setLoading(true);
    setError(null);
    try {
      await createMealPlanEntry({
        date: planDate,
        entryType: planType,
        recipeId: recipe.id,
      });
      setStatus("Recipe added to meal plan");
      setPlanDialogOpen(false);
    }
    catch (planError) {
      setError(planError instanceof Error ? planError.message : "Unable to add recipe to meal plan");
    }
    finally {
      setLoading(false);
    }
  }

  return (
    <Stack spacing={2}>
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}

      <Card variant="outlined">
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h6">Workflow links</Typography>
            <Typography color="text.secondary">
              Move between recipes, meal planning, and shopping without leaving the React slice.
            </Typography>
            <Stack direction={{ xs: "column", md: "row" }} spacing={2} useFlexGap flexWrap="wrap">
              <Button onClick={() => void navigate({ href: "/household/mealplan/planner/view" })} variant="outlined">
                Open meal planner
              </Button>
              <Button onClick={() => void navigate({ href: "/shopping-lists?disableRedirect=true" })} variant="outlined">
                Open shopping lists
              </Button>
              <Button onClick={() => void navigate({ href: `/g/${groupSlug}/recipes/categories` })} variant="outlined">
                Browse recipes
              </Button>
              {recipe ? (
                <>
                  <Button variant="contained" onClick={() => setPlanDialogOpen(true)}>
                    Plan this recipe
                  </Button>
                  <Button variant="contained" color="secondary" onClick={() => void openShoppingDialog("recipe")}>
                    Add recipe to shopping list
                  </Button>
                </>
              ) : null}
              {hasMealPlanRecipes ? (
                <Button variant="contained" color="secondary" onClick={() => void openShoppingDialog("planner")}>
                  Add planned recipes to shopping list
                </Button>
              ) : null}
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Dialog open={planDialogOpen} onClose={() => setPlanDialogOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Plan this recipe</DialogTitle>
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
              {["breakfast", "lunch", "dinner", "side", "snack", "drink", "dessert"].map(type => (
                <MenuItem key={type} value={type}>{type}</MenuItem>
              ))}
            </TextField>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPlanDialogOpen(false)}>Cancel</Button>
          <Button onClick={() => void submitPlanAction()} variant="contained" disabled={loading}>
            Save
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={shoppingDialogMode !== null} onClose={() => setShoppingDialogMode(null)} fullWidth maxWidth="sm">
        <DialogTitle>
          {shoppingDialogMode === "planner" ? "Add planned recipes to shopping list" : "Add recipe to shopping list"}
        </DialogTitle>
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
          <Button onClick={() => setShoppingDialogMode(null)}>Cancel</Button>
          <Button onClick={() => void submitShoppingAction()} variant="contained" disabled={loading}>
            Save
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
