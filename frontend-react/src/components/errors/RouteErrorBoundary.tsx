import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useRouter } from "@tanstack/react-router";

type Props = {
  error: Error;
};

export function RouteErrorBoundary({ error }: Props) {
  const router = useRouter();
  const routeError = error as Error & { statusText?: string };

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Stack spacing={3}>
        <Typography variant="h4">Mealie</Typography>
        <Alert severity="error">
          {routeError?.statusText ?? error.message ?? "Something went wrong"}
        </Alert>
        <Box>
          <Button variant="contained" onClick={() => router.invalidate()}>
            Retry
          </Button>
        </Box>
      </Stack>
    </Container>
  );
}
