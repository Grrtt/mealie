import { useEffect, useMemo } from "react";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useQuery } from "@tanstack/react-query";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchRecipeTimelineEvents, fetchRecipes } from "@/features/recipes/api";
import { apiClient } from "@/lib/api/client";
import { useParams } from "@tanstack/react-router";
import type { PaginationData, RecipeSummary } from "@/lib/api/contracts";

export function RecipeTimelineRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/recipes/timeline" });
  const { data: user } = useCurrentUser();

  useEffect(() => {
    document.title = "Recipe Timeline · Mealie";
  }, []);

  const timelineQuery = useQuery({
    queryKey: ["recipe-timeline", groupSlug],
    queryFn: async () => await fetchRecipeTimelineEvents(1, 24),
  });

  const recipeIds = useMemo(() => {
    return Array.from(new Set((timelineQuery.data?.items ?? []).map(event => event.recipeId)));
  }, [timelineQuery.data?.items]);

  const recipesQuery = useQuery({
    queryKey: ["timeline-recipes", recipeIds],
    queryFn: async () => {
      if (!recipeIds.length) {
        return {
          page: 1,
          per_page: 0,
          total: 0,
          total_pages: 0,
          items: [],
        } as PaginationData<RecipeSummary>;
      }
      return await fetchRecipes({
        perPage: recipeIds.length,
        queryFilter: `id IN [${recipeIds.map(id => `"${id}"`).join(", ")}]`,
      });
    },
    enabled: recipeIds.length > 0,
  });

  const recipes = useMemo(() => {
    return new Map((recipesQuery.data?.items ?? []).map(recipe => [recipe.id, recipe] as const));
  }, [recipesQuery.data?.items]);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Recipe Timeline">
      <Stack spacing={3}>
        <Typography variant="h4">Recipe timeline</Typography>
        {timelineQuery.isLoading ? <CircularProgress /> : null}
        {(timelineQuery.data?.items ?? []).map(event => {
          const recipe = recipes.get(event.recipeId);
          return (
            <Card key={event.id}>
              <CardContent>
                <Stack spacing={1}>
                  <Typography variant="h6">{event.subject}</Typography>
                  <Typography color="text.secondary">
                    {event.eventType} · {new Date(event.createdAt).toLocaleString()}
                  </Typography>
                  {event.eventMessage ? <Typography>{event.eventMessage}</Typography> : null}
                  {recipe?.slug ? (
                    <Button href={apiClient.resolvePath(`/g/${groupSlug}/r/${recipe.slug}`)}>
                      Open {recipe.name}
                    </Button>
                  ) : null}
                </Stack>
              </CardContent>
            </Card>
          );
        })}
      </Stack>
    </AppShell>
  );
}
