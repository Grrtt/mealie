import { useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useMutation } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { debugOpenAi } from "@/features/settings/api";

export function AdminDebugOpenAiRouteComponent() {
  const { data: user } = useCurrentUser();
  const [file, setFile] = useState<File | null>(null);
  const mutation = useMutation({
    mutationFn: async () => await debugOpenAi(file),
  });

  return (
    <SettingsPage user={user} title="OpenAI debug" description="Upload a test image and inspect the backend OpenAI debug response.">
      {mutation.data?.response ? <Alert severity="info">{mutation.data.response}</Alert> : null}
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Button component="label" variant="outlined">
              {file ? file.name : "Choose image"}
              <input hidden type="file" accept="image/*" onChange={event => setFile(event.target.files?.[0] ?? null)} />
            </Button>
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
                Run OpenAI test
              </Button>
            </Stack>
            {mutation.data ? (
              <Typography component="pre" sx={{ whiteSpace: "pre-wrap", fontFamily: "monospace", fontSize: 12 }}>
                {JSON.stringify(mutation.data, null, 2)}
              </Typography>
            ) : null}
          </Stack>
        </CardContent>
      </Card>
    </SettingsPage>
  );
}
