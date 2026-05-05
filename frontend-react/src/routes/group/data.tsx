import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardActions from "@mui/material/CardActions";
import CardContent from "@mui/material/CardContent";
import Grid from "@mui/material/Grid";
import Typography from "@mui/material/Typography";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { apiClient } from "@/lib/api/client";

export function GroupDataRouteComponent() {
  const { data: user } = useCurrentUser();
  const links = [
    ["/group/data/recipes", "Recipes", "Browse recipe inventory and deep-link into individual recipes."],
    ["/group/data/categories", "Categories", "Create, rename, and delete recipe categories."],
    ["/group/data/tags", "Tags", "Manage recipe tags used across the group."],
    ["/group/data/tools", "Tools", "Manage recipe tools and defaults."],
    ["/group/data/foods", "Foods", "Maintain ingredient foods and label assignments."],
    ["/group/data/units", "Units", "Manage ingredient units and abbreviations."],
    ["/group/data/labels", "Labels", "Maintain reusable labels for foods and shopping items."],
    ["/group/data/recipe-actions", "Recipe actions", "Configure group recipe actions and external links."],
  ] as const;

  return (
    <SettingsPage
      user={user}
      title="Group data"
      description="Open the same organizer and data-management surfaces that remain available in the legacy frontend."
    >
      <Grid container spacing={2}>
        {links.map(([href, title, description]) => (
          <Grid key={href} size={{ xs: 12, md: 6 }}>
            <Card sx={{ height: "100%" }}>
              <CardContent>
                <Typography variant="h6">{title}</Typography>
                <Typography color="text.secondary">{description}</Typography>
              </CardContent>
              <CardActions>
                <Button href={apiClient.resolvePath(href)}>Open</Button>
              </CardActions>
            </Card>
          </Grid>
        ))}
      </Grid>
    </SettingsPage>
  );
}
