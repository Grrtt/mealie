import { useEffect } from "react";
import AutoStoriesRoundedIcon from "@mui/icons-material/AutoStoriesRounded";
import CategoryRoundedIcon from "@mui/icons-material/CategoryRounded";
import Inventory2RoundedIcon from "@mui/icons-material/Inventory2Rounded";
import ManageSearchRoundedIcon from "@mui/icons-material/ManageSearchRounded";
import MenuBookRoundedIcon from "@mui/icons-material/MenuBookRounded";
import ShoppingCartRoundedIcon from "@mui/icons-material/ShoppingCartRounded";
import TimelineRoundedIcon from "@mui/icons-material/TimelineRounded";
import TodayRoundedIcon from "@mui/icons-material/TodayRounded";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardActionArea from "@mui/material/CardActionArea";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { alpha } from "@mui/material/styles";
import { useParams } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { apiClient } from "@/lib/api/client";

type DashboardAction = {
  title: string;
  description: string;
  href: string;
  actionLabel: string;
  icon: React.ReactNode;
};

export function GroupLandingRouteComponent() {
  const { t } = useTranslation();
  const { groupSlug } = useParams({ from: "/g/$groupSlug" });
  const { data: user } = useCurrentUser();

  useEffect(() => {
    document.title = `Mealie · ${groupSlug}`;
  }, [groupSlug]);

  const displayName = user?.fullName ?? user?.username ?? "Mealie";
  const subtitle = [user?.group ?? groupSlug, user?.household].filter(Boolean).join(" • ");

  const primaryActions: DashboardAction[] = [
    {
      title: t("general.recipes"),
      description: "Browse your collection, revisit favorites, and jump into the recipes you use most.",
      href: apiClient.resolvePath(`/g/${groupSlug}/recipes`),
      actionLabel: "Open recipes",
      icon: <AutoStoriesRoundedIcon fontSize="large" />,
    },
    {
      title: t("meal-plan.meal-planner"),
      description: "Shape the week quickly with a clearer view of what is cooking next.",
      href: apiClient.resolvePath("/household/mealplan/planner/view"),
      actionLabel: "Plan meals",
      icon: <TodayRoundedIcon fontSize="large" />,
    },
    {
      title: t("shopping-list.shopping-lists"),
      description: "Stay on top of ingredients, prep, and list cleanup without digging through menus.",
      href: apiClient.resolvePath("/shopping-lists"),
      actionLabel: "View lists",
      icon: <ShoppingCartRoundedIcon fontSize="large" />,
    },
  ];

  const secondaryActions: DashboardAction[] = [
    {
      title: "Finder",
      description: "Narrow recipes down by ingredients, tags, and the tools you have on hand.",
      href: apiClient.resolvePath(`/g/${groupSlug}/recipes/finder`),
      actionLabel: "Search recipes",
      icon: <ManageSearchRoundedIcon />,
    },
    {
      title: "Timeline",
      description: "See recent recipe activity and get a quick pulse on what changed lately.",
      href: apiClient.resolvePath(`/g/${groupSlug}/recipes/timeline`),
      actionLabel: "Open timeline",
      icon: <TimelineRoundedIcon />,
    },
    {
      title: "Cookbooks",
      description: "Group recipes into curated collections for recurring menus and themed sets.",
      href: apiClient.resolvePath(`/g/${groupSlug}/cookbooks`),
      actionLabel: "Browse cookbooks",
      icon: <MenuBookRoundedIcon />,
    },
    {
      title: "Organizers",
      description: "Keep categories, tags, and tools tidy so recipes stay easy to browse later.",
      href: apiClient.resolvePath(`/g/${groupSlug}/recipes/categories`),
      actionLabel: "Manage organizers",
      icon: <CategoryRoundedIcon />,
    },
  ];

  const workspaceHighlights = [
    {
      label: "Signed in as",
      value: displayName,
    },
    {
      label: "Group",
      value: user?.group ?? groupSlug,
    },
    {
      label: "Household",
      value: user?.household ?? "Not set",
    },
  ];

  return (
    <AppShell
      groupSlug={groupSlug}
      subtitle={subtitle}
      title="Home"
      userName={user?.fullName}
    >
      <Stack spacing={3.5}>
        <Card
          sx={{
            overflow: "hidden",
            borderRadius: 4,
            background: theme => `linear-gradient(135deg, ${theme.palette.primary.dark} 0%, ${theme.palette.secondary.main} 58%, ${theme.palette.primary.main} 100%)`,
            color: "common.white",
            boxShadow: theme => `0 24px 48px ${alpha(theme.palette.primary.main, 0.24)}`,
          }}
        >
          <Grid container>
            <Grid size={{ xs: 12, md: 8 }}>
              <Box sx={{ px: { xs: 3, md: 4 }, py: { xs: 3.5, md: 4 } }}>
                <Stack spacing={2.5}>
                  <Box>
                    <Typography sx={{ fontSize: 12, fontWeight: 700, letterSpacing: 1.2, opacity: 0.96, textTransform: "uppercase" }}>
                      Kitchen dashboard
                    </Typography>
                    <Typography variant="h3" sx={{ mt: 1, fontSize: { xs: "2rem", md: "2.6rem" } }}>
                      Welcome back
                    </Typography>
                    <Typography variant="h5" sx={{ mt: 0.75, fontWeight: 600, opacity: 0.95 }}>
                      {displayName}
                    </Typography>
                  </Box>
                    <Typography sx={{ maxWidth: 720, opacity: 0.98 }}>
                      Keep recipes, planning, and shopping in one place with a cleaner starting point for your group and household.
                    </Typography>
                  <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                    <Chip label={`Group: ${user?.group ?? groupSlug}`} color="primary" />
                    <Chip label={`Household: ${user?.household ?? "Not set"}`} color="primary" />
                    {user?.canManage ? (
                      <Chip label="Can manage" color="primary" />
                    ) : null}
                    {user?.admin ? (
                      <Chip label="Admin access" color="primary" />
                    ) : null}
                  </Stack>
                </Stack>
              </Box>
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <Box
                sx={{
                  height: "100%",
                  px: { xs: 3, md: 3.5 },
                  py: { xs: 3, md: 4 },
                  borderLeft: { md: `1px solid ${alpha("#ffffff", 0.22)}` },
                  backgroundColor: alpha("#000000", 0.14),
                }}
              >
                <Stack spacing={2}>
                  <Typography variant="h6">Jump back in</Typography>
                  <Button
                    href={apiClient.resolvePath(`/g/${groupSlug}/r/create`)}
                    sx={{ justifyContent: "flex-start" }}
                    variant="contained"
                  >
                    Create recipe
                  </Button>
                  <Button
                    href={apiClient.resolvePath(`/g/${groupSlug}/recipes/finder`)}
                    sx={{
                      justifyContent: "flex-start",
                      bgcolor: alpha("#ffffff", 0.94),
                      color: "text.primary",
                      borderColor: alpha("#ffffff", 0.72),
                      "&:hover": {
                        bgcolor: alpha("#ffffff", 0.88),
                        borderColor: alpha("#ffffff", 0.88),
                      },
                    }}
                    variant="outlined"
                  >
                    Open finder
                  </Button>
                  <Button
                    href={apiClient.resolvePath("/shopping-lists")}
                    sx={{
                      justifyContent: "flex-start",
                      bgcolor: alpha("#ffffff", 0.94),
                      color: "text.primary",
                      borderColor: alpha("#ffffff", 0.72),
                      "&:hover": {
                        bgcolor: alpha("#ffffff", 0.88),
                        borderColor: alpha("#ffffff", 0.88),
                      },
                    }}
                    variant="outlined"
                  >
                    Review shopping lists
                  </Button>
                </Stack>
              </Box>
            </Grid>
          </Grid>
        </Card>

        <Grid container spacing={3}>
          {primaryActions.map(action => (
            <Grid key={action.title} size={{ xs: 12, md: 4 }}>
              <Card sx={{ height: "100%", borderRadius: 4 }}>
                <CardActionArea href={action.href} sx={{ height: "100%" }}>
                  <CardContent sx={{ p: 3, height: "100%" }}>
                    <Stack spacing={2.5} sx={{ height: "100%" }}>
                      <Box
                        sx={{
                          width: 56,
                          height: 56,
                          display: "grid",
                          placeItems: "center",
                          borderRadius: 3,
                          bgcolor: theme => alpha(theme.palette.primary.main, 0.12),
                          color: "primary.dark",
                        }}
                      >
                        {action.icon}
                      </Box>
                      <Box>
                        <Typography variant="h5" sx={{ fontSize: "1.2rem", fontWeight: 700 }}>
                          {action.title}
                        </Typography>
                        <Typography color="text.secondary" sx={{ mt: 1.25 }}>
                          {action.description}
                        </Typography>
                      </Box>
                      <Typography color="primary.dark" fontWeight={700} sx={{ mt: "auto" }}>
                        {action.actionLabel}
                      </Typography>
                    </Stack>
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          ))}
        </Grid>

        <Grid container spacing={3}>
          <Grid size={{ xs: 12, lg: 8 }}>
            <Card sx={{ borderRadius: 4, height: "100%" }}>
              <CardContent sx={{ p: 3 }}>
                <Stack spacing={2.5}>
                  <Box>
                    <Typography variant="h5" sx={{ fontSize: "1.25rem", fontWeight: 700 }}>
                      Explore and organize
                    </Typography>
                    <Typography color="text.secondary" sx={{ mt: 0.75 }}>
                      Keep the landing page useful by surfacing the places people usually need right after recipes, planning, and shopping.
                    </Typography>
                  </Box>
                  <Grid container spacing={2}>
                    {secondaryActions.map(action => (
                      <Grid key={action.title} size={{ xs: 12, sm: 6 }}>
                        <Card variant="outlined" sx={{ height: "100%", borderRadius: 3 }}>
                          <CardActionArea href={action.href} sx={{ height: "100%" }}>
                            <CardContent sx={{ p: 2.5, height: "100%" }}>
                              <Stack spacing={1.75} sx={{ height: "100%" }}>
                                <Box
                                  sx={{
                                    width: 44,
                                    height: 44,
                                    display: "grid",
                                    placeItems: "center",
                                    borderRadius: 2.5,
                                    bgcolor: theme => alpha(theme.palette.secondary.main, 0.1),
                                    color: "secondary.dark",
                                  }}
                                >
                                  {action.icon}
                                </Box>
                                <Box>
                                  <Typography variant="h6" sx={{ fontSize: "1rem", fontWeight: 700 }}>
                                    {action.title}
                                  </Typography>
                                  <Typography color="text.secondary" variant="body2" sx={{ mt: 0.75 }}>
                                    {action.description}
                                  </Typography>
                                </Box>
                                <Typography color="secondary.dark" fontWeight={700} variant="body2" sx={{ mt: "auto" }}>
                                  {action.actionLabel}
                                </Typography>
                              </Stack>
                            </CardContent>
                          </CardActionArea>
                        </Card>
                      </Grid>
                    ))}
                  </Grid>
                </Stack>
              </CardContent>
            </Card>
          </Grid>
          <Grid size={{ xs: 12, lg: 4 }}>
            <Card sx={{ borderRadius: 4, height: "100%" }}>
              <CardContent sx={{ p: 3, height: "100%" }}>
                <Stack spacing={2.5} sx={{ height: "100%" }}>
                  <Box>
                    <Typography variant="h5" sx={{ fontSize: "1.2rem", fontWeight: 700 }}>
                      Workspace details
                    </Typography>
                    <Typography color="text.secondary" sx={{ mt: 0.75 }}>
                      Clear labels make the shell feel intentional instead of temporary.
                    </Typography>
                  </Box>
                  <Stack spacing={1.5}>
                    {workspaceHighlights.map(item => (
                      <Box
                        key={item.label}
                        sx={{
                          px: 2,
                          py: 1.5,
                          borderRadius: 3,
                          bgcolor: theme => alpha(theme.palette.primary.main, 0.06),
                        }}
                      >
                        <Typography color="text.secondary" variant="caption" sx={{ textTransform: "uppercase", letterSpacing: 0.8 }}>
                          {item.label}
                        </Typography>
                        <Typography fontWeight={700} sx={{ mt: 0.4 }}>
                          {item.value}
                        </Typography>
                      </Box>
                    ))}
                  </Stack>
                  <Box
                    sx={{
                      p: 2,
                      borderRadius: 3,
                      bgcolor: theme => alpha(theme.palette.secondary.main, 0.08),
                    }}
                  >
                    <Stack direction="row" spacing={1.25}>
                      <Box
                        sx={{
                          width: 40,
                          height: 40,
                          display: "grid",
                          placeItems: "center",
                          borderRadius: 2.5,
                          bgcolor: theme => alpha(theme.palette.secondary.main, 0.14),
                          color: "secondary.dark",
                          flexShrink: 0,
                        }}
                      >
                        <Inventory2RoundedIcon />
                      </Box>
                      <Box>
                        <Typography fontWeight={700}>Need to adjust your workspace?</Typography>
                        <Typography color="text.secondary" variant="body2" sx={{ mt: 0.5 }}>
                          Use settings and data tools for deeper housekeeping without cluttering the home page itself.
                        </Typography>
                      </Box>
                    </Stack>
                  </Box>
                  <Stack direction={{ xs: "column", sm: "row", lg: "column" }} spacing={1.5} sx={{ mt: "auto" }}>
                    <Button href={apiClient.resolvePath("/user/profile")} variant="outlined">
                      User settings
                    </Button>
                    {user?.canManage ? (
                      <Button href={apiClient.resolvePath("/group/data")} variant="outlined">
                        Data management
                      </Button>
                    ) : null}
                    {user?.admin ? (
                      <Button href={apiClient.resolvePath("/admin/site-settings")} variant="contained">
                        Admin settings
                      </Button>
                    ) : null}
                  </Stack>
                </Stack>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      </Stack>
    </AppShell>
  );
}
