import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import {
  fetchAdminAbout,
  fetchAdminAnalytics,
  fetchAdminChecks,
  fetchSiteSettings,
  updateSiteSettings,
} from "@/features/settings/api";

export function AdminSiteSettingsRouteComponent() {
  const { data: user } = useCurrentUser();
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const aboutQuery = useQuery({ queryKey: ["admin-about"], queryFn: fetchAdminAbout });
  const checksQuery = useQuery({ queryKey: ["admin-checks"], queryFn: fetchAdminChecks });
  const analyticsQuery = useQuery({ queryKey: ["admin-analytics"], queryFn: fetchAdminAnalytics });
  const siteSettingsQuery = useQuery({ queryKey: ["admin-site-settings"], queryFn: fetchSiteSettings });
  const [form, setForm] = useState({
    defaultParser: "nlp",
    ingredientSystemPrompt: "",
    categorySystemPrompt: "",
    tagSystemPrompt: "",
  });

  useEffect(() => {
    if (siteSettingsQuery.data) {
      setForm({
        defaultParser: siteSettingsQuery.data.defaultParser,
        ingredientSystemPrompt: siteSettingsQuery.data.ingredientSystemPrompt ?? "",
        categorySystemPrompt: siteSettingsQuery.data.categorySystemPrompt ?? "",
        tagSystemPrompt: siteSettingsQuery.data.tagSystemPrompt ?? "",
      });
    }
  }, [siteSettingsQuery.data]);

  const mutation = useMutation({
    mutationFn: async () => await updateSiteSettings(form),
    onSuccess: async () => {
      setStatus("Site settings updated.");
      setError(null);
      await siteSettingsQuery.refetch();
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to update site settings.");
    },
  });

  return (
    <SettingsPage
      user={user}
      title="Admin site settings"
      description="Keep the administrative site settings, diagnostics, and baseline environment checks available during the React migration."
    >
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <TextField label="Default parser" value={form.defaultParser} onChange={event => setForm(current => ({ ...current, defaultParser: event.target.value }))} />
            <TextField
              label="Ingredient prompt"
              value={form.ingredientSystemPrompt}
              multiline
              minRows={3}
              onChange={event => setForm(current => ({ ...current, ingredientSystemPrompt: event.target.value }))}
            />
            <TextField
              label="Category prompt"
              value={form.categorySystemPrompt}
              multiline
              minRows={3}
              onChange={event => setForm(current => ({ ...current, categorySystemPrompt: event.target.value }))}
            />
            <TextField
              label="Tag prompt"
              value={form.tagSystemPrompt}
              multiline
              minRows={3}
              onChange={event => setForm(current => ({ ...current, tagSystemPrompt: event.target.value }))}
            />
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
                Save site settings
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Grid container spacing={2}>
        {([
          ["About", aboutQuery.data],
          ["Checks", checksQuery.data],
          ["Analytics", analyticsQuery.data],
        ] as Array<[string, unknown]>).map(([title, data]) => (
          <Grid key={title} size={{ xs: 12, md: 4 }}>
            <Card sx={{ height: "100%" }}>
              <CardContent>
                <Typography variant="h6">{title}</Typography>
                <Typography component="pre" sx={{ whiteSpace: "pre-wrap", fontFamily: "monospace", fontSize: 12 }}>
                  {JSON.stringify(data ?? {}, null, 2)}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </SettingsPage>
  );
}
