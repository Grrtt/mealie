import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createAdminGroup, deleteAdminGroup, fetchAdminGroups } from "@/features/settings/api";
import { apiClient } from "@/lib/api/client";
import { useState } from "react";

export function AdminManageGroupsRouteComponent() {
  const { data: user } = useCurrentUser();
  const [name, setName] = useState("");
  const [status, setStatus] = useState<string | null>(null);
  const groupsQuery = useQuery({ queryKey: ["admin-manage-groups"], queryFn: fetchAdminGroups });
  const createMutation = useMutation({
    mutationFn: async () => await createAdminGroup({ name }),
    onSuccess: async created => {
      setStatus(`Group created: ${created.name}`);
      setName("");
      await groupsQuery.refetch();
    },
  });
  const deleteMutation = useMutation({ mutationFn: deleteAdminGroup, onSuccess: async () => await groupsQuery.refetch() });

  return (
    <SettingsPage user={user} title="Manage groups" description="Create, inspect, and delete groups.">
      {status ? <Alert severity="success">{status}</Alert> : null}
      <Card>
        <CardContent>
          <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
            <TextField label="New group name" value={name} onChange={event => setName(event.target.value)} sx={{ flex: 1 }} />
            <Button variant="contained" onClick={() => createMutation.mutate()} disabled={!name.trim()}>Create group</Button>
          </Stack>
        </CardContent>
      </Card>
      <Stack spacing={2}>
        {(groupsQuery.data?.items ?? []).map(item => (
          <Card key={item.id} variant="outlined">
            <CardContent>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                <Stack spacing={0.5} sx={{ flex: 1 }}>
                  <Typography variant="h6">{item.name}</Typography>
                  <Typography color="text.secondary">{item.slug}</Typography>
                </Stack>
                <Button href={apiClient.resolvePath(`/admin/manage/groups/${item.id}`)}>Edit</Button>
                <Button color="error" variant="outlined" onClick={() => deleteMutation.mutate(item.id)}>Delete</Button>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </SettingsPage>
  );
}
