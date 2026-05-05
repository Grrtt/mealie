import { useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Checkbox from "@mui/material/Checkbox";
import FormControlLabel from "@mui/material/FormControlLabel";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchGroupReports, startGroupMigration } from "@/features/settings/api";
import type { SupportedMigrations } from "../../../../frontend/app/lib/api/types/group";

const migrationOptions: SupportedMigrations[] = [
  "mealie_alpha",
  "chowdown",
  "copymethat",
  "myrecipebox",
  "nextcloud",
  "paprika",
  "plantoeat",
  "recipekeeper",
  "tandoor",
  "cookn",
];

export function GroupMigrationsRouteComponent() {
  const { data: user } = useCurrentUser();
  const [migrationType, setMigrationType] = useState<SupportedMigrations>("mealie_alpha");
  const [file, setFile] = useState<File | null>(null);
  const [addTag, setAddTag] = useState(false);
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const reportsQuery = useQuery({ queryKey: ["group-migration-reports"], queryFn: async () => await fetchGroupReports("migration") });
  const mutation = useMutation({
    mutationFn: async () => {
      if (!file) throw new Error("Choose a migration archive first.");
      return await startGroupMigration(migrationType, file, addTag);
    },
    onSuccess: async report => {
      setStatus(`Migration started: ${report.name}`);
      setError(null);
      setFile(null);
      await reportsQuery.refetch();
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to start migration.");
    },
  });

  return (
    <SettingsPage
      user={user}
      title="Migration helper"
      description="Upload supported migration archives and review the generated migration reports."
    >
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <TextField select label="Migration type" value={migrationType} onChange={event => setMigrationType(event.target.value as SupportedMigrations)}>
              {migrationOptions.map(option => <MenuItem key={option} value={option}>{option}</MenuItem>)}
            </TextField>
            <Button component="label" variant="outlined">
              {file ? file.name : "Choose archive"}
              <input hidden type="file" accept=".zip" onChange={event => setFile(event.target.files?.[0] ?? null)} />
            </Button>
            <FormControlLabel control={<Checkbox checked={addTag} onChange={event => setAddTag(event.target.checked)} />} label="Tag imported recipes with the migration type" />
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => mutation.mutate()} disabled={mutation.isPending || !file}>
                Start migration
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Stack spacing={2}>
        {(reportsQuery.data ?? []).map(report => (
          <Card key={report.id} variant="outlined">
            <CardContent>
              <Typography variant="h6">{report.name}</Typography>
              <Typography color="text.secondary">
                {report.status ?? "queued"} · processed {report.processedCount ?? 0} of {report.totalCount ?? 0}
              </Typography>
            </CardContent>
          </Card>
        ))}
        {!(reportsQuery.data?.length ?? 0) ? (
          <Typography color="text.secondary">No migration reports have been generated yet.</Typography>
        ) : null}
      </Stack>
    </SettingsPage>
  );
}
