import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import FormControlLabel from "@mui/material/FormControlLabel";
import LinearProgress from "@mui/material/LinearProgress";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useNavigate, useParams } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createRecipeFromHtmlOrJson } from "@/features/recipes/api";

export function RecipeCreateHtmlRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/r/create/html" });
  const { data: user } = useCurrentUser();
  const navigate = useNavigate();
  const [url, setUrl] = useState("");
  const [data, setData] = useState("");
  const [includeTags, setIncludeTags] = useState(true);
  const [includeCategories, setIncludeCategories] = useState(true);
  const [progress, setProgress] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    document.title = "Import HTML/JSON Recipe · Mealie";
  }, []);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Import HTML/JSON">
      <Stack spacing={3}>
        <Typography variant="h4">Import recipe from HTML or JSON</Typography>
        {progress ? <LinearProgress /> : null}
        {progress ? <Alert severity="info">{progress}</Alert> : null}
        {error ? <Alert severity="error">{error}</Alert> : null}
        <TextField label="Source URL (optional)" value={url} onChange={event => setUrl(event.target.value)} />
        <TextField
          label="Raw HTML or JSON-LD"
          multiline
          minRows={12}
          value={data}
          onChange={event => setData(event.target.value)}
        />
        <FormControlLabel control={<Checkbox checked={includeTags} onChange={(_, checked) => setIncludeTags(checked)} />} label="Import tags" />
        <FormControlLabel control={<Checkbox checked={includeCategories} onChange={(_, checked) => setIncludeCategories(checked)} />} label="Import categories" />
        <Button
          variant="contained"
          disabled={!data.trim()}
          onClick={async () => {
            try {
              const slug = await createRecipeFromHtmlOrJson({
                data,
                url: url || null,
                includeTags,
                includeCategories,
              }, setProgress);
              await navigate({ href: `/g/${groupSlug}/r/${slug}?edit=1` });
            }
            catch (htmlError) {
              setError(htmlError instanceof Error ? htmlError.message : "Unable to import recipe");
            }
            finally {
              setProgress(null);
            }
          }}
        >
          Import recipe
        </Button>
      </Stack>
    </AppShell>
  );
}
