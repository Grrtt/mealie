import { useEffect } from "react";
import Card from "@mui/material/Card";
import CardActionArea from "@mui/material/CardActionArea";
import CardContent from "@mui/material/CardContent";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useParams } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { apiClient } from "@/lib/api/client";

export function GroupLandingRouteComponent() {
  const { t } = useTranslation();
  const { groupSlug } = useParams({ from: "/g/$groupSlug" });
  const { data: user } = useCurrentUser();

  useEffect(() => {
    document.title = `Mealie · ${groupSlug}`;
  }, [groupSlug]);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName}>
      <Stack spacing={3}>
        <Typography variant="h4">{user?.fullName ?? "Mealie"}</Typography>
        <Typography color="text.secondary">{groupSlug}</Typography>
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, md: 4 }}>
            <Card>
              <CardActionArea href={apiClient.resolvePath(`/g/${groupSlug}/recipes/categories`)}>
                <CardContent>
                  <Typography variant="h6">{t("general.recipes")}</Typography>
                </CardContent>
              </CardActionArea>
            </Card>
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <Card>
              <CardActionArea href={apiClient.resolvePath("/household/mealplan/planner/view")}>
                <CardContent>
                  <Typography variant="h6">{t("meal-plan.meal-planner")}</Typography>
                </CardContent>
              </CardActionArea>
            </Card>
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <Card>
              <CardActionArea href={apiClient.resolvePath("/shopping-lists")}>
                <CardContent>
                  <Typography variant="h6">{t("shopping-list.shopping-lists")}</Typography>
                </CardContent>
              </CardActionArea>
            </Card>
          </Grid>
        </Grid>
      </Stack>
    </AppShell>
  );
}
