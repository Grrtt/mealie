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
import { fetchRecipes, fetchTools } from "@/features/recipes/api";

export function RecipeToolsRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/recipes/tools" });
  const search = useSearch({ strict: false }) as { id?: string };
  const navigate = useNavigate();
  const { data: user } = useCurrentUser();
  const toolsQuery = useQuery({ queryKey: ["browse-tools"], queryFn: fetchTools });
  const recipesQuery = useQuery({
    queryKey: ["browse-tool-recipes", search.id],
    queryFn: async () => await fetchRecipes({ tools: search.id ? [search.id] : undefined }),
  });

  useEffect(() => {
    document.title = "Recipe Tools · Mealie";
  }, []);

  const selected = (toolsQuery.data?.items ?? []).find(item => item.id === search.id) ?? null;

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Recipe Tools">
      <Stack spacing={3}>
        <Typography variant="h4">Recipe tools</Typography>
        <Autocomplete
          options={toolsQuery.data?.items ?? []}
          getOptionLabel={option => option.name}
          value={selected}
          onChange={(_, value) => navigate({ href: `/g/${groupSlug}/recipes/tools${value ? `?id=${value.id}` : ""}` })}
          renderInput={params => <TextField {...params} label="Filter by tool" />}
        />
        <RecipeCards recipes={recipesQuery.data?.items ?? []} hrefBuilder={recipe => `/g/${groupSlug}/r/${recipe.slug}`} />
      </Stack>
    </AppShell>
  );
}
