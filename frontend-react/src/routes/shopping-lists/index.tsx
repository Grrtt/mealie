import { useEffect, useMemo, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useSearch } from "@tanstack/react-router";
import { useMutation, useQuery } from "@tanstack/react-query";
import { AppShell } from "@/components/layout/AppShell";
import { WorkflowLinks } from "@/components/navigation/WorkflowLinks";
import { createShoppingList, deleteShoppingList, fetchShoppingLists } from "@/features/shopping/fromRecipe";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { apiClient } from "@/lib/api/client";

const showAllStorageKey = "react-migration-shopping-show-all";

export function ShoppingListsRouteComponent() {
  const { data: user } = useCurrentUser();
  const search = useSearch({ strict: false }) as { disableRedirect?: string | boolean };
  const [name, setName] = useState("");
  const [showAll, setShowAll] = useState(() => {
    if (typeof window === "undefined") return false;
    return window.localStorage.getItem(showAllStorageKey) === "true";
  });
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const listsQuery = useQuery({
    queryKey: ["shopping-lists"],
    queryFn: fetchShoppingLists,
  });

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!name.trim()) throw new Error("Enter a shopping list name.");
      return await createShoppingList({ name: name.trim() });
    },
    onSuccess: async created => {
      setStatus("Shopping list created");
      setName("");
      await listsQuery.refetch();
      if (typeof window !== "undefined") {
        window.location.assign(apiClient.resolvePath(`/shopping-lists/${created.id}`));
      }
    },
    onError: createError => {
      setError(createError instanceof Error ? createError.message : "Unable to create shopping list");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => await deleteShoppingList(id),
    onSuccess: async () => {
      setStatus("Shopping list deleted");
      await listsQuery.refetch();
    },
    onError: deleteError => {
      setError(deleteError instanceof Error ? deleteError.message : "Unable to delete shopping list");
    },
  });

  const visibleLists = useMemo(() => {
    const items = listsQuery.data?.items ?? [];
    return items.filter(list => showAll || list.userId === user?.id);
  }, [listsQuery.data?.items, showAll, user?.id]);

  useEffect(() => {
    if (typeof window !== "undefined") {
      window.localStorage.setItem(showAllStorageKey, String(showAll));
    }
  }, [showAll]);

  useEffect(() => {
    if (typeof window === "undefined") return;
    const disableRedirect = search.disableRedirect === true || search.disableRedirect === "true";

    if (!disableRedirect && visibleLists.length === 1) {
      window.location.replace(apiClient.resolvePath(`/shopping-lists/${visibleLists[0].id}`));
    }
  }, [search.disableRedirect, visibleLists]);

  if (listsQuery.isLoading) {
    return (
      <Box sx={{ display: "grid", placeItems: "center", minHeight: "40vh" }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <AppShell groupSlug={user?.groupSlug ?? "home"} userName={user?.fullName} title="Shopping lists">
      <Stack spacing={3}>
        {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
        {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}

        <Stack spacing={1}>
          <Typography variant="h4">Shopping lists</Typography>
          <Typography color="text.secondary">
            Keep direct list deep-links, new list creation, and recipe-to-list workflows available in the React frontend.
          </Typography>
        </Stack>

        <WorkflowLinks groupSlug={user?.groupSlug ?? "home"} />

        <Card>
          <CardContent>
            <Stack spacing={2}>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                <TextField
                  label="New shopping list"
                  value={name}
                  onChange={event => setName(event.target.value)}
                  sx={{ flex: 1 }}
                />
                <Button variant="contained" onClick={() => createMutation.mutate()} disabled={createMutation.isPending}>
                  Create list
                </Button>
              </Stack>
              <Stack direction="row" spacing={1} alignItems="center">
                <Switch checked={showAll} onChange={event => setShowAll(event.target.checked)} />
                <Typography>Show all household shopping lists</Typography>
              </Stack>
            </Stack>
          </CardContent>
        </Card>

        <Stack spacing={2}>
          {visibleLists.map(list => (
            <Card key={list.id} variant="outlined">
              <CardContent>
                <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                  <Stack spacing={0.5} sx={{ flex: 1 }}>
                    <Button href={apiClient.resolvePath(`/shopping-lists/${list.id}`)} sx={{ justifyContent: "flex-start", p: 0 }}>
                      {list.name ?? "Untitled list"}
                    </Button>
                    <Typography color="text.secondary">
                      {list.recipeReferences?.length ?? 0} linked recipe reference{(list.recipeReferences?.length ?? 0) === 1 ? "" : "s"}
                    </Typography>
                  </Stack>
                  <Button color="error" variant="outlined" onClick={() => deleteMutation.mutate(list.id)}>
                    Delete
                  </Button>
                </Stack>
              </CardContent>
            </Card>
          ))}
          {!visibleLists.length ? (
            <Typography color="text.secondary">No shopping lists found yet.</Typography>
          ) : null}
        </Stack>
      </Stack>
    </AppShell>
  );
}
