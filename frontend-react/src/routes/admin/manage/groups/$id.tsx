import { useEffect, useState } from "react";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import FormControlLabel from "@mui/material/FormControlLabel";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import { useParams } from "@tanstack/react-router";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchAdminGroup, updateAdminGroup } from "@/features/settings/api";

export function AdminManageGroupDetailRouteComponent() {
  const { id } = useParams({ from: "/admin/manage/groups/$id" });
  const { data: user } = useCurrentUser();
  const groupQuery = useQuery({ queryKey: ["admin-group", id], queryFn: async () => await fetchAdminGroup(id) });
  const [form, setForm] = useState({ name: "", privateGroup: false, showAnnouncements: false });

  useEffect(() => {
    if (groupQuery.data) {
      setForm({
        name: groupQuery.data.name,
        privateGroup: Boolean(groupQuery.data.preferences?.privateGroup),
        showAnnouncements: Boolean(groupQuery.data.preferences?.showAnnouncements),
      });
    }
  }, [groupQuery.data]);

  const mutation = useMutation({
    mutationFn: async () => await updateAdminGroup(id, {
      ...groupQuery.data,
      name: form.name,
      preferences: {
        privateGroup: form.privateGroup,
        showAnnouncements: form.showAnnouncements,
        groupId: groupQuery.data?.id ?? id,
        id: groupQuery.data?.preferences?.id ?? groupQuery.data?.id ?? id,
      },
    }),
    onSuccess: async () => await groupQuery.refetch(),
  });

  return (
    <SettingsPage user={user} title="Edit group" description="Update group metadata and preferences.">
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <TextField label="Group name" value={form.name} onChange={event => setForm(current => ({ ...current, name: event.target.value }))} />
            <FormControlLabel control={<Switch checked={form.privateGroup} onChange={event => setForm(current => ({ ...current, privateGroup: event.target.checked }))} />} label="Private group" />
            <FormControlLabel control={<Switch checked={form.showAnnouncements} onChange={event => setForm(current => ({ ...current, showAnnouncements: event.target.checked }))} />} label="Show announcements" />
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => mutation.mutate()}>Save group</Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </SettingsPage>
  );
}
