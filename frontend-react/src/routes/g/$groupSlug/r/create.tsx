import { useEffect } from "react";
import Card from "@mui/material/Card";
import CardActionArea from "@mui/material/CardActionArea";
import CardContent from "@mui/material/CardContent";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useParams } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { apiClient } from "@/lib/api/client";

const actions = [
  { slug: "new", title: "Create from scratch", description: "Start a new editable recipe." },
  { slug: "url", title: "Import from URL", description: "Scrape a recipe from a source link." },
  { slug: "bulk", title: "Bulk import URLs", description: "Import several recipe links in one go." },
  { slug: "html", title: "Import HTML/JSON", description: "Paste raw recipe markup and save it." },
  { slug: "zip", title: "Import ZIP", description: "Import exported recipe archives." },
  { slug: "image", title: "Import from image", description: "Extract a recipe from a photo using AI vision." },
  { slug: "debug", title: "Debug scraper", description: "Preview scrape output before importing." },
];

export function RecipeCreateRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/r/create" });
  const { data: user } = useCurrentUser();

  useEffect(() => {
    document.title = "Create Recipe · Mealie";
  }, []);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Create Recipe">
      <Stack spacing={3}>
        <Typography variant="h4">Recipe creation</Typography>
        <Grid container spacing={2}>
          {actions.map(action => (
            <Grid key={action.slug} size={{ xs: 12, md: 6, lg: 4 }}>
              <Card>
                <CardActionArea href={apiClient.resolvePath(`/g/${groupSlug}/r/create/${action.slug}`)}>
                  <CardContent>
                    <Stack spacing={1}>
                      <Typography variant="h6">{action.title}</Typography>
                      <Typography color="text.secondary">{action.description}</Typography>
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
