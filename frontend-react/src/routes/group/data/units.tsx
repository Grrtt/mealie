import TextField from "@mui/material/TextField";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SimpleCrudList } from "@/components/settings/SimpleCrudList";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createUnit, deleteUnit, fetchUnitsPage, updateUnit } from "@/features/settings/api";

export function GroupDataUnitsRouteComponent() {
  const { data: user } = useCurrentUser();
  const query = useQuery({ queryKey: ["group-data-units"], queryFn: fetchUnitsPage });
  const refresh = async () => await query.refetch();
  const createMutation = useMutation({ mutationFn: createUnit, onSuccess: refresh });
  const saveMutation = useMutation({ mutationFn: async (draft: { id: string; name: string; abbreviation?: string }) => await updateUnit(draft.id, draft), onSuccess: refresh });
  const deleteMutation = useMutation({ mutationFn: async (id: string) => await deleteUnit(id), onSuccess: refresh });

  return (
    <SettingsPage user={user} title="Units" description="Manage ingredient units and abbreviations.">
      <SimpleCrudList
        title="Existing units"
        createLabel="New unit"
        items={query.data?.items ?? []}
        initialDraft={{ id: "new", name: "", abbreviation: "", pluralName: "", fraction: false }}
        renderFields={(draft, setDraft) => (
          <>
            <TextField label="Name" value={draft.name} onChange={event => setDraft(current => ({ ...current, name: event.target.value }))} />
            <TextField label="Abbreviation" value={draft.abbreviation ?? ""} onChange={event => setDraft(current => ({ ...current, abbreviation: event.target.value }))} />
          </>
        )}
        getPrimaryText={item => item.name}
        getSecondaryText={item => item.abbreviation ?? ""}
        onCreate={draft => createMutation.mutate({ name: draft.name, abbreviation: draft.abbreviation || undefined })}
        onSave={draft => saveMutation.mutate({ id: draft.id, name: draft.name, abbreviation: draft.abbreviation })}
        onDelete={draft => deleteMutation.mutate(draft.id)}
      />
    </SettingsPage>
  );
}
