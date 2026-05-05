import { useEffect, useState } from "react";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import { useParams } from "@tanstack/react-router";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchAdminGroups, fetchAdminHousehold, updateAdminHousehold } from "@/features/settings/api";

export function AdminManageHouseholdDetailRouteComponent() {
  const { id } = useParams({ from: "/admin/manage/households/$id" });
  const { data: user } = useCurrentUser();
  const householdQuery = useQuery({ queryKey: ["admin-household", id], queryFn: async () => await fetchAdminHousehold(id) });
  const groupsQuery = useQuery({ queryKey: ["admin-groups"], queryFn: fetchAdminGroups });
  const [form, setForm] = useState({ name: "", groupId: "" });

  useEffect(() => {
    if (householdQuery.data) {
      setForm({ name: householdQuery.data.name, groupId: householdQuery.data.groupId });
    }
  }, [householdQuery.data]);

  const mutation = useMutation({
    mutationFn: async () => await updateAdminHousehold(id, { ...householdQuery.data, name: form.name, groupId: form.groupId }),
    onSuccess: async () => await householdQuery.refetch(),
  });

  return (
    <SettingsPage user={user} title="Edit household" description="Update household metadata and group assignment.">
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <TextField label="Household name" value={form.name} onChange={event => setForm(current => ({ ...current, name: event.target.value }))} />
            <TextField select label="Group" value={form.groupId} onChange={event => setForm(current => ({ ...current, groupId: event.target.value }))}>
              {(groupsQuery.data?.items ?? []).map(item => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
            </TextField>
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => mutation.mutate()}>Save household</Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </SettingsPage>
  );
}
