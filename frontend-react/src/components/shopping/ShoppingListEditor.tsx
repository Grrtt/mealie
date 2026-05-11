import { useEffect, useMemo, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Checkbox from "@mui/material/Checkbox";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { Dialog, DialogActions, DialogContent, DialogTitle } from "@/components/dialogs";
import { useMutation, useQuery } from "@tanstack/react-query";
import { WorkflowLinks } from "@/components/navigation/WorkflowLinks";
import {
  addRecipesToShoppingList,
  createShoppingListItems,
  deleteShoppingListItems,
  fetchShoppingList,
  removeRecipeFromShoppingList,
  updateShoppingList,
  updateShoppingListItems,
} from "@/features/shopping/fromRecipe";
import { apiClient } from "@/lib/api/client";
import type { ShoppingListItemCreate, ShoppingListItemOut } from "@/lib/api/contracts";

type Props = {
  groupSlug: string;
  listId: string;
};

type ItemDraft = {
  id?: string;
  display: string;
  note: string;
  quantity: number;
  checked: boolean;
};

function sortByPosition(left: ShoppingListItemOut, right: ShoppingListItemOut) {
  return (left.position ?? 0) - (right.position ?? 0);
}

function itemTitle(item: ShoppingListItemOut) {
  return item.display || item.food?.name || item.note || "Shopping list item";
}

function itemSubtitle(item: ShoppingListItemOut) {
  const parts = [
    item.quantity ? String(item.quantity) : "",
    item.unit?.name || "",
    item.note || "",
  ].filter(Boolean);

  return parts.join(" · ");
}

export function ShoppingListEditor({ groupSlug, listId }: Props) {
  const [listName, setListName] = useState("");
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [createDraft, setCreateDraft] = useState<ItemDraft>({
    display: "",
    note: "",
    quantity: 1,
    checked: false,
  });
  const [editingItem, setEditingItem] = useState<ShoppingListItemOut | null>(null);
  const [itemDraft, setItemDraft] = useState<ItemDraft>({
    display: "",
    note: "",
    quantity: 1,
    checked: false,
  });

  const listQuery = useQuery({
    queryKey: ["shopping-list", listId],
    queryFn: async () => await fetchShoppingList(listId),
    refetchInterval: 30_000,
  });

  const list = listQuery.data;

  useEffect(() => {
    if (list?.name) {
      setListName(list.name);
    }
  }, [list?.name]);

  const refresh = async () => {
    await listQuery.refetch();
  };

  const groupedItems = useMemo(() => {
    const source = list?.listItems ?? [];
    const unchecked = source.filter(item => !item.checked).sort(sortByPosition);
    const checked = source.filter(item => item.checked).sort((left, right) => (right.updatedAt ?? "").localeCompare(left.updatedAt ?? ""));

    const grouped = unchecked.reduce<Record<string, ShoppingListItemOut[]>>((acc, item) => {
      const label = item.label?.name || "Other items";
      acc[label] ??= [];
      acc[label].push(item);
      return acc;
    }, {});

    return { grouped, checked };
  }, [list?.listItems]);

  const renameMutation = useMutation({
    mutationFn: async () => {
      if (!list) throw new Error("Shopping list data is unavailable.");
      return await updateShoppingList(list.id, {
        ...list,
        name: listName.trim(),
      });
    },
    onSuccess: async () => {
      setStatus("Shopping list renamed");
      await refresh();
    },
    onError: renameError => {
      setError(renameError instanceof Error ? renameError.message : "Unable to rename shopping list");
    },
  });

  const createItemMutation = useMutation({
    mutationFn: async () => {
      if (!list) throw new Error("Shopping list data is unavailable.");
      if (!createDraft.display.trim() && !createDraft.note.trim()) {
        throw new Error("Add an item name or note before saving.");
      }

      const payload: ShoppingListItemCreate = {
        shoppingListId: list.id,
        display: createDraft.display.trim() || createDraft.note.trim(),
        note: createDraft.note.trim() || undefined,
        quantity: createDraft.quantity || undefined,
        checked: false,
        position: (list.listItems ?? []).length,
      };

      return await createShoppingListItems([payload]);
    },
    onSuccess: async () => {
      setCreateDraft({ display: "", note: "", quantity: 1, checked: false });
      setStatus("Shopping list item created");
      await refresh();
    },
    onError: createError => {
      setError(createError instanceof Error ? createError.message : "Unable to create shopping list item");
    },
  });

  const updateItemMutation = useMutation({
    mutationFn: async (items: ShoppingListItemOut[]) => await updateShoppingListItems(items),
    onSuccess: async () => {
      setStatus("Shopping list updated");
      setEditingItem(null);
      await refresh();
    },
    onError: updateError => {
      setError(updateError instanceof Error ? updateError.message : "Unable to update shopping list");
    },
  });

  const deleteItemsMutation = useMutation({
    mutationFn: async (items: Array<Pick<ShoppingListItemOut, "id">>) => await deleteShoppingListItems(items),
    onSuccess: async () => {
      setStatus("Shopping list updated");
      await refresh();
    },
    onError: deleteError => {
      setError(deleteError instanceof Error ? deleteError.message : "Unable to delete shopping list items");
    },
  });

  const recipeReferenceMutation = useMutation({
    mutationFn: async (action: () => Promise<unknown>) => await action(),
    onSuccess: async () => {
      setStatus("Linked recipes updated");
      await refresh();
    },
    onError: recipeError => {
      setError(recipeError instanceof Error ? recipeError.message : "Unable to update linked recipes");
    },
  });

  function openItemEditor(item: ShoppingListItemOut) {
    setEditingItem(item);
    setItemDraft({
      id: item.id,
      display: item.display || item.food?.name || "",
      note: item.note || "",
      quantity: item.quantity ?? 1,
      checked: Boolean(item.checked),
    });
  }

  async function toggleChecked(item: ShoppingListItemOut, checked: boolean) {
    await updateItemMutation.mutateAsync([{ ...item, checked }]);
  }

  if (listQuery.isLoading || !list) {
    return (
      <Box sx={{ display: "grid", placeItems: "center", minHeight: "40vh" }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Stack spacing={3}>
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
              <Typography variant="h4">{list.name ?? "Shopping list"}</Typography>
              <Box sx={{ flexGrow: 1 }} />
              <Button href={apiClient.resolvePath("/shopping-lists?disableRedirect=true")} variant="outlined">
                All shopping lists
              </Button>
            </Stack>
            <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
              <TextField
                label="List name"
                value={listName}
                onChange={event => setListName(event.target.value)}
                sx={{ flex: 1 }}
              />
              <Button variant="contained" onClick={() => renameMutation.mutate()} disabled={renameMutation.isPending || !listName.trim()}>
                Save name
              </Button>
            </Stack>
            <Stack direction={{ xs: "column", md: "row" }} spacing={1} useFlexGap flexWrap="wrap">
              <Button
                variant="outlined"
                onClick={() => updateItemMutation.mutate((list.listItems ?? []).filter(item => !item.checked).map(item => ({ ...item, checked: true })))}
                disabled={updateItemMutation.isPending}
              >
                Check all
              </Button>
              <Button
                variant="outlined"
                onClick={() => updateItemMutation.mutate((list.listItems ?? []).filter(item => item.checked).map(item => ({ ...item, checked: false })))}
                disabled={updateItemMutation.isPending}
              >
                Uncheck all
              </Button>
              <Button
                color="error"
                variant="outlined"
                onClick={() => deleteItemsMutation.mutate(groupedItems.checked.map(item => ({ id: item.id })))}
                disabled={deleteItemsMutation.isPending || groupedItems.checked.length === 0}
              >
                Delete checked
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <WorkflowLinks groupSlug={groupSlug} />

      <Card variant="outlined">
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h6">Add item</Typography>
            <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
              <TextField
                label="Item"
                value={createDraft.display}
                onChange={event => setCreateDraft(current => ({ ...current, display: event.target.value }))}
                sx={{ flex: 1 }}
              />
              <TextField
                label="Quantity"
                type="number"
                value={createDraft.quantity}
                onChange={event => setCreateDraft(current => ({ ...current, quantity: Number(event.target.value) }))}
              />
            </Stack>
            <TextField
              label="Note"
              value={createDraft.note}
              onChange={event => setCreateDraft(current => ({ ...current, note: event.target.value }))}
            />
            <Button variant="contained" onClick={() => createItemMutation.mutate()} disabled={createItemMutation.isPending}>
              Add item
            </Button>
          </Stack>
        </CardContent>
      </Card>

      <Stack spacing={2}>
        {Object.entries(groupedItems.grouped).map(([label, items]) => (
          <Card key={label} variant="outlined">
            <CardContent>
              <Stack spacing={2}>
                <Typography variant="h6">{label}</Typography>
                {items.map(item => (
                  <Card key={item.id} variant="outlined">
                    <CardContent>
                      <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                        <Stack direction="row" spacing={1} alignItems="center" sx={{ flex: 1 }}>
                          <Checkbox
                            checked={Boolean(item.checked)}
                            onChange={event => void toggleChecked(item, event.target.checked)}
                          />
                          <Stack spacing={0.5}>
                            <Typography variant="subtitle1">{itemTitle(item)}</Typography>
                            {itemSubtitle(item) ? (
                              <Typography color="text.secondary">{itemSubtitle(item)}</Typography>
                            ) : null}
                          </Stack>
                        </Stack>
                        <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
                          <Button variant="outlined" onClick={() => openItemEditor(item)}>
                            Edit
                          </Button>
                          <Button color="error" variant="outlined" onClick={() => deleteItemsMutation.mutate([{ id: item.id }])}>
                            Delete
                          </Button>
                        </Stack>
                      </Stack>
                    </CardContent>
                  </Card>
                ))}
              </Stack>
            </CardContent>
          </Card>
        ))}

        {groupedItems.checked.length ? (
          <Card variant="outlined">
            <CardContent>
              <Stack spacing={2}>
                <Typography variant="h6">Checked items</Typography>
                {groupedItems.checked.map(item => (
                  <Card key={item.id} variant="outlined">
                    <CardContent>
                      <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                        <Stack direction="row" spacing={1} alignItems="center" sx={{ flex: 1 }}>
                          <Checkbox
                            checked={Boolean(item.checked)}
                            onChange={event => void toggleChecked(item, event.target.checked)}
                          />
                          <Stack spacing={0.5}>
                            <Typography variant="subtitle1" sx={{ textDecoration: "line-through" }}>
                              {itemTitle(item)}
                            </Typography>
                            {itemSubtitle(item) ? (
                              <Typography color="text.secondary">{itemSubtitle(item)}</Typography>
                            ) : null}
                          </Stack>
                        </Stack>
                        <Button color="error" variant="outlined" onClick={() => deleteItemsMutation.mutate([{ id: item.id }])}>
                          Delete
                        </Button>
                      </Stack>
                    </CardContent>
                  </Card>
                ))}
              </Stack>
            </CardContent>
          </Card>
        ) : null}
      </Stack>

      {(list.recipeReferences ?? []).length ? (
        <Card variant="outlined">
          <CardContent>
            <Stack spacing={2}>
              <Typography variant="h6">Linked recipes</Typography>
              {(list.recipeReferences ?? []).map(reference => (
                <Card key={reference.id} variant="outlined">
                  <CardContent>
                    <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                      <Stack spacing={0.5} sx={{ flex: 1 }}>
                        {reference.recipe.slug ? (
                          <Button
                            href={apiClient.resolvePath(`/g/${groupSlug}/r/${reference.recipe.slug}`)}
                            sx={{ justifyContent: "flex-start", p: 0 }}
                          >
                            {reference.recipe.name}
                          </Button>
                        ) : (
                          <Typography variant="subtitle1">{reference.recipe.name}</Typography>
                        )}
                        <Chip label={`Quantity: ${reference.recipeQuantity}`} size="small" sx={{ width: "fit-content" }} />
                      </Stack>
                      <Stack direction={{ xs: "column", md: "row" }} spacing={1}>
                        <Button
                          variant="outlined"
                          onClick={() => recipeReferenceMutation.mutate(async () => await removeRecipeFromShoppingList(list.id, reference.recipeId))}
                        >
                          -
                        </Button>
                        <Button
                          variant="outlined"
                          onClick={() => recipeReferenceMutation.mutate(async () => await addRecipesToShoppingList(list.id, [{ recipeId: reference.recipeId }]))}
                        >
                          +
                        </Button>
                      </Stack>
                    </Stack>
                  </CardContent>
                </Card>
              ))}
            </Stack>
          </CardContent>
        </Card>
      ) : null}

      <Dialog open={Boolean(editingItem)} onClose={() => setEditingItem(null)} fullWidth maxWidth="sm">
        <DialogTitle>Edit shopping list item</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              label="Item"
              value={itemDraft.display}
              onChange={event => setItemDraft(current => ({ ...current, display: event.target.value }))}
            />
            <TextField
              label="Quantity"
              type="number"
              value={itemDraft.quantity}
              onChange={event => setItemDraft(current => ({ ...current, quantity: Number(event.target.value) }))}
            />
            <TextField
              label="Note"
              value={itemDraft.note}
              onChange={event => setItemDraft(current => ({ ...current, note: event.target.value }))}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditingItem(null)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => {
              if (!editingItem) return;
              updateItemMutation.mutate([{
                ...editingItem,
                display: itemDraft.display.trim() || itemDraft.note.trim(),
                note: itemDraft.note.trim() || undefined,
                quantity: itemDraft.quantity || undefined,
              }]);
            }}
            disabled={updateItemMutation.isPending}
          >
            Save
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
