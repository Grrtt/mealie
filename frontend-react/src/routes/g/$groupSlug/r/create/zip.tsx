import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useParams } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { importRecipesZip } from "@/features/recipes/api";

export function RecipeCreateZipRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/r/create/zip" });
  const { data: user } = useCurrentUser();
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    document.title = "Import Recipe ZIP · Mealie";
  }, []);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Import Recipe ZIP">
      <Stack spacing={3}>
        <Typography variant="h4">Import recipe archive</Typography>
        {status ? <Alert severity="success">{status}</Alert> : null}
        {error ? <Alert severity="error">{error}</Alert> : null}
        <Button component="label" variant="contained">
          Select ZIP archive
          <input
            hidden
            type="file"
            accept=".zip,application/zip"
            onChange={async event => {
              const file = event.target.files?.[0];
              if (!file) return;
              try {
                const response = await importRecipesZip(file);
                setStatus(`Imported ${response.imported} recipe(s)`);
                setError(null);
              }
              catch (zipError) {
                setError(zipError instanceof Error ? zipError.message : "Unable to import archive");
              }
            }}
          />
        </Button>
      </Stack>
    </AppShell>
  );
}
