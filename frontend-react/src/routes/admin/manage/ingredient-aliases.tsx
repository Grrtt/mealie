import { useMemo, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import {
  createIngredientAlias,
  deleteIngredientAlias,
  fetchFoodsPage,
  fetchIngredientAliases,
  fetchUnresolvedIngredientAliases,
} from "@/features/settings/api";

export function AdminManageIngredientAliasesRouteComponent() {
  const { data: user } = useCurrentUser();
  const [selectedRawText, setSelectedRawText] = useState("");
  const [selectedFoodId, setSelectedFoodId] = useState("");
  const [status, setStatus] = useState<string | null>(null);
  const aliasesQuery = useQuery({ queryKey: ["ingredient-aliases"], queryFn: async () => await fetchIngredientAliases() });
  const unresolvedQuery = useQuery({ queryKey: ["ingredient-aliases-unresolved"], queryFn: async () => await fetchUnresolvedIngredientAliases() });
  const foodsQuery = useQuery({ queryKey: ["ingredient-alias-foods"], queryFn: fetchFoodsPage });
  const foodOptions = useMemo(() => foodsQuery.data?.items ?? [], [foodsQuery.data?.items]);
  const createMutation = useMutation({
    mutationFn: async () => await createIngredientAlias({ rawText: selectedRawText, foodId: selectedFoodId, backfillRecipes: true }),
    onSuccess: async () => {
      setStatus("Ingredient alias created.");
      await aliasesQuery.refetch();
      await unresolvedQuery.refetch();
    },
  });
  const deleteMutation = useMutation({
    mutationFn: deleteIngredientAlias,
    onSuccess: async () => await aliasesQuery.refetch(),
  });

  return (
    <SettingsPage user={user} title="Ingredient aliases" description="Resolve unresolved ingredient text and maintain existing alias mappings.">
      {status ? <Alert severity="success">{status}</Alert> : null}
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <TextField select label="Unresolved ingredient text" value={selectedRawText} onChange={event => setSelectedRawText(event.target.value)}>
              {(unresolvedQuery.data?.items ?? []).map(item => (
                <MenuItem key={item.rawText} value={item.rawText}>{item.rawText} ({item.count})</MenuItem>
              ))}
            </TextField>
            <TextField select label="Mapped food" value={selectedFoodId} onChange={event => setSelectedFoodId(event.target.value)}>
              {foodOptions.map(item => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
            </TextField>
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => createMutation.mutate()} disabled={!selectedRawText || !selectedFoodId}>
                Create alias
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
      <Stack spacing={2}>
        {(aliasesQuery.data?.items ?? []).map(item => (
          <Card key={item.id} variant="outlined">
            <CardContent>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                <Stack spacing={0.5} sx={{ flex: 1 }}>
                  <Typography variant="h6">{item.name}</Typography>
                  <Typography color="text.secondary">Mapped to {item.foodName}</Typography>
                </Stack>
                <Button color="error" variant="outlined" onClick={() => deleteMutation.mutate(item.id)}>Delete</Button>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </SettingsPage>
  );
}
