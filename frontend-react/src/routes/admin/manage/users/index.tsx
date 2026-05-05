import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { deleteAdminUser, fetchAdminUsers, unlockAllUsers } from "@/features/settings/api";
import { apiClient } from "@/lib/api/client";

export function AdminManageUsersRouteComponent() {
  const { data: user } = useCurrentUser();
  const usersQuery = useQuery({ queryKey: ["admin-manage-users"], queryFn: fetchAdminUsers });
  const unlockMutation = useMutation({ mutationFn: unlockAllUsers });
  const deleteMutation = useMutation({ mutationFn: deleteAdminUser, onSuccess: async () => await usersQuery.refetch() });

  return (
    <SettingsPage
      user={user}
      title="Manage users"
      description="Create, inspect, update, unlock, and delete users with the same backend contracts used by the legacy admin UI."
      actions={(
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" onClick={() => unlockMutation.mutate()}>Unlock all</Button>
          <Button variant="contained" href={apiClient.resolvePath("/admin/manage/users/create")}>Create user</Button>
        </Stack>
      )}
    >
      {unlockMutation.data?.unlocked != null ? (
        <Alert severity="success">Unlocked {unlockMutation.data.unlocked} user(s).</Alert>
      ) : null}
      <Stack spacing={2}>
        {(usersQuery.data?.items ?? []).map(item => (
          <Card key={item.id} variant="outlined">
            <CardContent>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                <Stack spacing={0.5} sx={{ flex: 1 }}>
                  <Typography variant="h6">{item.fullName ?? item.username ?? item.email}</Typography>
                  <Typography color="text.secondary">
                    {item.email} · {item.group} / {item.household} {item.admin ? "· admin" : ""}
                  </Typography>
                </Stack>
                <Button href={apiClient.resolvePath(`/admin/manage/users/${item.id}`)}>Edit</Button>
                <Button color="error" variant="outlined" onClick={() => deleteMutation.mutate(item.id)}>Delete</Button>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </SettingsPage>
  );
}
