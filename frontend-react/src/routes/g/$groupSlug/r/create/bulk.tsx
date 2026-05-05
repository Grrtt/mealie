import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Checkbox from "@mui/material/Checkbox";
import FormControlLabel from "@mui/material/FormControlLabel";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useParams } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createRecipesFromUrls } from "@/features/recipes/api";
import { apiClient } from "@/lib/api/client";

export function RecipeCreateBulkRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/r/create/bulk" });
  const { data: user } = useCurrentUser();
  const [urls, setUrls] = useState("");
  const [includeTags, setIncludeTags] = useState(true);
  const [includeCategories, setIncludeCategories] = useState(true);
  const [results, setResults] = useState<Array<{ url: string; success: boolean; slug?: string; detail?: string }>>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    document.title = "Bulk Recipe Import · Mealie";
  }, []);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Bulk Recipe Import">
      <Stack spacing={3}>
        <Typography variant="h4">Bulk URL import</Typography>
        {error ? <Alert severity="error">{error}</Alert> : null}
        <TextField
          label="Recipe URLs"
          multiline
          minRows={8}
          helperText="Use one URL per line."
          value={urls}
          onChange={event => setUrls(event.target.value)}
        />
        <FormControlLabel control={<Checkbox checked={includeTags} onChange={(_, checked) => setIncludeTags(checked)} />} label="Import tags" />
        <FormControlLabel control={<Checkbox checked={includeCategories} onChange={(_, checked) => setIncludeCategories(checked)} />} label="Import categories" />
        <Button
          variant="contained"
          disabled={!urls.trim()}
          onClick={async () => {
            try {
              const response = await createRecipesFromUrls(
                urls.split("\n").map(line => line.trim()).filter(Boolean),
                includeTags,
                includeCategories,
              );
              setResults(response);
              setError(null);
            }
            catch (bulkError) {
              setError(bulkError instanceof Error ? bulkError.message : "Unable to import recipes");
            }
          }}
        >
          Import recipes
        </Button>
        <List>
          {results.map(result => (
            <ListItem key={result.url} sx={{ display: "block" }}>
              <Typography fontWeight={600}>{result.url}</Typography>
              <Typography color={result.success ? "success.main" : "error.main"}>
                {result.success && result.slug ? (
                  <a href={apiClient.resolvePath(`/g/${groupSlug}/r/${result.slug}`)}>Open imported recipe</a>
                ) : (
                  result.detail ?? "Import failed"
                )}
              </Typography>
            </ListItem>
          ))}
        </List>
      </Stack>
    </AppShell>
  );
}
