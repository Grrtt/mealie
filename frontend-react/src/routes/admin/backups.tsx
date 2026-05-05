import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { backupDownloadPath, createBackup, deleteBackup, fetchBackups, restoreBackup } from "@/features/settings/api";

export function AdminBackupsRouteComponent() {
  const { data: user } = useCurrentUser();
  const backupsQuery = useQuery({ queryKey: ["admin-backups"], queryFn: fetchBackups });
  const createMutation = useMutation({ mutationFn: createBackup, onSuccess: async () => await backupsQuery.refetch() });
  const deleteMutation = useMutation({ mutationFn: deleteBackup, onSuccess: async () => await backupsQuery.refetch() });
  const restoreMutation = useMutation({ mutationFn: restoreBackup });

  return (
    <SettingsPage
      user={user}
      title="Backups"
      description="Create, download, delete, and restore application backups from the React migration workspace."
      actions={(
        <Button variant="contained" onClick={() => createMutation.mutate()} disabled={createMutation.isPending}>
          Create backup
        </Button>
      )}
    >
      {restoreMutation.isSuccess ? <Alert severity="success">Backup restore started.</Alert> : null}
      <Stack spacing={2}>
        {(backupsQuery.data?.imports ?? []).map(backup => (
          <Card key={backup.name} variant="outlined">
            <CardContent>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                <Stack spacing={0.5} sx={{ flex: 1 }}>
                  <Typography variant="h6">{backup.name}</Typography>
                  <Typography color="text.secondary">
                    {backup.date} · {backup.size}
                  </Typography>
                </Stack>
                <Button href={backupDownloadPath(backup.name)}>Download</Button>
                <Button variant="outlined" onClick={() => restoreMutation.mutate(backup.name)}>Restore</Button>
                <Button color="error" variant="outlined" onClick={() => deleteMutation.mutate(backup.name)}>Delete</Button>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </SettingsPage>
  );
}
