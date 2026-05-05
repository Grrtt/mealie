import TextField from "@mui/material/TextField";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SimpleCrudList } from "@/components/settings/SimpleCrudList";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createTool, deleteTool, fetchToolsPage, updateTool } from "@/features/settings/api";

export function GroupDataToolsRouteComponent() {
  const { data: user } = useCurrentUser();
  const query = useQuery({ queryKey: ["group-data-tools"], queryFn: fetchToolsPage });
  const refresh = async () => await query.refetch();
  const createMutation = useMutation({ mutationFn: createTool, onSuccess: refresh });
  const saveMutation = useMutation({ mutationFn: async ({ id, name }: { id: string; name: string }) => await updateTool(id, { name }), onSuccess: refresh });
  const deleteMutation = useMutation({ mutationFn: async (id: string) => await deleteTool(id), onSuccess: refresh });

  return (
    <SettingsPage user={user} title="Tools" description="Create and manage recipe tools.">
      <SimpleCrudList
        title="Existing tools"
        createLabel="New tool"
        items={query.data?.items ?? []}
        initialDraft={{ id: "new", name: "", groupId: "", slug: "", householdsWithTool: [] }}
        renderFields={(draft, setDraft) => (
          <TextField label="Name" value={draft.name} onChange={event => setDraft(current => ({ ...current, name: event.target.value }))} />
        )}
        getPrimaryText={item => item.name}
        getSecondaryText={item => item.slug}
        onCreate={draft => createMutation.mutate({ name: draft.name })}
        onSave={draft => saveMutation.mutate({ id: draft.id, name: draft.name })}
        onDelete={draft => deleteMutation.mutate(draft.id)}
      />
    </SettingsPage>
  );
}
