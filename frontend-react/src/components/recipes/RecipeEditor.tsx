import { useEffect, useMemo, useState } from "react";
import Alert from "@mui/material/Alert";
import Autocomplete from "@mui/material/Autocomplete";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CircularProgress from "@mui/material/CircularProgress";
import Divider from "@mui/material/Divider";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import type { PrivateUser, Recipe, RecipeCategory, RecipeTag, RecipeTool } from "@/lib/api/contracts";
import { applyRecipeMeta } from "@/lib/seo/recipeMeta";
import {
  fetchCategories,
  fetchRecipe,
  fetchTags,
  fetchTools,
  updateRecipe,
  type UpdateRecipePayload,
} from "@/features/recipes/api";
import { WorkflowLinks } from "@/components/navigation/WorkflowLinks";
import { RecipeDetailView } from "@/components/recipes/RecipeDetailView";
import { RecipeInteractions } from "@/components/recipes/RecipeInteractions";

type Props = {
  groupSlug: string;
  recipeSlug: string;
  currentUser: PrivateUser | null;
  initialEditMode?: boolean;
  landscapeView?: boolean;
};

type RecipeDraft = {
  name: string;
  description: string;
  recipeYield: string;
  totalTime: string;
  prepTime: string;
  cookTime: string;
  orgURL: string;
  ingredients: string;
  instructions: string;
  notes: string;
  categories: RecipeCategory[];
  tags: RecipeTag[];
  tools: RecipeTool[];
};

function organizerName(item: { name?: string | null }) {
  return item.name ?? "";
}

function ingredientToLine(ingredient: NonNullable<Recipe["recipeIngredient"]>[number]) {
  if (ingredient.originalText?.trim()) return ingredient.originalText.trim();
  const parts = [
    ingredient.quantity ? String(ingredient.quantity) : "",
    ingredient.unit?.abbreviation || ingredient.unit?.name || "",
    ingredient.food?.name || "",
    ingredient.note || "",
  ].filter(Boolean);
  return parts.join(" ").trim();
}

function stepToLine(step: NonNullable<Recipe["recipeInstructions"]>[number]) {
  return [step.title, step.text].filter(Boolean).join(": ");
}

function noteToLine(note: NonNullable<Recipe["notes"]>[number]) {
  return note.title ? `${note.title}: ${note.text}` : note.text;
}

function createDraft(recipe: Recipe): RecipeDraft {
  return {
    name: recipe.name ?? "",
    description: recipe.description ?? "",
    recipeYield: recipe.recipeYield ?? "",
    totalTime: recipe.totalTime ?? "",
    prepTime: recipe.prepTime ?? "",
    cookTime: recipe.cookTime ?? "",
    orgURL: recipe.orgURL ?? "",
    ingredients: (recipe.recipeIngredient ?? []).map(ingredientToLine).join("\n"),
    instructions: (recipe.recipeInstructions ?? []).map(stepToLine).join("\n"),
    notes: (recipe.notes ?? []).map(noteToLine).join("\n"),
    categories: recipe.recipeCategory ?? [],
    tags: recipe.tags ?? [],
    tools: recipe.tools ?? [],
  };
}

function draftToPayload(draft: RecipeDraft): UpdateRecipePayload {
  return {
    name: draft.name.trim(),
    description: draft.description.trim() || null || undefined,
    recipeYield: draft.recipeYield.trim() || undefined,
    totalTime: draft.totalTime.trim() || undefined,
    prepTime: draft.prepTime.trim() || undefined,
    cookTime: draft.cookTime.trim() || undefined,
    orgURL: draft.orgURL.trim() || undefined,
    recipeIngredients: draft.ingredients
      .split("\n")
      .map(line => line.trim())
      .filter(Boolean)
      .map((line, index) => ({
        position: index,
        originalText: line,
        disableAmount: false,
      })),
    recipeInstructions: draft.instructions
      .split("\n")
      .map(line => line.trim())
      .filter(Boolean)
      .map((line, index) => ({
        position: index,
        text: line,
      })),
    notes: draft.notes
      .split("\n")
      .map(line => line.trim())
      .filter(Boolean)
      .map((line, index) => ({
        title: `Note ${index + 1}`,
        text: line,
      })),
    tags: draft.tags.map(tag => tag.slug),
    categories: draft.categories.map(category => category.slug),
    tools: draft.tools.map(tool => tool.slug),
  };
}

