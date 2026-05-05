import MenuItem from "@mui/material/MenuItem";
import TextField from "@mui/material/TextField";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SimpleCrudList } from "@/components/settings/SimpleCrudList";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createFood, deleteFood, fetchFoodsPage, fetchLabelsPage, updateFood } from "@/features/settings/api";

export function GroupDataFoodsRouteComponent() {
  const { data: user } = useCurrentUser();
  const foodsQuery = useQuery({ queryKey: ["group-data-foods"], queryFn: fetchFoodsPage });
  const labelsQuery = useQuery({ queryKey: ["group-data-food-labels"], queryFn: fetchLabelsPage });
  const refresh = async () => await foodsQuery.refetch();
  const createMutation = useMutation({ mutationFn: createFood, onSuccess: refresh });
  const saveMutation = useMutation({ mutationFn: async (draft: { id: string; name: string; pluralName?: string | null; labelId?: string | null }) => await updateFood(draft.id, draft), onSuccess: refresh });
  const deleteMutation = useMutation({ mutationFn: async (id: string) => await deleteFood(id), onSuccess: refresh });

  return (
    <SettingsPage user={user} title="Foods" description="Manage ingredient foods and optional label assignments.">
      <SimpleCrudList
        title="Existing foods"
        createLabel="New food"
        items={foodsQuery.data?.items ?? []}
        initialDraft={{ id: "new", name: "", pluralName: "", description: "", labelId: "", aliases: [] }}
        renderFields={(draft, setDraft) => (
          <>
            <TextField label="Name" value={draft.name} onChange={event => setDraft(current => ({ ...current, name: event.target.value }))} />
            <TextField label="Plural name" value={draft.pluralName ?? ""} onChange={event => setDraft(current => ({ ...current, pluralName: event.target.value }))} />
            <TextField
              select
              label="Label"
              value={draft.labelId ?? ""}
              onChange={event => setDraft(current => ({ ...current, labelId: event.target.value || null }))}
            >
              <MenuItem value="">None</MenuItem>
              {(labelsQuery.data?.items ?? []).map(label => (
                <MenuItem key={label.id} value={label.id}>{label.name}</MenuItem>
              ))}
            </TextField>
          </>
        )}
        getPrimaryText={item => item.name}
        getSecondaryText={item => item.label?.name ?? item.pluralName ?? ""}
        onCreate={draft => createMutation.mutate({ name: draft.name, pluralName: draft.pluralName || undefined, labelId: draft.labelId || undefined })}
        onSave={draft => saveMutation.mutate({ id: draft.id, name: draft.name, pluralName: draft.pluralName, labelId: draft.labelId })}
        onDelete={draft => deleteMutation.mutate(draft.id)}
      />
    </SettingsPage>
  );
}
