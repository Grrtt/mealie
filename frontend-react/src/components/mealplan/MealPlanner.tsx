import { useMemo, useState } from "react";
import { addDays, differenceInCalendarDays, format, parseISO } from "date-fns";
import Alert from "@mui/material/Alert";
import Autocomplete from "@mui/material/Autocomplete";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Divider from "@mui/material/Divider";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { RecipeImage } from "@/components/recipes/RecipeImage";
import { Dialog, DialogActions, DialogContent, DialogTitle } from "@/components/dialogs";
import { recipeImageUrl } from "@/features/recipes/api";
import {
  createMealPlanEntry,
  defaultMealPlanRange,
  deleteMealPlanEntry,
  fetchMealPlans,
  fillMealPlanDay,
  fillMealPlanWeek,
  mealPlanEntryTypes,
  randomMealPlanEntry,
  searchPlannerRecipes,
  shiftMealPlanDate,
  updateMealPlanEntry,
} from "@/features/mealplan/actions";
import { WorkflowLinks } from "@/components/navigation/WorkflowLinks";
import { apiClient } from "@/lib/api/client";
import type { PlanEntryType, ReadPlanEntry, RecipeSummary } from "@/lib/api/contracts";

type Props = {
  groupSlug: string;
  mode: "view" | "edit";
  search: {
    start?: string;
    end?: string;
  };
};

type MealForm = {
  id?: number;
  date: string;
  entryType: PlanEntryType;
  recipeId: string;
  recipeName: string;
  title: string;
  text: string;
  noteOnly: boolean;
  groupId?: string;
  userId?: string;
};

type DayGroup = {
  date: string;
  meals: ReadPlanEntry[];
};

const mealPlanLabels: Record<PlanEntryType, string> = {
  breakfast: "Breakfast",
  lunch: "Lunch",
  dinner: "Dinner",
  side: "Side",
  snack: "Snack",
  drink: "Drink",
  dessert: "Dessert",
};

function createInitialForm(date: string): MealForm {
  return {
    date,
    entryType: "dinner",
    recipeId: "",
    recipeName: "",
    title: "",
    text: "",
    noteOnly: false,
  };
}

function buildPlannerHref(mode: "view" | "edit", range: { start: string; end: string }) {
  return apiClient.resolvePath(`/household/mealplan/planner/${mode}?start=${encodeURIComponent(range.start)}&end=${encodeURIComponent(range.end)}`);
}

function entryHeading(entry: ReadPlanEntry) {
  return entry.recipe?.name ?? entry.title ?? "Meal";
}

function entryDescription(entry: ReadPlanEntry) {
  return entry.recipe?.description ?? entry.text ?? "";
}

function toUpdatePayload(entry: ReadPlanEntry, overrides: Partial<ReadPlanEntry>) {
  return {
    id: entry.id,
    groupId: entry.groupId,
    userId: entry.userId,
    date: overrides.date ?? entry.date,
    entryType: overrides.entryType ?? entry.entryType,
    recipeId: overrides.recipeId ?? entry.recipeId,
    title: overrides.title ?? entry.title,
    text: overrides.text ?? entry.text,
  };
}

