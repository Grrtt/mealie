import { useEffect } from "react";
import Autocomplete from "@mui/material/Autocomplete";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useNavigate, useParams, useSearch } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { AppShell } from "@/components/layout/AppShell";
import { RecipeCards } from "@/components/recipes/RecipeCards";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchCategories, fetchRecipes } from "@/features/recipes/api";

export function RecipeCategoriesRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/recipes/categories" });
  const search = useSearch({ strict: false }) as { id?: string };
  const navigate = useNavigate();
  const { data: user } = useCurrentUser();
  const categoriesQuery = useQuery({ queryKey: ["browse-categories"], queryFn: fetchCategories });
  const recipesQuery = useQuery({
    queryKey: ["browse-category-recipes", search.id],
    queryFn: async () => await fetchRecipes({ categories: search.id ? [search.id] : undefined }),
  });

  useEffect(() => {
    document.title = "Recipe Categories · Mealie";
  }, []);

  const selected = (categoriesQuery.data?.items ?? []).find(item => item.id === search.id) ?? null;

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Recipe Categories">
      <Stack spacing={3}>
        <Typography variant="h4">Recipe categories</Typography>
        <Autocomplete
          options={categoriesQuery.data?.items ?? []}
          getOptionLabel={option => option.name}
          value={selected}
          onChange={(_, value) => navigate({ href: `/g/${groupSlug}/recipes/categories${value ? `?id=${value.id}` : ""}` })}
          renderInput={params => <TextField {...params} label="Filter by category" />}
        />
        <RecipeCards recipes={recipesQuery.data?.items ?? []} hrefBuilder={recipe => `/g/${groupSlug}/r/${recipe.slug}`} />
      </Stack>
    </AppShell>
  );
}
