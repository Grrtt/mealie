import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createAdminHousehold, deleteAdminHousehold, fetchAdminGroups, fetchAdminHouseholds } from "@/features/settings/api";
import { apiClient } from "@/lib/api/client";
import { useState } from "react";

export function AdminManageHouseholdsRouteComponent() {
  const { data: user } = useCurrentUser();
  const [name, setName] = useState("");
  const [groupId, setGroupId] = useState("");
  const [status, setStatus] = useState<string | null>(null);
  const groupsQuery = useQuery({ queryKey: ["admin-groups"], queryFn: fetchAdminGroups });
  const householdsQuery = useQuery({ queryKey: ["admin-manage-households"], queryFn: fetchAdminHouseholds });
  const createMutation = useMutation({
    mutationFn: async () => await createAdminHousehold({ name, groupId }),
    onSuccess: async created => {
      setStatus(`Household created: ${created.name}`);
      setName("");
      await householdsQuery.refetch();
    },
  });
  const deleteMutation = useMutation({ mutationFn: deleteAdminHousehold, onSuccess: async () => await householdsQuery.refetch() });

  return (
    <SettingsPage user={user} title="Manage households" description="Create, inspect, and delete households.">
      {status ? <Alert severity="success">{status}</Alert> : null}
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <TextField label="New household name" value={name} onChange={event => setName(event.target.value)} />
            <TextField select label="Group" value={groupId} onChange={event => setGroupId(event.target.value)}>
              {(groupsQuery.data?.items ?? []).map(item => <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>)}
            </TextField>
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => createMutation.mutate()} disabled={!name.trim() || !groupId}>Create household</Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
      <Stack spacing={2}>
        {(householdsQuery.data?.items ?? []).map(item => (
          <Card key={item.id} variant="outlined">
            <CardContent>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                <Stack spacing={0.5} sx={{ flex: 1 }}>
                  <Typography variant="h6">{item.name}</Typography>
                  <Typography color="text.secondary">{item.group}</Typography>
                </Stack>
                <Button href={apiClient.resolvePath(`/admin/manage/households/${item.id}`)}>Edit</Button>
                <Button color="error" variant="outlined" onClick={() => deleteMutation.mutate(item.id)}>Delete</Button>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </SettingsPage>
  );
}
