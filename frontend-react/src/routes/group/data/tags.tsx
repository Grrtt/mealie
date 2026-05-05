import TextField from "@mui/material/TextField";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SimpleCrudList } from "@/components/settings/SimpleCrudList";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createTag, deleteTag, fetchTagsPage, updateTag } from "@/features/settings/api";

export function GroupDataTagsRouteComponent() {
  const { data: user } = useCurrentUser();
  const query = useQuery({ queryKey: ["group-data-tags"], queryFn: fetchTagsPage });
  const refresh = async () => await query.refetch();
  const createMutation = useMutation({ mutationFn: createTag, onSuccess: refresh });
  const saveMutation = useMutation({ mutationFn: async ({ id, name }: { id: string; name: string }) => await updateTag(id, { name }), onSuccess: refresh });
  const deleteMutation = useMutation({ mutationFn: async (id: string) => await deleteTag(id), onSuccess: refresh });

  return (
    <SettingsPage user={user} title="Tags" description="Create and manage recipe tags.">
      <SimpleCrudList
        title="Existing tags"
        createLabel="New tag"
        items={query.data?.items ?? []}
        initialDraft={{ id: "new", name: "", groupId: "", slug: "" }}
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
