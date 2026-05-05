import { useSearch } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { MealPlanner } from "@/components/mealplan/MealPlanner";
import { useCurrentUser } from "@/features/auth/useCurrentUser";

export function MealPlannerEditRouteComponent() {
  const { data: user } = useCurrentUser();
  const search = useSearch({ from: "/household/mealplan/planner/edit" });

  return (
    <AppShell groupSlug={user?.groupSlug ?? "home"} userName={user?.fullName} title="Meal planner">
      <MealPlanner groupSlug={user?.groupSlug ?? "home"} mode="edit" search={search} />
    </AppShell>
  );
}
