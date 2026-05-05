import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { AppShell } from "@/components/layout/AppShell";
import { PlanningRulesForm } from "@/components/mealplan/PlanningRulesForm";
import { useCurrentUser } from "@/features/auth/useCurrentUser";

export function MealPlannerSettingsRouteComponent() {
  const { data: user } = useCurrentUser();

  return (
    <AppShell groupSlug={user?.groupSlug ?? "home"} userName={user?.fullName} title="Meal planning settings">
      <Stack spacing={3}>
        <Typography variant="h4">Meal planning rules</Typography>
        <Typography color="text.secondary">
          Manage the same backend-powered planning rules used by the legacy frontend and keep assisted planning actions available from the planner.
        </Typography>
        <PlanningRulesForm />
      </Stack>
    </AppShell>
  );
}
