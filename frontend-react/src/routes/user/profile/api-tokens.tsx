import { useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { currentUserQueryKey } from "@/features/auth/session";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createApiToken, deleteApiToken } from "@/features/settings/api";
import { queryClient } from "@/lib/query/queryClient";

export function UserApiTokensRouteComponent() {
  const { data: user } = useCurrentUser();
  const [name, setName] = useState("");
  const [createdToken, setCreatedToken] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: async () => {
      if (!name.trim()) throw new Error("Enter a token name.");
      return await createApiToken(name.trim());
    },
    onSuccess: async token => {
      setCreatedToken(token.token);
      setStatus("API token created.");
      setError(null);
      setName("");
      await queryClient.invalidateQueries({ queryKey: currentUserQueryKey });
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to create token.");
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (tokenId: number) => await deleteApiToken(tokenId),
    onSuccess: async () => {
      setStatus("API token deleted.");
      setError(null);
      await queryClient.invalidateQueries({ queryKey: currentUserQueryKey });
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to delete token.");
    },
  });

  return (
    <SettingsPage
      user={user}
      title="API tokens"
      description="Create and revoke long-lived API tokens for external integrations while keeping the legacy backend contract intact."
    >
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}
      {createdToken ? (
        <Alert severity="warning" onClose={() => setCreatedToken(null)}>
          Copy this token now — it will not be shown again: <strong>{createdToken}</strong>
        </Alert>
      ) : null}

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h6">Create API token</Typography>
            <TextField
              label="Token name"
              value={name}
              onChange={event => setName(event.target.value)}
            />
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => createMutation.mutate()} disabled={createMutation.isPending}>
                Generate token
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Stack spacing={2}>
        {(user?.tokens ?? []).map(token => (
          <Card key={token.id} variant="outlined">
            <CardContent>
              <Stack direction={{ xs: "column", md: "row" }} spacing={2} alignItems={{ md: "center" }}>
                <Stack spacing={0.5} sx={{ flex: 1 }}>
                  <Typography variant="h6">{token.name}</Typography>
                  <Typography color="text.secondary">
                    Created {token.createdAt ? new Date(token.createdAt).toLocaleString() : "recently"}
                  </Typography>
                </Stack>
                <Button color="error" variant="outlined" onClick={() => deleteMutation.mutate(token.id)}>
                  Delete
                </Button>
              </Stack>
            </CardContent>
          </Card>
        ))}
        {!(user?.tokens?.length ?? 0) ? (
          <Typography color="text.secondary">No API tokens have been created yet.</Typography>
        ) : null}
      </Stack>
    </SettingsPage>
  );
}
