import { useEffect } from "react";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useParams } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { AppShell } from "@/components/layout/AppShell";
import { RecipeCards } from "@/components/recipes/RecipeCards";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchCookbooks, fetchRecipes } from "@/features/recipes/api";

export function CookbookDetailRouteComponent() {
  const { groupSlug, slug } = useParams({ from: "/g/$groupSlug/cookbooks/$slug" });
  const { data: user } = useCurrentUser();
  const cookbooksQuery = useQuery({ queryKey: ["cookbooks"], queryFn: fetchCookbooks });
  const cookbook = (cookbooksQuery.data?.items ?? []).find(item => item.slug === slug || item.id === slug);
  const recipesQuery = useQuery({
    queryKey: ["cookbook-recipes", cookbook?.id],
    queryFn: async () => await fetchRecipes({ cookbook: cookbook?.id, perPage: 100 }),
    enabled: Boolean(cookbook?.id),
  });

  useEffect(() => {
    document.title = `${cookbook?.name ?? "Cookbook"} · Mealie`;
  }, [cookbook?.name]);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title={cookbook?.name ?? "Cookbook"}>
      <Stack spacing={3}>
        <Typography variant="h4">{cookbook?.name ?? "Cookbook"}</Typography>
        {cookbook?.description ? <Typography color="text.secondary">{cookbook.description}</Typography> : null}
        {!cookbook ? <CircularProgress /> : null}
        <RecipeCards recipes={recipesQuery.data?.items ?? []} hrefBuilder={recipe => `/g/${groupSlug}/r/${recipe.slug}`} />
      </Stack>
    </AppShell>
  );
}
