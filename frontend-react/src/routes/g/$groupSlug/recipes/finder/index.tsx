import { useEffect, useState } from "react";
import Autocomplete from "@mui/material/Autocomplete";
import Button from "@mui/material/Button";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useQuery } from "@tanstack/react-query";
import { useParams } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { RecipeCards } from "@/components/recipes/RecipeCards";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchFoods, fetchRecipeSuggestions, fetchTools } from "@/features/recipes/api";
import type { IngredientFood, RecipeTool } from "@/lib/api/contracts";

export function RecipeFinderRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/recipes/finder" });
  const { data: user } = useCurrentUser();
  const [selectedFoods, setSelectedFoods] = useState<IngredientFood[]>([]);
  const [selectedTools, setSelectedTools] = useState<RecipeTool[]>([]);
  const [includeFoodsOnHand, setIncludeFoodsOnHand] = useState(false);
  const [includeToolsOnHand, setIncludeToolsOnHand] = useState(false);

  useEffect(() => {
    document.title = "Recipe Finder · Mealie";
  }, []);

  const foodsQuery = useQuery({
    queryKey: ["finder-foods"],
    queryFn: fetchFoods,
  });
  const toolsQuery = useQuery({
    queryKey: ["finder-tools"],
    queryFn: fetchTools,
  });

  const suggestionsQuery = useQuery({
    queryKey: ["recipe-finder", selectedFoods.map(item => item.id), selectedTools.map(item => item.id), includeFoodsOnHand, includeToolsOnHand],
    queryFn: async () => await fetchRecipeSuggestions({
      foods: selectedFoods.map(item => item.id),
      tools: selectedTools.map(item => item.id),
      includeFoodsOnHand,
      includeToolsOnHand,
    }),
  });

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Recipe Finder">
      <Stack spacing={3}>
        <Typography variant="h4">Recipe finder</Typography>
        <Typography color="text.secondary">
          Match recipes by the foods and tools you already have available.
        </Typography>
        <Autocomplete
          multiple
          options={foodsQuery.data?.items ?? []}
          getOptionLabel={option => option.pluralName ?? option.name}
          value={selectedFoods}
          onChange={(_, value) => setSelectedFoods(value)}
          renderInput={params => <TextField {...params} label="Foods" />}
        />
        <Autocomplete
          multiple
          options={toolsQuery.data?.items ?? []}
          getOptionLabel={option => option.name}
          value={selectedTools}
          onChange={(_, value) => setSelectedTools(value)}
          renderInput={params => <TextField {...params} label="Tools" />}
        />
        <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
          <Button variant={includeFoodsOnHand ? "contained" : "outlined"} onClick={() => setIncludeFoodsOnHand(value => !value)}>
            Include foods on hand <Switch checked={includeFoodsOnHand} />
          </Button>
          <Button variant={includeToolsOnHand ? "contained" : "outlined"} onClick={() => setIncludeToolsOnHand(value => !value)}>
            Include tools on hand <Switch checked={includeToolsOnHand} />
          </Button>
        </Stack>
        {suggestionsQuery.isLoading ? <CircularProgress /> : null}
        <RecipeCards
          recipes={(suggestionsQuery.data?.items ?? []).map(item => item.recipe)}
          hrefBuilder={recipe => `/g/${groupSlug}/r/${recipe.slug}`}
        />
      </Stack>
    </AppShell>
  );
}
