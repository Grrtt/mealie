import { useSearch, useParams } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { RecipeEditor } from "@/components/recipes/RecipeEditor";
import { useCurrentUser } from "@/features/auth/useCurrentUser";

export function RecipeDetailRouteComponent() {
  const { groupSlug, slug } = useParams({ from: "/g/$groupSlug/r/$slug" });
  const search = useSearch({ strict: false }) as { edit?: string | boolean };
  const { data: user } = useCurrentUser();

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Recipes">
      <RecipeEditor
        groupSlug={groupSlug}
        recipeSlug={slug}
        currentUser={user ?? null}
        initialEditMode={search.edit === true || search.edit === "1" || search.edit === "true"}
      />
    </AppShell>
  );
}
