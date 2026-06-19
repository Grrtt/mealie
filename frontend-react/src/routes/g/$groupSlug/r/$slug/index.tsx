import { useSearch, useParams } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { AppShell } from "@/components/layout/AppShell";
import { RecipeEditor } from "@/components/recipes/RecipeEditor";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchHouseholdPreferences } from "@/features/settings/api";

export function RecipeDetailRouteComponent() {
  const { groupSlug, slug } = useParams({ from: "/g/$groupSlug/r/$slug" });
  const search = useSearch({ strict: false }) as { edit?: string | boolean };
  const { data: user } = useCurrentUser();
  const preferencesQuery = useQuery({
    queryKey: ["household-preferences"],
    queryFn: fetchHouseholdPreferences,
  });

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Recipes">
      <RecipeEditor
        groupSlug={groupSlug}
        recipeSlug={slug}
        currentUser={user ?? null}
        initialEditMode={search.edit === true || search.edit === "1" || search.edit === "true"}
        landscapeView={preferencesQuery.data?.recipeLandscapeView ?? false}
      />
    </AppShell>
  );
}
