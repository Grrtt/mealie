import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardActions from "@mui/material/CardActions";
import CardContent from "@mui/material/CardContent";
import FormControlLabel from "@mui/material/FormControlLabel";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import {
  fetchGroup,
  fetchGroupPreferences,
  fetchGroupReports,
  fetchGroupStorage,
  updateGroupPreferences,
} from "@/features/settings/api";
import { apiClient } from "@/lib/api/client";

export function GroupRouteComponent() {
  const { data: user } = useCurrentUser();
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const groupQuery = useQuery({ queryKey: ["group-self"], queryFn: fetchGroup });
  const preferencesQuery = useQuery({ queryKey: ["group-preferences"], queryFn: fetchGroupPreferences });
  const storageQuery = useQuery({ queryKey: ["group-storage"], queryFn: fetchGroupStorage });
  const reportsQuery = useQuery({ queryKey: ["group-reports", null], queryFn: async () => await fetchGroupReports(null) });
  const [privateGroup, setPrivateGroup] = useState(false);
  const [showAnnouncements, setShowAnnouncements] = useState(false);

  useEffect(() => {
    setPrivateGroup(Boolean(preferencesQuery.data?.privateGroup));
    setShowAnnouncements(Boolean(preferencesQuery.data?.showAnnouncements));
  }, [preferencesQuery.data]);

  const mutation = useMutation({
    mutationFn: async () => await updateGroupPreferences({ privateGroup, showAnnouncements }),
    onSuccess: async () => {
      setStatus("Group preferences updated.");
      setError(null);
      await preferencesQuery.refetch();
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to update group preferences.");
    },
  });

  return (
    <SettingsPage
      user={user}
      title="Group settings"
      description={`Manage settings for ${groupQuery.data?.name ?? "the current group"} and keep organizer workflows reachable from the React frontend.`}
    >
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}
      <Card>
        <CardContent>
          <Stack spacing={1}>
            <FormControlLabel
              control={<Switch checked={privateGroup} onChange={event => setPrivateGroup(event.target.checked)} />}
              label="Private group"
            />
            <FormControlLabel
              control={<Switch checked={showAnnouncements} onChange={event => setShowAnnouncements(event.target.checked)} />}
              label="Show announcements"
            />
            <Typography color="text.secondary">
              Storage: {storageQuery.data?.usedStorageStr ?? "0 B"} / {storageQuery.data?.totalStorageStr ?? "Unknown"}
            </Typography>
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
                Save group settings
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Grid container spacing={2}>
        {[
          {
            title: "Group data",
            description: "Manage recipes, organizers, foods, units, labels, and recipe actions.",
            href: "/group/data",
          },
          {
            title: "Migration helper",
            description: "Upload migration archives and review generated reports.",
            href: "/group/migrations",
          },
        ].map(item => (
          <Grid key={item.href} size={{ xs: 12, md: 6 }}>
            <Card sx={{ height: "100%" }}>
              <CardContent>
                <Typography variant="h6">{item.title}</Typography>
                <Typography color="text.secondary">{item.description}</Typography>
              </CardContent>
              <CardActions>
                <Button href={apiClient.resolvePath(item.href)}>Open</Button>
              </CardActions>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Stack spacing={2}>
        <Typography variant="h6">Recent reports</Typography>
        {(reportsQuery.data ?? []).slice(0, 5).map(report => (
          <Card key={report.id} variant="outlined">
            <CardContent>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                <Stack spacing={0.5} sx={{ flex: 1 }}>
                  <Typography variant="h6">{report.name}</Typography>
                  <Typography color="text.secondary">
                    {report.category} · {report.status ?? "unknown"}
                  </Typography>
                </Stack>
                <Button href={apiClient.resolvePath(`/group/reports/${report.id}`)}>View report</Button>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </SettingsPage>
  );
}
