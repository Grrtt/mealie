import { useMemo, useState } from "react";
import AddRoundedIcon from "@mui/icons-material/AddRounded";
import CheckCircleRoundedIcon from "@mui/icons-material/CheckCircleRounded";
import DeleteRoundedIcon from "@mui/icons-material/DeleteRounded";
import EditRoundedIcon from "@mui/icons-material/EditRounded";
import RadioButtonUncheckedRoundedIcon from "@mui/icons-material/RadioButtonUncheckedRounded";
import SmartToyRoundedIcon from "@mui/icons-material/SmartToyRounded";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Divider from "@mui/material/Divider";
import FormControlLabel from "@mui/material/FormControlLabel";
import IconButton from "@mui/material/IconButton";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Dialog, DialogActions, DialogContent, DialogTitle } from "@/components/dialogs";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import {
  activateAiConfiguration,
  createAiConfiguration,
  deleteAiConfiguration,
  fetchAiConfigurations,
  updateAiConfiguration,
  type AiConfigurationResponse,
  type CreateAiConfigurationRequest,
  type UpdateAiConfigurationRequest,
} from "@/features/settings/api";

type AiConfigurationForm = {
  name: string;
  providerType: string;
  apiKey: string;
  projectId: string;
  baseUrl: string;
  defaultModel: string;
  enableImageServices: boolean;
  enableTranscriptionServices: boolean;
};

const providerTypes = [
  { label: "OpenAI", value: "openAi" },
  { label: "Azure OpenAI", value: "azureOpenAi" },
  { label: "Anthropic", value: "anthropic" },
  { label: "Ollama", value: "ollama" },
  { label: "Custom (OpenAI-compatible)", value: "custom" },
];

function defaultForm(): AiConfigurationForm {
  return {
    name: "",
    providerType: "openAi",
    apiKey: "",
    projectId: "",
    baseUrl: "",
    defaultModel: "",
    enableImageServices: true,
    enableTranscriptionServices: true,
  };
}

function subtitleParts(config: AiConfigurationResponse) {
  return [
    config.providerType,
    config.defaultModel,
    config.baseUrl,
    config.maskedApiKey,
  ].filter(Boolean).join(" · ");
}

