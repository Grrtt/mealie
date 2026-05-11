import { useEffect, useMemo, useState } from "react";
import SearchRoundedIcon from "@mui/icons-material/SearchRounded";
import InputAdornment from "@mui/material/InputAdornment";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useQuery } from "@tanstack/react-query";
import { useParams } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { RecipeCards } from "@/components/recipes/RecipeCards";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchRecipes } from "@/features/recipes/api";

export function RecipesRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/recipes" });
  const { data: user } = useCurrentUser();
  const [search, setSearch] = useState("");
  const trimmedSearch = useMemo(() => search.trim(), [search]);
  const recipesQuery = useQuery({
    queryKey: ["browse-recipes", groupSlug, trimmedSearch],
    queryFn: async () => await fetchRecipes({ search: trimmedSearch || undefined }),
  });

  useEffect(() => {
    document.title = "Recipes · Mealie";
  }, []);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Recipes">
      <Stack spacing={3}>
        <Typography variant="h4">Recipes</Typography>
        <Typography color="text.secondary">
          Browse the current recipe collection and jump straight into any recipe detail page.
        </Typography>
        <TextField
          fullWidth
          label="Search recipes"
          onChange={event => setSearch(event.target.value)}
          placeholder="Search by name or description"
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchRoundedIcon fontSize="small" />
                </InputAdornment>
              ),
            },
          }}
          value={search}
        />
        <RecipeCards
          recipes={recipesQuery.data?.items ?? []}
          hrefBuilder={recipe => `/g/${groupSlug}/r/${recipe.slug}`}
        />
      </Stack>
    </AppShell>
  );
}
