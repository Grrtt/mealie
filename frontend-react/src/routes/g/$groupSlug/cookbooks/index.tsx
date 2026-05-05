import { useEffect } from "react";
import Card from "@mui/material/Card";
import CardActionArea from "@mui/material/CardActionArea";
import CardContent from "@mui/material/CardContent";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useParams } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchCookbooks } from "@/features/recipes/api";
import { apiClient } from "@/lib/api/client";

export function CookbooksRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/cookbooks" });
  const { data: user } = useCurrentUser();
  const cookbooksQuery = useQuery({ queryKey: ["cookbooks"], queryFn: fetchCookbooks });

  useEffect(() => {
    document.title = "Cookbooks · Mealie";
  }, []);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Cookbooks">
      <Stack spacing={3}>
        <Typography variant="h4">Cookbooks</Typography>
        <Grid container spacing={2}>
          {(cookbooksQuery.data?.items ?? []).map(cookbook => (
            <Grid key={cookbook.id} size={{ xs: 12, md: 6 }}>
              <Card>
                <CardActionArea href={apiClient.resolvePath(`/g/${groupSlug}/cookbooks/${cookbook.slug ?? cookbook.id}`)}>
                  <CardContent>
                    <Stack spacing={1}>
                      <Typography variant="h6">{cookbook.name}</Typography>
                      {cookbook.description ? <Typography color="text.secondary">{cookbook.description}</Typography> : null}
                    </Stack>
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          ))}
        </Grid>
      </Stack>
    </AppShell>
  );
}
