import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import FormControlLabel from "@mui/material/FormControlLabel";
import LinearProgress from "@mui/material/LinearProgress";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useNavigate, useParams, useSearch } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createRecipeFromUrl } from "@/features/recipes/api";

export function RecipeCreateUrlRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/r/create/url" });
  const search = useSearch({ strict: false }) as { recipe_import_url?: string };
  const navigate = useNavigate();
  const { data: user } = useCurrentUser();
  const [url, setUrl] = useState(search.recipe_import_url ?? "");
  const [includeTags, setIncludeTags] = useState(true);
  const [includeCategories, setIncludeCategories] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    document.title = "Import Recipe URL · Mealie";
  }, []);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Import Recipe URL">
      <Stack spacing={3}>
        <Typography variant="h4">Import recipe from URL</Typography>
        {busy ? <LinearProgress /> : null}
        {error ? <Alert severity="error">{error}</Alert> : null}
        <TextField
          label="Recipe URL"
          value={url}
          onChange={event => setUrl(event.target.value)}
          helperText="Paste the source recipe URL to scrape and import it."
        />
        <FormControlLabel control={<Checkbox checked={includeTags} onChange={(_, checked) => setIncludeTags(checked)} />} label="Import tags" />
        <FormControlLabel control={<Checkbox checked={includeCategories} onChange={(_, checked) => setIncludeCategories(checked)} />} label="Import categories" />
        <Button
          variant="contained"
          disabled={busy || !url.trim()}
          onClick={async () => {
            try {
              setBusy(true);
              const slug = await createRecipeFromUrl(url.trim(), includeTags, includeCategories);
              await navigate({ href: `/g/${groupSlug}/r/${slug}?edit=1` });
            }
            catch (createError) {
              setError(createError instanceof Error ? createError.message : "Unable to import recipe");
            }
            finally {
              setBusy(false);
            }
          }}
        >
          Import recipe
        </Button>
      </Stack>
    </AppShell>
  );
}
