import TextField from "@mui/material/TextField";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SimpleCrudList } from "@/components/settings/SimpleCrudList";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createLabel, deleteLabel, fetchLabelsPage, updateLabel } from "@/features/settings/api";

export function GroupDataLabelsRouteComponent() {
  const { data: user } = useCurrentUser();
  const query = useQuery({ queryKey: ["group-data-labels"], queryFn: fetchLabelsPage });
  const refresh = async () => await query.refetch();
  const createMutation = useMutation({ mutationFn: createLabel, onSuccess: refresh });
  const saveMutation = useMutation({
    mutationFn: async (draft: { id: string; name: string; color?: string; groupId?: string }) => await updateLabel(draft.id, { ...draft, groupId: draft.groupId ?? "" }),
    onSuccess: refresh,
  });
  const deleteMutation = useMutation({ mutationFn: async (id: string) => await deleteLabel(id), onSuccess: refresh });

  return (
    <SettingsPage user={user} title="Labels" description="Create and manage reusable group labels.">
      <SimpleCrudList
        title="Existing labels"
        createLabel="New label"
        items={query.data?.items ?? []}
        initialDraft={{ id: "new", name: "", color: "#4f46e5", groupId: "" }}
        renderFields={(draft, setDraft) => (
          <>
            <TextField label="Name" value={draft.name} onChange={event => setDraft(current => ({ ...current, name: event.target.value }))} />
            <TextField label="Color" value={draft.color ?? ""} onChange={event => setDraft(current => ({ ...current, color: event.target.value }))} />
          </>
        )}
        getPrimaryText={item => item.name}
        getSecondaryText={item => item.color ?? ""}
        onCreate={draft => createMutation.mutate({ name: draft.name, color: draft.color })}
        onSave={draft => saveMutation.mutate({ id: draft.id, name: draft.name, color: draft.color, groupId: draft.groupId })}
        onDelete={draft => deleteMutation.mutate(draft.id)}
      />
    </SettingsPage>
  );
}