export function RecipeEditor({ groupSlug, recipeSlug, currentUser, initialEditMode = false, landscapeView = false }: Props) {
  const [isEditing, setIsEditing] = useState(initialEditMode);
  const [draft, setDraft] = useState<RecipeDraft | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const recipeQuery = useQuery({
    queryKey: ["recipe", recipeSlug],
    queryFn: async () => await fetchRecipe(recipeSlug),
  });
  const categoriesQuery = useQuery({
    queryKey: ["recipe-categories"],
    queryFn: fetchCategories,
  });
  const tagsQuery = useQuery({
    queryKey: ["recipe-tags"],
    queryFn: fetchTags,
  });
  const toolsQuery = useQuery({
    queryKey: ["recipe-tools"],
    queryFn: fetchTools,
  });

  useEffect(() => {
    if (recipeQuery.data) {
      setDraft(createDraft(recipeQuery.data));
      applyRecipeMeta(recipeQuery.data, { prefix: "Mealie" });
    }
  }, [recipeQuery.data]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!draft) throw new Error("Recipe draft is unavailable");
      return await updateRecipe(recipeSlug, draftToPayload(draft));
    },
    onSuccess: nextRecipe => {
      setDraft(createDraft(nextRecipe));
      setStatus("Recipe saved");
      setIsEditing(false);
      recipeQuery.refetch().catch(() => undefined);
    },
    onError: saveError => {
      setError(saveError instanceof Error ? saveError.message : "Unable to save recipe");
    },
  });

  const recipe = useMemo(() => saveMutation.data ?? recipeQuery.data, [recipeQuery.data, saveMutation.data]);

  if (recipeQuery.isLoading || !recipe || !draft) {
    return (
      <Box sx={{ display: "grid", placeItems: "center", minHeight: "50vh" }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!isEditing) {
    return (
      <Stack spacing={3}>
        {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
        {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}

        <RecipeDetailView
          currentUser={currentUser}
          groupSlug={groupSlug}
          landscapeView={landscapeView}
          onEdit={() => setIsEditing(true)}
          onRecipeRefresh={async () => {
            await recipeQuery.refetch();
          }}
          recipe={recipe}
        />

        <WorkflowLinks groupSlug={groupSlug} recipe={recipe} />

        <RecipeInteractions
          groupSlug={groupSlug}
          recipe={recipe}
          currentUser={currentUser}
          onRecipeRefresh={async () => {
            await recipeQuery.refetch();
          }}
        />
      </Stack>
    );
  }

  return (
    <Stack spacing={3}>
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Stack spacing={2} sx={{ flex: 1 }}>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                <Typography variant="h4">{recipe.name}</Typography>
                <Box sx={{ flexGrow: 1 }} />
                <Button variant="outlined" onClick={() => setIsEditing(false)}>
                  Close editor
                </Button>
              </Stack>
              {recipe.description ? <Typography color="text.secondary">{recipe.description}</Typography> : null}
            </Stack>

            <Divider />

            <Stack spacing={2}>
              <TextField
                label="Recipe name"
                value={draft.name}
                onChange={event => setDraft(current => current ? { ...current, name: event.target.value } : current)}
              />
              <TextField
                label="Description"
                multiline
                minRows={3}
                value={draft.description}
                onChange={event => setDraft(current => current ? { ...current, description: event.target.value } : current)}
              />
              <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                <TextField label="Yield" value={draft.recipeYield} onChange={event => setDraft(current => current ? { ...current, recipeYield: event.target.value } : current)} />
                <TextField label="Total time" value={draft.totalTime} onChange={event => setDraft(current => current ? { ...current, totalTime: event.target.value } : current)} />
                <TextField label="Prep time" value={draft.prepTime} onChange={event => setDraft(current => current ? { ...current, prepTime: event.target.value } : current)} />
                <TextField label="Cook time" value={draft.cookTime} onChange={event => setDraft(current => current ? { ...current, cookTime: event.target.value } : current)} />
              </Stack>
              <TextField label="Source URL" value={draft.orgURL} onChange={event => setDraft(current => current ? { ...current, orgURL: event.target.value } : current)} />
              <Autocomplete
                multiple
                options={categoriesQuery.data?.items ?? []}
                getOptionLabel={organizerName}
                value={draft.categories}
                onChange={(_, value) => setDraft(current => current ? { ...current, categories: value as RecipeCategory[] } : current)}
                renderInput={params => <TextField {...params} label="Categories" />}
              />
              <Autocomplete
                multiple
                options={tagsQuery.data?.items ?? []}
                getOptionLabel={organizerName}
                value={draft.tags}
                onChange={(_, value) => setDraft(current => current ? { ...current, tags: value as RecipeTag[] } : current)}
                renderInput={params => <TextField {...params} label="Tags" />}
              />
              <Autocomplete
                multiple
                options={toolsQuery.data?.items ?? []}
                getOptionLabel={organizerName}
                value={draft.tools}
                onChange={(_, value) => setDraft(current => current ? { ...current, tools: value as RecipeTool[] } : current)}
                renderInput={params => <TextField {...params} label="Tools" />}
              />
              <TextField
                label="Ingredients"
                multiline
                minRows={6}
                helperText="Use one ingredient per line."
                value={draft.ingredients}
                onChange={event => setDraft(current => current ? { ...current, ingredients: event.target.value } : current)}
              />
              <TextField
                label="Instructions"
                multiline
                minRows={6}
                helperText="Use one step per line."
                value={draft.instructions}
                onChange={event => setDraft(current => current ? { ...current, instructions: event.target.value } : current)}
              />
              <TextField
                label="Notes"
                multiline
                minRows={4}
                helperText="Use one note per line."
                value={draft.notes}
                onChange={event => setDraft(current => current ? { ...current, notes: event.target.value } : current)}
              />
              <Stack direction="row" spacing={2}>
                <Button variant="contained" onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending}>
                  Save changes
                </Button>
                <Button variant="outlined" onClick={() => setDraft(createDraft(recipe))}>
                  Reset
                </Button>
              </Stack>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}
