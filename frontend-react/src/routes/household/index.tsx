import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import FormControlLabel from "@mui/material/FormControlLabel";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchHousehold, fetchHouseholdPreferences, updateHouseholdPreferences } from "@/features/settings/api";

export function HouseholdRouteComponent() {
  const { data: user } = useCurrentUser();
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const householdQuery = useQuery({ queryKey: ["household-self"], queryFn: fetchHousehold });
  const preferencesQuery = useQuery({ queryKey: ["household-preferences"], queryFn: fetchHouseholdPreferences });
  const [preferences, setPreferences] = useState<Record<string, boolean | number>>({});

  useEffect(() => {
    if (preferencesQuery.data) {
      setPreferences({
        privateHousehold: Boolean(preferencesQuery.data.privateHousehold),
        showAnnouncements: Boolean(preferencesQuery.data.showAnnouncements),
        lockRecipeEditsFromOtherHouseholds: Boolean(preferencesQuery.data.lockRecipeEditsFromOtherHouseholds),
        recipePublic: Boolean(preferencesQuery.data.recipePublic),
        recipeShowNutrition: Boolean(preferencesQuery.data.recipeShowNutrition),
        recipeShowAssets: Boolean(preferencesQuery.data.recipeShowAssets),
        recipeLandscapeView: Boolean(preferencesQuery.data.recipeLandscapeView),
        recipeDisableComments: Boolean(preferencesQuery.data.recipeDisableComments),
        firstDayOfWeek: preferencesQuery.data.firstDayOfWeek ?? 0,
      });
    }
  }, [preferencesQuery.data]);

  const mutation = useMutation({
    mutationFn: async () => await updateHouseholdPreferences(preferences),
    onSuccess: async () => {
      setStatus("Household preferences updated.");
      setError(null);
      await preferencesQuery.refetch();
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to update household preferences.");
    },
  });

  return (
    <SettingsPage
      user={user}
      title="Household settings"
      description={`Manage preferences for ${householdQuery.data?.name ?? "the current household"} without leaving the React workspace.`}
    >
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}
      <Card>
        <CardContent>
          <Stack spacing={1}>
            {[
              ["privateHousehold", "Private household"],
              ["showAnnouncements", "Show announcements"],
              ["lockRecipeEditsFromOtherHouseholds", "Lock recipe edits from other households"],
              ["recipePublic", "Allow household recipes to be public"],
              ["recipeShowNutrition", "Show nutrition by default"],
              ["recipeShowAssets", "Show recipe assets"],
              ["recipeLandscapeView", "Use landscape recipe layout"],
              ["recipeDisableComments", "Disable recipe comments"],
            ].map(([key, label]) => (
              <FormControlLabel
                key={key}
                control={(
                  <Switch
                    checked={Boolean(preferences[key])}
                    onChange={event => setPreferences(current => ({ ...current, [key]: event.target.checked }))}
                  />
                )}
                label={label}
              />
            ))}
            <TextField
              select
              label="First day of week"
              value={String(preferences.firstDayOfWeek ?? 0)}
              onChange={event => setPreferences(current => ({ ...current, firstDayOfWeek: Number(event.target.value) }))}
            >
              <MenuItem value="0">Sunday</MenuItem>
              <MenuItem value="1">Monday</MenuItem>
            </TextField>
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
                Save household settings
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </SettingsPage>
  );
}
