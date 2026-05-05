import TextField from "@mui/material/TextField";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SimpleCrudList } from "@/components/settings/SimpleCrudList";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createCategory, deleteCategory, fetchCategoriesPage, updateCategory } from "@/features/settings/api";

export function GroupDataCategoriesRouteComponent() {
  const { data: user } = useCurrentUser();
  const query = useQuery({ queryKey: ["group-data-categories"], queryFn: fetchCategoriesPage });
  const refresh = async () => await query.refetch();
  const createMutation = useMutation({ mutationFn: createCategory, onSuccess: refresh });
  const saveMutation = useMutation({ mutationFn: async ({ id, name }: { id: string; name: string }) => await updateCategory(id, { name }), onSuccess: refresh });
  const deleteMutation = useMutation({ mutationFn: async (id: string) => await deleteCategory(id), onSuccess: refresh });

  return (
    <SettingsPage user={user} title="Categories" description="Create and manage recipe categories.">
      <SimpleCrudList
        title="Existing categories"
        createLabel="New category"
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
