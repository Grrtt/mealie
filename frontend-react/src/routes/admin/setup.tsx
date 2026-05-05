import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { useQuery } from "@tanstack/react-query";
import { fetchAdminAbout, fetchAdminChecks } from "@/features/settings/api";
import { apiClient } from "@/lib/api/client";

export function AdminSetupRouteComponent() {
  const { data: user } = useCurrentUser();
  const aboutQuery = useQuery({ queryKey: ["admin-about"], queryFn: fetchAdminAbout });
  const checksQuery = useQuery({ queryKey: ["admin-checks"], queryFn: fetchAdminChecks });

  return (
    <AppShell groupSlug={user?.groupSlug ?? "home"} userName={user?.fullName}>
      <Stack spacing={3}>
        <Typography variant="h4">Admin Setup</Typography>
        <Alert severity="info">
          The React migration keeps the first-login admin destination available and links directly into
          the core configuration, backup, and user-management tasks needed after initial deployment.
        </Alert>
        <Grid container spacing={2}>
          {[
            ["/admin/site-settings", "Site settings", "Review parser defaults, prompts, and configuration checks."],
            ["/admin/manage/users", "Manage users", "Create the first operators and assign households."],
            ["/admin/backups", "Backups", "Create a safety snapshot before broader rollout changes."],
          ].map(([href, title, description]) => (
            <Grid key={href} size={{ xs: 12, md: 4 }}>
              <Card sx={{ height: "100%" }}>
                <CardContent>
                  <Stack spacing={2}>
                    <Typography variant="h6">{title}</Typography>
                    <Typography color="text.secondary">{description}</Typography>
                    <Button href={apiClient.resolvePath(href)}>Open</Button>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
        <Card>
          <CardContent>
            <Typography variant="h6">Startup diagnostics</Typography>
            <Typography component="pre" sx={{ whiteSpace: "pre-wrap", fontFamily: "monospace", fontSize: 12 }}>
              {JSON.stringify({ about: aboutQuery.data ?? {}, checks: checksQuery.data ?? {} }, null, 2)}
            </Typography>
          </CardContent>
        </Card>
      </Stack>
    </AppShell>
  );
}
