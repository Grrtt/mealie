import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useParams } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { attemptImageImport } from "@/features/recipes/api";

export function RecipeCreateImageRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/r/create/image" });
  const { data: user } = useCurrentUser();
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    document.title = "Import Recipe Image · Mealie";
  }, []);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Import Recipe Image">
      <Stack spacing={3}>
        <Typography variant="h4">Import recipe from image</Typography>
        <Typography color="text.secondary">
          Upload an image of a recipe and AI will extract the ingredients and instructions.
          Make sure the image is clear and well-lit for best results.
        </Typography>
        {status ? <Alert severity="success">{status}</Alert> : null}
        {error ? <Alert severity="error">{error}</Alert> : null}
        <Button component="label" variant="contained">
          Select image
          <input
            hidden
            type="file"
            accept="image/*"
            onChange={async event => {
              const file = event.target.files?.[0];
              if (!file) return;
              try {
                const response = await attemptImageImport(file);
                setStatus(response.detail);
                setError(null);
              }
              catch (imageError) {
                setError(imageError instanceof Error ? imageError.message : "Unable to import from image");
              }
            }}
          />
        </Button>
      </Stack>
    </AppShell>
  );
}
