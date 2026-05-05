import Typography from "@mui/material/Typography";
import { useParams } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { RecipeCards } from "@/components/recipes/RecipeCards";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchRecipes } from "@/features/recipes/api";

export function UserFavoritesRouteComponent() {
  const { id } = useParams({ from: "/user/$id/favorites" });
  const { data: user } = useCurrentUser();
  const favoritesQuery = useQuery({
    queryKey: ["user-favorites", id],
    queryFn: async () => await fetchRecipes({
      page: 1,
      perPage: 48,
      queryFilter: `favoritedBy.id = "${id}"`,
    }),
  });

  return (
    <SettingsPage
      user={user}
      title="Favorites"
      description="Browse the recipes favorited by this user with the same backend filtering used by the legacy frontend."
    >
      {favoritesQuery.data?.items?.length ? (
        <RecipeCards
          recipes={favoritesQuery.data.items}
          hrefBuilder={recipe => `/g/${user?.groupSlug ?? "home"}/r/${recipe.slug}`}
        />
      ) : (
        <Typography color="text.secondary">No favorite recipes have been found for this user.</Typography>
      )}
    </SettingsPage>
  );
}
