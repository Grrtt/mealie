import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import FormControlLabel from "@mui/material/FormControlLabel";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import {
  createHouseholdWebhook,
  deleteHouseholdWebhook,
  fetchHouseholdWebhooks,
  testHouseholdWebhook,
  updateHouseholdWebhook,
} from "@/features/settings/api";

type WebhookRecord = Awaited<ReturnType<typeof fetchHouseholdWebhooks>>["items"][number];

export function HouseholdWebhooksRouteComponent() {
  const { data: user } = useCurrentUser();
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const webhooksQuery = useQuery({
    queryKey: ["household-webhooks"],
    queryFn: fetchHouseholdWebhooks,
  });
  const [webhooks, setWebhooks] = useState<WebhookRecord[]>([]);

  useEffect(() => {
    setWebhooks(webhooksQuery.data?.items ?? []);
  }, [webhooksQuery.data?.items]);

  const refresh = async () => {
    await webhooksQuery.refetch();
  };

  const createMutation = useMutation({
    mutationFn: async () => await createHouseholdWebhook({
      enabled: true,
      name: "New webhook",
      url: "https://example.com/webhook",
      webhookType: "mealplan",
      scheduledTime: "12:00",
    }),
    onSuccess: async () => {
      setStatus("Webhook created.");
      setError(null);
      await refresh();
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to create webhook.");
    },
  });

  const saveMutation = useMutation({
    mutationFn: async (webhook: WebhookRecord) => await updateHouseholdWebhook(webhook.id, webhook),
    onSuccess: async () => {
      setStatus("Webhook updated.");
      setError(null);
      await refresh();
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to update webhook.");
    },
  });

  const testMutation = useMutation({
    mutationFn: async (id: string) => await testHouseholdWebhook(id),
    onSuccess: () => {
      setStatus("Webhook test sent.");
      setError(null);
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to test webhook.");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => await deleteHouseholdWebhook(id),
    onSuccess: async () => {
      setStatus("Webhook deleted.");
      setError(null);
      await refresh();
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to delete webhook.");
    },
  });

  const updateWebhook = (id: string, updater: (current: WebhookRecord) => WebhookRecord) => {
    setWebhooks(current => current.map(webhook => webhook.id === id ? updater(webhook) : webhook));
  };

  return (
    <SettingsPage
      user={user}
      title="Household webhooks"
      description="Manage the same scheduled household webhooks exposed by the legacy frontend."
      actions={(
        <Button variant="contained" onClick={() => createMutation.mutate()} disabled={createMutation.isPending}>
          Add webhook
        </Button>
      )}
    >
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}
      <Stack spacing={2}>
        {webhooks.map(webhook => (
          <Card key={webhook.id} variant="outlined">
            <CardContent>
              <Stack spacing={2}>
                <Typography variant="h6">{webhook.name || "Webhook"}</Typography>
                <TextField
                  label="Name"
                  value={webhook.name ?? ""}
                  onChange={event => updateWebhook(webhook.id, current => ({ ...current, name: event.target.value }))}
                />
                <TextField
                  label="URL"
                  value={webhook.url ?? ""}
                  onChange={event => updateWebhook(webhook.id, current => ({ ...current, url: event.target.value }))}
                />
                <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
                  <TextField
                    label="Scheduled time"
                    value={webhook.scheduledTime}
                    onChange={event => updateWebhook(webhook.id, current => ({ ...current, scheduledTime: event.target.value }))}
                    sx={{ flex: 1 }}
                  />
                  <TextField
                    select
                    label="Webhook type"
                    value={webhook.webhookType ?? "mealplan"}
                    onChange={event => updateWebhook(webhook.id, current => ({ ...current, webhookType: event.target.value as "mealplan" }))}
                    sx={{ flex: 1 }}
                  >
                    <MenuItem value="mealplan">Meal plan</MenuItem>
                  </TextField>
                </Stack>
                <FormControlLabel
                  control={(
                    <Switch
                      checked={Boolean(webhook.enabled)}
                      onChange={event => updateWebhook(webhook.id, current => ({ ...current, enabled: event.target.checked }))}
                    />
                  )}
                  label="Enabled"
                />
                <Stack direction="row" spacing={1} justifyContent="flex-end">
                  <Button variant="outlined" onClick={() => testMutation.mutate(webhook.id)}>Test</Button>
                  <Button variant="contained" onClick={() => saveMutation.mutate(webhook)}>Save</Button>
                  <Button color="error" variant="outlined" onClick={() => deleteMutation.mutate(webhook.id)}>Delete</Button>
                </Stack>
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </SettingsPage>
  );
}