export function MealPlanner({ groupSlug, mode, search }: Props) {
  const defaults = defaultMealPlanRange();
  const [range, setRange] = useState({
    start: search.start ?? defaults.start,
    end: search.end ?? defaults.end,
  });
  const [dialogOpen, setDialogOpen] = useState(false);
  const [recipeSearch, setRecipeSearch] = useState("");
  const [form, setForm] = useState<MealForm>(createInitialForm(range.start));
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const mealsQuery = useQuery({
    queryKey: ["mealplans", range.start, range.end],
    queryFn: async () => await fetchMealPlans(range.start, range.end),
  });

  const recipeSearchQuery = useQuery({
    queryKey: ["mealplan-recipes", recipeSearch],
    queryFn: async () => await searchPlannerRecipes(recipeSearch),
    enabled: dialogOpen && !form.noteOnly,
  });

  const refreshMeals = async () => {
    await mealsQuery.refetch();
  };

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (form.noteOnly) {
        if (!form.title.trim()) {
          throw new Error("Enter a title for note-only meal plan entries.");
        }
      }
      else if (!form.recipeId) {
        throw new Error("Choose a recipe for this meal plan entry.");
      }

      if (form.id && form.groupId && form.userId) {
        return await updateMealPlanEntry({
          id: form.id,
          groupId: form.groupId,
          userId: form.userId,
          date: form.date,
          entryType: form.entryType,
          recipeId: form.noteOnly ? null : form.recipeId,
          title: form.noteOnly ? form.title.trim() : "",
          text: form.noteOnly ? form.text.trim() : "",
        });
      }

      return await createMealPlanEntry({
        date: form.date,
        entryType: form.entryType,
        recipeId: form.noteOnly ? null : form.recipeId,
        title: form.noteOnly ? form.title.trim() : "",
        text: form.noteOnly ? form.text.trim() : "",
      });
    },
    onSuccess: async () => {
      setStatus(form.id ? "Meal plan entry updated" : "Meal plan entry created");
      setDialogOpen(false);
      setForm(createInitialForm(range.start));
      await refreshMeals();
    },
    onError: saveError => {
      setError(saveError instanceof Error ? saveError.message : "Unable to save meal plan entry");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: number) => await deleteMealPlanEntry(id),
    onSuccess: async () => {
      setStatus("Meal plan entry removed");
      await refreshMeals();
    },
    onError: deleteError => {
      setError(deleteError instanceof Error ? deleteError.message : "Unable to delete meal plan entry");
    },
  });

  const helperMutation = useMutation({
    mutationFn: async (action: () => Promise<unknown>) => await action(),
    onSuccess: async result => {
      if (Array.isArray(result) && result.length === 0) {
        setStatus(null);
        setError("No meal plan entries were created. Add recipes first or adjust your planning rules.");
        return;
      }

      setStatus("Meal planner updated");
      await refreshMeals();
    },
    onError: helperError => {
      setError(helperError instanceof Error ? helperError.message : "Unable to update meal planner");
    },
  });

  const groupedDays = useMemo<DayGroup[]>(() => {
    if (mealsQuery.isLoading) return [];

    const start = parseISO(range.start);
    const end = parseISO(range.end);
    const dayCount = Math.max(differenceInCalendarDays(end, start), 0);
    const meals = mealsQuery.data ?? [];

    return Array.from({ length: dayCount + 1 }, (_, index) => {
      const day = addDays(start, index);
      const key = format(day, "yyyy-MM-dd");

      return {
        date: key,
        meals: meals
          .filter(entry => entry.date === key)
          .sort((left, right) => mealPlanEntryTypes.indexOf(left.entryType ?? "dinner") - mealPlanEntryTypes.indexOf(right.entryType ?? "dinner")),
      };
    });
  }, [mealsQuery.data, mealsQuery.isLoading, range.end, range.start]);

  const recipeOptions = useMemo(() => {
    return recipeSearchQuery.data?.items ?? [];
  }, [recipeSearchQuery.data]);

  function updateUrl(nextRange: typeof range) {
    if (typeof window === "undefined") return;
    window.history.replaceState({}, "", buildPlannerHref(mode, nextRange));
  }

  function updateRange(field: "start" | "end", value: string) {
    const nextRange = { ...range, [field]: value };
    setRange(nextRange);
    updateUrl(nextRange);
  }

  function openCreate(date: string) {
    setError(null);
    setForm(createInitialForm(date));
    setRecipeSearch("");
    setDialogOpen(true);
  }

  function openEdit(entry: ReadPlanEntry) {
    setError(null);
    setForm({
      id: entry.id,
      date: entry.date,
      entryType: entry.entryType ?? "dinner",
      recipeId: entry.recipeId ?? "",
      recipeName: entry.recipe?.name ?? "",
      title: entry.title ?? "",
      text: entry.text ?? "",
      noteOnly: !entry.recipeId,
      groupId: entry.groupId,
      userId: entry.userId,
    });
    setRecipeSearch(entry.recipe?.name ?? "");
    setDialogOpen(true);
  }

  if (mealsQuery.isLoading) {
    return (
      <Box sx={{ display: "grid", placeItems: "center", minHeight: "40vh" }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Stack spacing={3}>
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Stack direction={{ xs: "column", lg: "row" }} spacing={2} alignItems={{ lg: "center" }}>
              <Typography variant="h4">Meal planner</Typography>
              <Box sx={{ flexGrow: 1 }} />
              <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                <TextField
                  label="Start"
                  type="date"
                  value={range.start}
                  onChange={event => updateRange("start", event.target.value)}
                  InputLabelProps={{ shrink: true }}
                />
                <TextField
                  label="End"
                  type="date"
                  value={range.end}
                  onChange={event => updateRange("end", event.target.value)}
                  InputLabelProps={{ shrink: true }}
                />
              </Stack>
            </Stack>

            <Stack direction={{ xs: "column", md: "row" }} spacing={2} useFlexGap flexWrap="wrap">
              <Button href={buildPlannerHref("view", range)} variant={mode === "view" ? "contained" : "outlined"}>
                Planner view
              </Button>
              <Button href={buildPlannerHref("edit", range)} variant={mode === "edit" ? "contained" : "outlined"}>
                Planner edit
              </Button>
              <Button href={apiClient.resolvePath("/household/mealplan/settings")} variant="outlined">
                Planning rules
              </Button>
              {mode === "edit" ? (
                <Button
                  variant="contained"
                  onClick={() => helperMutation.mutate(async () => await fillMealPlanWeek({ startDate: range.start, endDate: range.end }))}
                  disabled={helperMutation.isPending}
                >
                  Fill date range
                </Button>
              ) : null}
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <WorkflowLinks groupSlug={groupSlug} mealPlanEntries={mealsQuery.data ?? []} />

      <Stack spacing={2}>
        {groupedDays.map(day => (
          <Card key={day.date} variant="outlined">
            <CardContent>
              <Stack spacing={2}>
                <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                  <Typography variant="h6">{format(parseISO(day.date), "EEE, MMM d")}</Typography>
                  <Chip label={`${day.meals.length} entr${day.meals.length === 1 ? "y" : "ies"}`} size="small" />
                  <Box sx={{ flexGrow: 1 }} />
                  {mode === "edit" ? (
                    <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
                      <Button size="small" variant="outlined" onClick={() => openCreate(day.date)}>
                        Add meal
                      </Button>
                      <Button
                        size="small"
                        variant="outlined"
                        onClick={() => helperMutation.mutate(async () => await fillMealPlanDay({ date: day.date, entryTypes: mealPlanEntryTypes }))}
                        disabled={helperMutation.isPending}
                      >
                        Fill day
                      </Button>
                      <Button
                        size="small"
                        variant="outlined"
                        onClick={() => helperMutation.mutate(async () => await randomMealPlanEntry({ date: day.date, entryType: "dinner" }))}
                        disabled={helperMutation.isPending}
                      >
                        Random dinner
                      </Button>
                    </Stack>
                  ) : null}
                </Stack>

                {day.meals.length ? (
                  <Stack spacing={2}>
                    {mealPlanEntryTypes.map(entryType => {
                      const items = day.meals.filter(entry => (entry.entryType ?? "dinner") === entryType);
                      if (!items.length) return null;

                      return (
                        <Stack key={entryType} spacing={1}>
                          <Typography variant="subtitle2" color="text.secondary">
                            {mealPlanLabels[entryType]}
                          </Typography>
                          {items.map(entry => (
                            <Card key={entry.id} variant="outlined">
                              <CardContent>
                                <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
                                  {entry.recipe?.id ? (
                                    <Box sx={{ width: { xs: "100%", sm: 144 }, flexShrink: 0 }}>
                                      <RecipeImage
                                        alt={entry.recipe.name ?? "Recipe image"}
                                        src={recipeImageUrl(
                                          entry.recipe.id,
                                          typeof entry.recipe.image === "string" ? entry.recipe.image : null,
                                          "min-original.webp",
                                        )}
                                        wrapperSx={{
                                          height: { xs: 180, sm: 112 },
                                          borderRadius: 2,
                                        }}
                                      />
                                    </Box>
                                  ) : null}
                                  <Stack spacing={1} sx={{ minWidth: 0, flex: 1 }}>
                                    <Stack direction={{ xs: "column", md: "row" }} spacing={1} alignItems={{ md: "center" }}>
                                      {entry.recipe?.slug ? (
                                        <Button
                                          href={apiClient.resolvePath(`/g/${groupSlug}/r/${entry.recipe.slug}`)}
                                          sx={{ justifyContent: "flex-start", p: 0 }}
                                        >
                                          {entryHeading(entry)}
                                        </Button>
                                      ) : (
                                        <Typography variant="subtitle1">{entryHeading(entry)}</Typography>
                                      )}
                                      <Box sx={{ flexGrow: 1 }} />
                                      <Chip label={mealPlanLabels[entry.entryType ?? "dinner"]} size="small" />
                                    </Stack>
                                    {entryDescription(entry) ? (
                                      <Typography color="text.secondary">{entryDescription(entry)}</Typography>
                                    ) : null}
                                    {mode === "edit" ? (
                                      <>
                                        <Divider />
                                        <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
                                          <Button size="small" variant="outlined" onClick={() => openEdit(entry)}>
                                            Edit
                                          </Button>
                                          <Button
                                            size="small"
                                            variant="outlined"
                                            onClick={() => helperMutation.mutate(async () => await updateMealPlanEntry(
                                              toUpdatePayload(entry, {
                                                date: shiftMealPlanDate(entry.date, -1),
                                                recipeId: entry.recipeId ?? null,
                                              }),
                                            ))}
                                            disabled={helperMutation.isPending}
                                          >
                                            Move earlier
                                          </Button>
                                          <Button
                                            size="small"
                                            variant="outlined"
                                            onClick={() => helperMutation.mutate(async () => await updateMealPlanEntry(
                                              toUpdatePayload(entry, {
                                                date: shiftMealPlanDate(entry.date, 1),
                                                recipeId: entry.recipeId ?? null,
                                              }),
                                            ))}
                                            disabled={helperMutation.isPending}
                                          >
                                            Move later
                                          </Button>
                                          <Button
                                            size="small"
                                            color="error"
                                            variant="outlined"
                                            onClick={() => deleteMutation.mutate(entry.id)}
                                            disabled={deleteMutation.isPending}
                                          >
                                            Delete
                                          </Button>
                                        </Stack>
                                      </>
                                    ) : null}
                                  </Stack>
                                </Stack>
                              </CardContent>
                            </Card>
                          ))}
                        </Stack>
                      );
                    })}
                  </Stack>
                ) : (
                  <Typography color="text.secondary">No meals planned for this day yet.</Typography>
                )}
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>{form.id ? "Edit meal plan entry" : "Add meal plan entry"}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              label="Date"
              type="date"
              value={form.date}
              onChange={event => setForm(current => ({ ...current, date: event.target.value }))}
              InputLabelProps={{ shrink: true }}
            />
            <TextField
              select
              label="Meal type"
              value={form.entryType}
              onChange={event => setForm(current => ({ ...current, entryType: event.target.value as PlanEntryType }))}
            >
              {mealPlanEntryTypes.map(entryType => (
                <MenuItem key={entryType} value={entryType}>
                  {mealPlanLabels[entryType]}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              select
              label="Entry mode"
              value={form.noteOnly ? "note" : "recipe"}
              onChange={event => setForm(current => ({
                ...current,
                noteOnly: event.target.value === "note",
                recipeId: event.target.value === "note" ? "" : current.recipeId,
              }))}
            >
              <MenuItem value="recipe">Recipe</MenuItem>
              <MenuItem value="note">Note only</MenuItem>
            </TextField>
            {form.noteOnly ? (
              <>
                <TextField
                  label="Title"
                  value={form.title}
                  onChange={event => setForm(current => ({ ...current, title: event.target.value }))}
                />
                <TextField
                  label="Note"
                  value={form.text}
                  multiline
                  minRows={3}
                  onChange={event => setForm(current => ({ ...current, text: event.target.value }))}
                />
              </>
            ) : (
              <Autocomplete
                options={recipeOptions}
                loading={recipeSearchQuery.isFetching}
                getOptionLabel={option => option.name ?? ""}
                inputValue={recipeSearch}
                value={
                  recipeOptions.find(option => option.id === form.recipeId)
                  ?? (form.recipeId ? { id: form.recipeId, name: form.recipeName } as RecipeSummary : null)
                }
                onInputChange={(_, value) => setRecipeSearch(value)}
                onChange={(_, value) => setForm(current => ({
                  ...current,
                  recipeId: value?.id ?? "",
                  recipeName: value?.name ?? "",
                }))}
                renderInput={params => (
                  <TextField
                    {...params}
                    label="Recipe"
                    placeholder="Search recipes"
                  />
                )}
              />
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
          <Button onClick={() => saveMutation.mutate()} variant="contained" disabled={saveMutation.isPending}>
            Save
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
