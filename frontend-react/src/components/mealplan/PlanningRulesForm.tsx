import { useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Divider from "@mui/material/Divider";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { createPlanningRule, deletePlanningRule, fetchPlanningRules, updatePlanningRule } from "@/features/mealplan/actions";
import type { PlanRulesCreate, PlanRulesOut } from "@/lib/api/contracts";

const dayOptions = [
  "unset",
  "monday",
  "tuesday",
  "wednesday",
  "thursday",
  "friday",
  "saturday",
  "sunday",
] as const;

const mealTypeOptions = [
  "unset",
  "breakfast",
  "lunch",
  "dinner",
  "side",
  "snack",
  "drink",
  "dessert",
] as const;

const emptyRule: PlanRulesCreate = {
  day: "unset",
  entryType: "unset",
  queryFilterString: "",
};

export function PlanningRulesForm() {
  const [draft, setDraft] = useState<PlanRulesCreate>(emptyRule);
  const [editing, setEditing] = useState<Record<string, PlanRulesOut>>({});
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const rulesQuery = useQuery({
    queryKey: ["mealplan-rules"],
    queryFn: fetchPlanningRules,
  });

  const refresh = async () => {
    await rulesQuery.refetch();
  };

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!draft.queryFilterString?.trim()) {
        throw new Error("Enter a query filter string before creating a rule.");
      }
      return await createPlanningRule({
        ...draft,
        queryFilterString: draft.queryFilterString.trim(),
      });
    },
    onSuccess: async () => {
      setDraft(emptyRule);
      setStatus("Planning rule created");
      await refresh();
    },
    onError: createError => {
      setError(createError instanceof Error ? createError.message : "Unable to create rule");
    },
  });

  const updateMutation = useMutation({
    mutationFn: async (rule: PlanRulesOut) => await updatePlanningRule(rule),
    onSuccess: async () => {
      setStatus("Planning rule updated");
      setEditing({});
      await refresh();
    },
    onError: updateError => {
      setError(updateError instanceof Error ? updateError.message : "Unable to update rule");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => await deletePlanningRule(id),
    onSuccess: async () => {
      setStatus("Planning rule removed");
      await refresh();
    },
    onError: deleteError => {
      setError(deleteError instanceof Error ? deleteError.message : "Unable to delete rule");
    },
  });

  return (
    <Stack spacing={3}>
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h6">Create planning rule</Typography>
            <TextField
              select
              label="Day"
              value={draft.day ?? "unset"}
              onChange={event => setDraft(current => ({ ...current, day: event.target.value as PlanRulesCreate["day"] }))}
            >
              {dayOptions.map(option => <MenuItem key={option} value={option}>{option}</MenuItem>)}
            </TextField>
            <TextField
              select
              label="Meal type"
              value={draft.entryType ?? "unset"}
              onChange={event => setDraft(current => ({ ...current, entryType: event.target.value as PlanRulesCreate["entryType"] }))}
            >
              {mealTypeOptions.map(option => <MenuItem key={option} value={option}>{option}</MenuItem>)}
            </TextField>
            <TextField
              label="Query filter string"
              value={draft.queryFilterString ?? ""}
              onChange={event => setDraft(current => ({ ...current, queryFilterString: event.target.value }))}
              helperText="Reuse the legacy query filter syntax so existing backend rules continue to work."
            />
            <Button variant="contained" onClick={() => createMutation.mutate()} disabled={createMutation.isPending}>
              Create rule
            </Button>
          </Stack>
        </CardContent>
      </Card>

      <Stack spacing={2}>
        {(rulesQuery.data ?? []).map(rule => {
          const current = editing[rule.id] ?? rule;

          return (
            <Card key={rule.id} variant="outlined">
              <CardContent>
                <Stack spacing={2}>
                  <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                    <TextField
                      select
                      label="Day"
                      value={current.day ?? "unset"}
                      onChange={event => setEditing(state => ({
                        ...state,
                        [rule.id]: { ...current, day: event.target.value as PlanRulesOut["day"] },
                      }))}
                    >
                      {dayOptions.map(option => <MenuItem key={option} value={option}>{option}</MenuItem>)}
                    </TextField>
                    <TextField
                      select
                      label="Meal type"
                      value={current.entryType ?? "unset"}
                      onChange={event => setEditing(state => ({
                        ...state,
                        [rule.id]: { ...current, entryType: event.target.value as PlanRulesOut["entryType"] },
                      }))}
                    >
                      {mealTypeOptions.map(option => <MenuItem key={option} value={option}>{option}</MenuItem>)}
                    </TextField>
                  </Stack>
                  <TextField
                    label="Query filter string"
                    value={current.queryFilterString ?? ""}
                    onChange={event => setEditing(state => ({
                      ...state,
                      [rule.id]: { ...current, queryFilterString: event.target.value },
                    }))}
                  />
                  <Divider />
                  <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
                    <Button variant="contained" onClick={() => updateMutation.mutate(current)} disabled={updateMutation.isPending}>
                      Save changes
                    </Button>
                    <Button variant="outlined" onClick={() => setEditing(state => ({ ...state, [rule.id]: rule }))}>
                      Reset
                    </Button>
                    <Button color="error" variant="outlined" onClick={() => deleteMutation.mutate(rule.id)} disabled={deleteMutation.isPending}>
                      Delete
                    </Button>
                  </Stack>
                </Stack>
              </CardContent>
            </Card>
          );
        })}
      </Stack>
    </Stack>
  );
}
