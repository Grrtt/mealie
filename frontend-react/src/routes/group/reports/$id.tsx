import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useParams } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchGroupReport } from "@/features/settings/api";

export function GroupReportDetailRouteComponent() {
  const { id } = useParams({ from: "/group/reports/$id" });
  const { data: user } = useCurrentUser();
  const reportQuery = useQuery({
    queryKey: ["group-report", id],
    queryFn: async () => await fetchGroupReport(id),
  });

  return (
    <SettingsPage
      user={user}
      title="Report details"
      description="Inspect migration, backup, restore, and bulk import report details."
    >
      {reportQuery.data ? (
        <Stack spacing={2}>
          <Card>
            <CardContent>
              <Typography variant="h5">{reportQuery.data.name}</Typography>
              <Typography color="text.secondary">
                {reportQuery.data.category} · {reportQuery.data.status ?? "unknown"}
              </Typography>
              <Typography color="text.secondary">
                Processed {reportQuery.data.processedCount ?? 0} of {reportQuery.data.totalCount ?? 0}
              </Typography>
            </CardContent>
          </Card>
          {(reportQuery.data.entries ?? []).map(entry => (
            <Card key={entry.id} variant="outlined">
              <CardContent>
                <Typography variant="body1">{entry.message}</Typography>
                {entry.exception ? (
                  <Typography color="error.main" sx={{ whiteSpace: "pre-wrap", mt: 1 }}>
                    {entry.exception}
                  </Typography>
                ) : null}
              </CardContent>
            </Card>
          ))}
        </Stack>
      ) : null}
    </SettingsPage>
  );
}
