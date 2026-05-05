import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Checkbox from "@mui/material/Checkbox";
import Divider from "@mui/material/Divider";
import FormControlLabel from "@mui/material/FormControlLabel";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import {
  createHouseholdNotifier,
  deleteHouseholdNotifier,
  fetchHouseholdNotifiers,
  testHouseholdNotifier,
  updateHouseholdNotifier,
} from "@/features/settings/api";

type NotifierRecord = Awaited<ReturnType<typeof fetchHouseholdNotifiers>>["items"][number];

export function HouseholdNotifiersRouteComponent() {
  const { data: user } = useCurrentUser();
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [newName, setNewName] = useState("");
  const [newUrl, setNewUrl] = useState("");
  const notifiersQuery = useQuery({
    queryKey: ["household-notifiers"],
    queryFn: fetchHouseholdNotifiers,
  });
  const [notifiers, setNotifiers] = useState<NotifierRecord[]>([]);

  useEffect(() => {
    setNotifiers(notifiersQuery.data?.items ?? []);
  }, [notifiersQuery.data?.items]);

  const refresh = async () => {
    await notifiersQuery.refetch();
  };

  const createMutation = useMutation({
    mutationFn: async () => await createHouseholdNotifier({ name: newName, appriseUrl: newUrl || undefined }),
    onSuccess: async () => {
      setStatus("Notifier created.");
      setError(null);
      setNewName("");
      setNewUrl("");
      await refresh();
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to create notifier.");
    },
  });

  const saveMutation = useMutation({
    mutationFn: async (notifier: NotifierRecord) => await updateHouseholdNotifier(notifier.id, notifier),
    onSuccess: async () => {
      setStatus("Notifier updated.");
      setError(null);
      await refresh();
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to update notifier.");
    },
  });

  const testMutation = useMutation({
    mutationFn: async (id: string) => await testHouseholdNotifier(id),
    onSuccess: () => {
      setStatus("Notifier test sent.");
      setError(null);
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to send test notification.");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => await deleteHouseholdNotifier(id),
    onSuccess: async () => {
      setStatus("Notifier deleted.");
      setError(null);
      await refresh();
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to delete notifier.");
    },
  });

  const updateNotifier = (id: string, updater: (current: NotifierRecord) => NotifierRecord) => {
    setNotifiers(current => current.map(notifier => notifier.id === id ? updater(notifier) : notifier));
  };

  return (
    <SettingsPage
      user={user}
      title="Household notifiers"
      description="Configure Apprise-based event notifications and toggle the individual events that should emit notifications."
    >
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h6">Create notifier</Typography>
            <TextField label="Notifier name" value={newName} onChange={event => setNewName(event.target.value)} />
            <TextField label="Apprise URL" value={newUrl} onChange={event => setNewUrl(event.target.value)} />
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => createMutation.mutate()} disabled={createMutation.isPending || !newName.trim()}>
                Add notifier
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Stack spacing={2}>
        {notifiers.map(notifier => (
          <Card key={notifier.id} variant="outlined">
            <CardContent>
              <Stack spacing={2}>
                <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                  <TextField
                    label="Name"
                    value={notifier.name}
                    onChange={event => updateNotifier(notifier.id, current => ({ ...current, name: event.target.value }))}
                    sx={{ flex: 1 }}
                  />
                  <TextField
                    label="Apprise URL"
                    value={notifier.appriseUrl ?? ""}
                    onChange={event => updateNotifier(notifier.id, current => ({ ...current, appriseUrl: event.target.value }))}
                    sx={{ flex: 2 }}
                  />
                </Stack>
                <FormControlLabel
                  control={(
                    <Switch
                      checked={Boolean(notifier.enabled)}
                      onChange={event => updateNotifier(notifier.id, current => ({ ...current, enabled: event.target.checked }))}
                    />
                  )}
                  label="Enabled"
                />
                <Divider />
                <Grid container spacing={1}>
                  {Object.entries(notifier.options ?? {}).map(([key, value]) => (
                    <Grid key={key} size={{ xs: 12, md: 6 }}>
                      <FormControlLabel
                        control={(
                          <Checkbox
                            checked={Boolean(value)}
                            onChange={event => updateNotifier(notifier.id, current => ({
                              ...current,
                              options: {
                                ...(current.options ?? {}),
                                [key]: event.target.checked,
                              },
                            }))}
                          />
                        )}
                        label={key}
                      />
                    </Grid>
                  ))}
                </Grid>
                <Stack direction="row" spacing={1} justifyContent="flex-end">
                  <Button variant="outlined" onClick={() => testMutation.mutate(notifier.id)}>Test</Button>
                  <Button variant="contained" onClick={() => saveMutation.mutate(notifier)}>Save</Button>
                  <Button color="error" variant="outlined" onClick={() => deleteMutation.mutate(notifier.id)}>Delete</Button>
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </SettingsPage>
  );
}
