import { useEffect, useMemo, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Grid from "@mui/material/Grid";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import {
  fetchAdminAbout,
  fetchAdminAnalytics,
  fetchAiConfigurations,
  fetchAdminChecks,
  fetchSiteSettings,
  updateSiteSettings,
} from "@/features/settings/api";

const builtInParserOptions = [
  {
    value: "nlp",
    label: "NLP parser",
    description: "Use the built-in structured ingredient parser.",
  },
  {
    value: "brute",
    label: "Brute parser",
    description: "Use the fallback parser when structured parsing is not preferred.",
  },
];

export function AdminSiteSettingsRouteComponent() {
  const { data: user } = useCurrentUser();
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const aboutQuery = useQuery({ queryKey: ["admin-about"], queryFn: fetchAdminAbout });
  const checksQuery = useQuery({ queryKey: ["admin-checks"], queryFn: fetchAdminChecks });
  const analyticsQuery = useQuery({ queryKey: ["admin-analytics"], queryFn: fetchAdminAnalytics });
  const siteSettingsQuery = useQuery({ queryKey: ["admin-site-settings"], queryFn: fetchSiteSettings });
  const aiConfigurationsQuery = useQuery({ queryKey: ["admin-ai-configurations"], queryFn: fetchAiConfigurations });
  const [form, setForm] = useState({
    defaultParser: "nlp",
    ingredientSystemPrompt: "",
    categorySystemPrompt: "",
    tagSystemPrompt: "",
  });

  function promptValue(customValue: string | null | undefined, defaultValue: string | undefined) {
    return customValue ?? defaultValue ?? "";
  }

  useEffect(() => {
    if (siteSettingsQuery.data) {
      setForm({
        defaultParser: siteSettingsQuery.data.defaultParser,
        ingredientSystemPrompt: promptValue(
          siteSettingsQuery.data.ingredientSystemPrompt,
          siteSettingsQuery.data.defaultIngredientSystemPrompt,
        ),
        categorySystemPrompt: promptValue(
          siteSettingsQuery.data.categorySystemPrompt,
          siteSettingsQuery.data.defaultCategorySystemPrompt,
        ),
        tagSystemPrompt: promptValue(
          siteSettingsQuery.data.tagSystemPrompt,
          siteSettingsQuery.data.defaultTagSystemPrompt,
        ),
      });
    }
  }, [siteSettingsQuery.data]);

  const parserOptions = useMemo(() => {
    const aiOptions = (aiConfigurationsQuery.data ?? []).map(config => ({
      value: config.id,
      label: config.name,
      description: `AI provider (${config.providerType})`,
    }));
    const options = [...builtInParserOptions, ...aiOptions];
    const hasCurrentOption = options.some(option => option.value === form.defaultParser);

    if (!hasCurrentOption && siteSettingsQuery.data?.defaultParserUnavailable) {
      options.unshift({
        value: form.defaultParser,
        label: "Missing AI provider",
        description: "The saved AI configuration no longer exists. Choose a replacement and save.",
      });
    }

    return options;
  }, [aiConfigurationsQuery.data, form.defaultParser, siteSettingsQuery.data?.defaultParserUnavailable]);

  const mutation = useMutation({
    mutationFn: async () => {
      const settings = siteSettingsQuery.data;
      if (!settings) {
        throw new Error("Site settings are unavailable.");
      }

      const resolvePromptUpdate = (
        value: string,
        currentValue: string | null,
        defaultValue: string,
      ) => {
        if (value === "") {
          return "";
        }

        if (currentValue == null && value === defaultValue) {
          return null;
        }

        return value;
      };

      return await updateSiteSettings({
        defaultParser: form.defaultParser,
        ingredientSystemPrompt: resolvePromptUpdate(
          form.ingredientSystemPrompt,
          settings.ingredientSystemPrompt,
          settings.defaultIngredientSystemPrompt,
        ),
        categorySystemPrompt: resolvePromptUpdate(
          form.categorySystemPrompt,
          settings.categorySystemPrompt,
          settings.defaultCategorySystemPrompt,
        ),
        tagSystemPrompt: resolvePromptUpdate(
          form.tagSystemPrompt,
          settings.tagSystemPrompt,
          settings.defaultTagSystemPrompt,
        ),
      });
    },
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
      {aiConfigurationsQuery.error ? (
        <Alert severity="error">
          {aiConfigurationsQuery.error instanceof Error ? aiConfigurationsQuery.error.message : "Unable to load AI providers."}
        </Alert>
      ) : null}

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <TextField
              select
              label="Default parser"
              value={form.defaultParser}
              onChange={event => setForm(current => ({ ...current, defaultParser: event.target.value }))}
              helperText={siteSettingsQuery.data?.defaultParserUnavailable
                ? "The saved AI provider is no longer available. Choose another parser and save."
                : "Choose a built-in parser or one of your saved AI providers."}
            >
              {parserOptions.map(option => (
                <MenuItem key={option.value} value={option.value}>
                  {option.label}
                  {option.description ? ` — ${option.description}` : ""}
                </MenuItem>
              ))}
            </TextField>
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
