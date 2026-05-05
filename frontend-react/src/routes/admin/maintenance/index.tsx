import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchMaintenanceStorage, fetchMaintenanceSummary, runMaintenanceAction } from "@/features/settings/api";

export function AdminMaintenanceRouteComponent() {
  const { data: user } = useCurrentUser();
  const summaryQuery = useQuery({ queryKey: ["admin-maintenance-summary"], queryFn: fetchMaintenanceSummary });
  const storageQuery = useQuery({ queryKey: ["admin-maintenance-storage"], queryFn: fetchMaintenanceStorage });
  const mutation = useMutation({
    mutationFn: runMaintenanceAction,
    onSuccess: async () => {
      await summaryQuery.refetch();
      await storageQuery.refetch();
    },
  });

  return (
    <SettingsPage user={user} title="Maintenance" description="Review maintenance metrics and trigger the same cleanup actions offered by the legacy admin UI.">
      <Grid container spacing={2}>
        {[
          ["Data directory size", summaryQuery.data?.dataDirSize ?? "Unknown"],
          ["Cleanable directories", summaryQuery.data?.cleanableDirs ?? 0],
          ["Cleanable images", summaryQuery.data?.cleanableImages ?? 0],
        ].map(([label, value]) => (
          <Grid key={label} size={{ xs: 12, md: 4 }}>
            <Card>
              <CardContent>
                <Typography color="text.secondary">{label}</Typography>
                <Typography variant="h4">{String(value)}</Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Card>
        <CardContent>
          <Typography variant="h6">Storage details</Typography>
          <Typography component="pre" sx={{ whiteSpace: "pre-wrap", fontFamily: "monospace", fontSize: 12 }}>
            {JSON.stringify(storageQuery.data ?? {}, null, 2)}
          </Typography>
        </CardContent>
      </Card>

      <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
        <Button variant="outlined" onClick={() => mutation.mutate("recipe-folders")}>Clean recipe folders</Button>
        <Button variant="outlined" onClick={() => mutation.mutate("temp")}>Clean temp files</Button>
        <Button variant="outlined" onClick={() => mutation.mutate("images")}>Clean images</Button>
        <Button variant="outlined" onClick={() => mutation.mutate("logs")}>Clean logs</Button>
      </Stack>
    </SettingsPage>
  );
}
