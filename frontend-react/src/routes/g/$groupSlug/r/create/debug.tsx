import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useParams } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { testRecipeScrapeUrl } from "@/features/recipes/api";

export function RecipeCreateDebugRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/r/create/debug" });
  const { data: user } = useCurrentUser();
  const [url, setUrl] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [preview, setPreview] = useState<unknown>(null);

  useEffect(() => {
    document.title = "Recipe Scraper Debug · Mealie";
  }, []);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Recipe Scraper Debug">
      <Stack spacing={3}>
        <Typography variant="h4">Scraper debug</Typography>
        {error ? <Alert severity="error">{error}</Alert> : null}
        <TextField label="Recipe URL" value={url} onChange={event => setUrl(event.target.value)} />
        <Button
          variant="contained"
          disabled={!url.trim()}
          onClick={async () => {
            try {
              setPreview(await testRecipeScrapeUrl(url.trim()));
              setError(null);
            }
            catch (debugError) {
              setError(debugError instanceof Error ? debugError.message : "Unable to preview scrape");
            }
          }}
        >
          Preview scrape
        </Button>
        {preview ? (
          <pre style={{ overflowX: "auto", whiteSpace: "pre-wrap" }}>
            {JSON.stringify(preview, null, 2)}
          </pre>
        ) : null}
      </Stack>
    </AppShell>
  );
}
