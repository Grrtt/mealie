import { useState } from "react";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useQuery } from "@tanstack/react-query";
import { RecipeCards } from "@/components/recipes/RecipeCards";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchGroupRecipes } from "@/features/settings/api";

export function GroupDataRecipesRouteComponent() {
  const { data: user } = useCurrentUser();
  const [search, setSearch] = useState("");
  const recipesQuery = useQuery({
    queryKey: ["group-data-recipes", search],
    queryFn: async () => await fetchGroupRecipes(search),
  });

  return (
    <SettingsPage
      user={user}
      title="Group recipe data"
      description="Search the current recipe inventory and jump directly into recipe detail routes."
    >
      <TextField label="Search recipes" value={search} onChange={event => setSearch(event.target.value)} />
      {recipesQuery.data?.items?.length ? (
        <RecipeCards
          recipes={recipesQuery.data.items}
          hrefBuilder={recipe => `/g/${user?.groupSlug ?? "home"}/r/${recipe.slug}`}
        />
      ) : (
        <Typography color="text.secondary">No recipes matched the current search.</Typography>
      )}
    </SettingsPage>
  );
}
