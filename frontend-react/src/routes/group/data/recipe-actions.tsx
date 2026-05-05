import MenuItem from "@mui/material/MenuItem";
import TextField from "@mui/material/TextField";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SimpleCrudList } from "@/components/settings/SimpleCrudList";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import {
  createGroupRecipeAction,
  deleteGroupRecipeAction,
  fetchGroupRecipeActions,
  updateGroupRecipeAction,
} from "@/features/settings/api";

export function GroupDataRecipeActionsRouteComponent() {
  const { data: user } = useCurrentUser();
  const query = useQuery({ queryKey: ["group-data-recipe-actions"], queryFn: fetchGroupRecipeActions });
  const refresh = async () => await query.refetch();
  const createMutation = useMutation({ mutationFn: createGroupRecipeAction, onSuccess: refresh });
  const saveMutation = useMutation({
    mutationFn: async (draft: { id: string; title: string; url: string; actionType: "link" | "post" }) => await updateGroupRecipeAction(draft.id, draft),
    onSuccess: refresh,
  });
  const deleteMutation = useMutation({ mutationFn: async (id: string) => await deleteGroupRecipeAction(id), onSuccess: refresh });

  return (
    <SettingsPage user={user} title="Recipe actions" description="Manage external recipe actions available from recipe detail pages.">
      <SimpleCrudList
        title="Existing recipe actions"
        createLabel="New recipe action"
        items={query.data?.items ?? []}
        initialDraft={{ id: "new", title: "", url: "", actionType: "link" as const, groupId: "", householdId: "" }}
        renderFields={(draft, setDraft) => (
          <>
            <TextField label="Title" value={draft.title} onChange={event => setDraft(current => ({ ...current, title: event.target.value }))} />
            <TextField label="URL" value={draft.url} onChange={event => setDraft(current => ({ ...current, url: event.target.value }))} />
            <TextField
              select
              label="Action type"
              value={draft.actionType}
              onChange={event => setDraft(current => ({ ...current, actionType: event.target.value as "link" | "post" }))}
            >
              <MenuItem value="link">Link</MenuItem>
              <MenuItem value="post">POST</MenuItem>
            </TextField>
          </>
        )}
        getPrimaryText={item => item.title}
        getSecondaryText={item => `${item.actionType} · ${item.url}`}
        onCreate={draft => createMutation.mutate({ title: draft.title, url: draft.url, actionType: draft.actionType as "link" | "post" })}
        onSave={draft => saveMutation.mutate({ id: draft.id, title: draft.title, url: draft.url, actionType: draft.actionType as "link" | "post" })}
        onDelete={draft => deleteMutation.mutate(draft.id)}
      />
    </SettingsPage>
  );
}
