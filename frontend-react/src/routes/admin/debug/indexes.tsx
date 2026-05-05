import { useState } from "react";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { deleteIndex, fetchIndexes, rebuildIndex, searchIndex } from "@/features/settings/api";

export function AdminDebugIndexesRouteComponent() {
  const { data: user } = useCurrentUser();
  const [queryText, setQueryText] = useState("");
  const [searchResults, setSearchResults] = useState<Record<string, unknown[]>>({});
  const indexesQuery = useQuery({ queryKey: ["admin-indexes"], queryFn: fetchIndexes });
  const rebuildMutation = useMutation({ mutationFn: rebuildIndex, onSuccess: async () => await indexesQuery.refetch() });
  const deleteMutation = useMutation({ mutationFn: deleteIndex, onSuccess: async () => await indexesQuery.refetch() });
  const searchMutation = useMutation({
    mutationFn: async (name: string) => await searchIndex(name, queryText),
    onSuccess: (result, name) => setSearchResults(current => ({ ...current, [name]: result.results ?? [] })),
  });

  return (
    <SettingsPage user={user} title="Search indexes" description="Inspect, rebuild, delete, and search backend indexes.">
      <TextField label="Search query" value={queryText} onChange={event => setQueryText(event.target.value)} />
      <Stack spacing={2}>
        {(indexesQuery.data ?? []).map(index => (
          <Card key={index.name} variant="outlined">
            <CardContent>
              <Stack spacing={2}>
                <Typography variant="h6">{index.name}</Typography>
                <Typography color="text.secondary">
                  {index.documentCount ?? 0} documents · {index.directorySizeBytes ?? 0} bytes
                </Typography>
                <Stack direction="row" spacing={1} justifyContent="flex-end">
                  <Button variant="outlined" onClick={() => searchMutation.mutate(index.name)} disabled={!queryText}>Search</Button>
                  <Button variant="contained" onClick={() => rebuildMutation.mutate(index.name)}>Rebuild</Button>
                  <Button color="error" variant="outlined" onClick={() => deleteMutation.mutate(index.name)}>Delete</Button>
                </Stack>
                {searchResults[index.name]?.length ? (
                  <Typography component="pre" sx={{ whiteSpace: "pre-wrap", fontFamily: "monospace", fontSize: 12 }}>
                    {JSON.stringify(searchResults[index.name], null, 2)}
                  </Typography>
                ) : null}
              </Stack>
            </CardContent>
          </Card>
        ))}
      </Stack>
    </SettingsPage>
  );
}