export function AdminAiConfigurationsRouteComponent() {
  const { data: user } = useCurrentUser();
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [formDialogOpen, setFormDialogOpen] = useState(false);
  const [editingConfigId, setEditingConfigId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<AiConfigurationResponse | null>(null);
  const [form, setForm] = useState<AiConfigurationForm>(defaultForm);
  const query = useQuery({ queryKey: ["admin-ai-configurations"], queryFn: fetchAiConfigurations });
  const createMutation = useMutation({ mutationFn: createAiConfiguration });
  const updateMutation = useMutation({ mutationFn: async ({ id, payload }: { id: string; payload: UpdateAiConfigurationRequest }) => await updateAiConfiguration(id, payload) });
  const deleteMutation = useMutation({ mutationFn: deleteAiConfiguration });
  const activateMutation = useMutation({ mutationFn: activateAiConfiguration });

  const configs = query.data ?? [];
  const dialogTitle = editingConfigId ? "Edit AI provider" : "Add AI provider";
  const isSaving = createMutation.isPending || updateMutation.isPending;

  const deleteTargetName = deleteTarget?.name ?? "this provider";
  const pageDescription = useMemo(
    () => "Manage AI provider configurations and activate the default provider.",
    [],
  );

  async function refresh() {
    await query.refetch();
  }

  function openCreateDialog() {
    setForm(defaultForm());
    setEditingConfigId(null);
    setFormDialogOpen(true);
  }

  function openEditDialog(config: AiConfigurationResponse) {
    setForm({
      name: config.name,
      providerType: config.providerType,
      apiKey: "",
      projectId: config.projectId ?? "",
      baseUrl: config.baseUrl ?? "",
      defaultModel: config.defaultModel ?? "",
      enableImageServices: config.enableImageServices,
      enableTranscriptionServices: config.enableTranscriptionServices,
    });
    setEditingConfigId(config.id);
    setFormDialogOpen(true);
  }

  async function handleSave() {
    setError(null);

    try {
      if (editingConfigId) {
        await updateMutation.mutateAsync({
          id: editingConfigId,
          payload: {
            name: form.name || null,
            apiKey: form.apiKey || null,
            projectId: form.projectId || null,
            baseUrl: form.baseUrl || null,
            defaultModel: form.defaultModel || null,
            enableImageServices: form.enableImageServices,
            enableTranscriptionServices: form.enableTranscriptionServices,
          },
        });
        setStatus("AI provider updated");
      } else {
        const payload: CreateAiConfigurationRequest = {
          name: form.name,
          providerType: form.providerType,
          apiKey: form.apiKey || null,
          projectId: form.projectId || null,
          baseUrl: form.baseUrl || null,
          defaultModel: form.defaultModel || null,
          enableImageServices: form.enableImageServices,
          enableTranscriptionServices: form.enableTranscriptionServices,
        };
        await createMutation.mutateAsync(payload);
        setStatus("AI provider created");
      }

      setFormDialogOpen(false);
      await refresh();
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : "Unable to save AI provider");
    }
  }

  async function handleActivate(id: string) {
    setError(null);

    try {
      await activateMutation.mutateAsync(id);
      setStatus("AI provider activated");
      await refresh();
    } catch (activateError) {
      setError(activateError instanceof Error ? activateError.message : "Unable to activate AI provider");
    }
  }

  async function handleDelete() {
    if (!deleteTarget) return;

    setError(null);

    try {
      await deleteMutation.mutateAsync(deleteTarget.id);
      setStatus("AI provider deleted");
      setDeleteTarget(null);
      await refresh();
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Unable to delete AI provider");
    }
  }

  if (!user) {
    return (
      <Box sx={{ display: "grid", placeItems: "center", minHeight: "40vh" }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <AppShell groupSlug={user.groupSlug} userName={user.fullName} title="AI configurations">
      <Box sx={{ width: "100%", maxWidth: 960, mx: "auto" }}>
        <Stack spacing={3}>
          {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
          {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}
          {query.error ? <Alert severity="error">{query.error instanceof Error ? query.error.message : "Unable to load AI configurations"}</Alert> : null}

          <Box sx={{ borderBottom: 1, borderColor: "divider", pb: 2.5 }}>
            <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems={{ sm: "center" }}>
              <SmartToyRoundedIcon color="primary" sx={{ fontSize: 64, flexShrink: 0 }} />
              <Stack spacing={0.5}>
                <Typography variant="h4">AI configurations</Typography>
                <Typography color="text.secondary">
                  {pageDescription}
                </Typography>
              </Stack>
            </Stack>
          </Box>

          <Stack direction="row" justifyContent="flex-end">
            <Button onClick={openCreateDialog} startIcon={<AddRoundedIcon />} variant="contained">
              Add AI provider
            </Button>
          </Stack>

          {query.isLoading ? (
            <Box sx={{ display: "grid", placeItems: "center", minHeight: 240 }}>
              <CircularProgress />
            </Box>
          ) : configs.length ? (
            <Card variant="outlined">
              {configs.map((config, index) => (
                <Box key={config.id}>
                  <Box sx={{ px: { xs: 2, md: 3 }, py: 2.5 }}>
                    <Stack
                      direction={{ xs: "column", md: "row" }}
                      spacing={2}
                      alignItems={{ md: "center" }}
                      justifyContent="space-between"
                    >
                      <Stack direction="row" spacing={2} sx={{ minWidth: 0, flex: 1 }} alignItems="flex-start">
                        {config.isActive ? (
                          <CheckCircleRoundedIcon color="success" sx={{ mt: 0.25 }} />
                        ) : (
                          <RadioButtonUncheckedRoundedIcon color="disabled" sx={{ mt: 0.25 }} />
                        )}
                        <Stack spacing={0.5} sx={{ minWidth: 0 }}>
                          <Typography variant="h6">{config.name}</Typography>
                          <Typography color="text.secondary" variant="body2">
                            {subtitleParts(config)}
                          </Typography>
                        </Stack>
                      </Stack>

                      <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                        {config.isActive ? (
                          <Chip color="success" label="Active" size="small" variant="outlined" />
                        ) : (
                          <Button
                            color="info"
                            onClick={() => void handleActivate(config.id)}
                            size="small"
                            variant="outlined"
                            disabled={activateMutation.isPending}
                          >
                            Set active
                          </Button>
                        )}
                        <IconButton aria-label={`Edit ${config.name}`} onClick={() => openEditDialog(config)} size="small">
                          <EditRoundedIcon />
                        </IconButton>
                        <IconButton aria-label={`Delete ${config.name}`} color="error" onClick={() => setDeleteTarget(config)} size="small">
                          <DeleteRoundedIcon />
                        </IconButton>
                      </Stack>
                    </Stack>
                  </Box>
                  {index < configs.length - 1 ? <Divider /> : null}
                </Box>
              ))}
            </Card>
          ) : (
            <Card variant="outlined">
              <Box sx={{ px: 3, py: 8, textAlign: "center" }}>
                <Typography color="text.secondary">No AI providers configured yet.</Typography>
              </Box>
            </Card>
          )}
        </Stack>
      </Box>

      <Dialog open={formDialogOpen} onClose={() => setFormDialogOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>{dialogTitle}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              autoFocus
              label="Name"
              required
              value={form.name}
              onChange={event => setForm(current => ({ ...current, name: event.target.value }))}
            />
            <TextField
              select
              label="Provider type"
              value={form.providerType}
              onChange={event => setForm(current => ({ ...current, providerType: event.target.value }))}
            >
              {providerTypes.map(option => (
                <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>
              ))}
            </TextField>
            <TextField
              label={editingConfigId ? "API key (leave blank to keep current key)" : "API key"}
              type="password"
              autoComplete="new-password"
              value={form.apiKey}
              onChange={event => setForm(current => ({ ...current, apiKey: event.target.value }))}
            />
            <TextField
              label="Project ID (optional)"
              value={form.projectId}
              onChange={event => setForm(current => ({ ...current, projectId: event.target.value }))}
            />
            <TextField
              label="Base URL (optional)"
              value={form.baseUrl}
              onChange={event => setForm(current => ({ ...current, baseUrl: event.target.value }))}
            />
            <TextField
              label="Default model"
              value={form.defaultModel}
              onChange={event => setForm(current => ({ ...current, defaultModel: event.target.value }))}
            />
            <FormControlLabel
              control={(
                <Switch
                  checked={form.enableImageServices}
                  onChange={(_, checked) => setForm(current => ({ ...current, enableImageServices: checked }))}
                />
              )}
              label="Enable image services"
            />
            <FormControlLabel
              control={(
                <Switch
                  checked={form.enableTranscriptionServices}
                  onChange={(_, checked) => setForm(current => ({ ...current, enableTranscriptionServices: checked }))}
                />
              )}
              label="Enable transcription services"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setFormDialogOpen(false)}>Cancel</Button>
          <Button onClick={() => void handleSave()} disabled={isSaving || !form.name.trim()} variant="contained">
            Save
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={Boolean(deleteTarget)} onClose={() => setDeleteTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>Delete AI provider</DialogTitle>
        <DialogContent>
          <Typography color="text.secondary" sx={{ pt: 1 }}>
            Delete {deleteTargetName}?
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteTarget(null)}>Cancel</Button>
          <Button color="error" onClick={() => void handleDelete()} disabled={deleteMutation.isPending} variant="contained">
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </AppShell>
  );
}
