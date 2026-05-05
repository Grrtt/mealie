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
import { fetchRecipes, fetchTags } from "@/features/recipes/api";

export function RecipeTagsRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/recipes/tags" });
  const search = useSearch({ strict: false }) as { id?: string };
  const navigate = useNavigate();
  const { data: user } = useCurrentUser();
  const tagsQuery = useQuery({ queryKey: ["browse-tags"], queryFn: fetchTags });
  const recipesQuery = useQuery({
    queryKey: ["browse-tag-recipes", search.id],
    queryFn: async () => await fetchRecipes({ tags: search.id ? [search.id] : undefined }),
  });

  useEffect(() => {
    document.title = "Recipe Tags · Mealie";
  }, []);

  const selected = (tagsQuery.data?.items ?? []).find(item => item.id === search.id) ?? null;

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Recipe Tags">
      <Stack spacing={3}>
        <Typography variant="h4">Recipe tags</Typography>
        <Autocomplete
          options={tagsQuery.data?.items ?? []}
          getOptionLabel={option => option.name}
          value={selected}
          onChange={(_, value) => navigate({ href: `/g/${groupSlug}/recipes/tags${value ? `?id=${value.id}` : ""}` })}
          renderInput={params => <TextField {...params} label="Filter by tag" />}
        />
        <RecipeCards recipes={recipesQuery.data?.items ?? []} hrefBuilder={recipe => `/g/${groupSlug}/r/${recipe.slug}`} />
      </Stack>
    </AppShell>
  );
}
