import { useEffect, useMemo } from "react";
import AccessTimeRoundedIcon from "@mui/icons-material/AccessTimeRounded";
import FiberManualRecordRoundedIcon from "@mui/icons-material/FiberManualRecordRounded";
import HistoryRoundedIcon from "@mui/icons-material/HistoryRounded";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Card from "@mui/material/Card";
import CardActionArea from "@mui/material/CardActionArea";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { alpha } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { AppShell } from "@/components/layout/AppShell";
import { RecipeImage } from "@/components/recipes/RecipeImage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { formatRecipeDuration } from "@/features/recipes/format-duration";
import { fetchRecipeTimelineEvents, recipeImageUrl } from "@/features/recipes/api";
import { apiClient } from "@/lib/api/client";
import { useParams } from "@tanstack/react-router";
import type { RecipeTimelineEventOut } from "@/lib/api/contracts";

type TimelineRecipeEvent = RecipeTimelineEventOut & {
  recipeName?: string | null;
  recipeSlug?: string | null;
  recipeImage?: string | null;
  recipeDescription?: string | null;
  recipeTotalTime?: string | null;
  recipeRating?: number | null;
};

function formatTimelineDate(value?: string) {
  if (!value) {
    return "No timestamp";
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

export function RecipeTimelineRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/recipes/timeline" });
  const { data: user } = useCurrentUser();

  useEffect(() => {
    document.title = "Recipe Timeline · Mealie";
  }, []);

  const timelineQuery = useQuery({
    queryKey: ["recipe-timeline", groupSlug],
    queryFn: async () => await fetchRecipeTimelineEvents(1, 24),
  });

  const timelineItems = useMemo(() => {
    return ((timelineQuery.data?.items ?? []) as TimelineRecipeEvent[]).map(event => ({
      event,
      timestamp: event.timestamp ?? event.createdAt,
    }));
  }, [timelineQuery.data?.items]);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="Recipe Timeline">
      <Stack spacing={3}>
        <Stack spacing={1}>
          <Typography variant="h4">Recipe timeline</Typography>
          <Typography color="text.secondary">
            Follow recent recipe activity in chronological order with quick access to each recipe card.
          </Typography>
        </Stack>

        {timelineQuery.isError ? (
          <Alert severity="error">Unable to load the recipe timeline right now.</Alert>
        ) : null}

        {timelineQuery.isLoading ? (
          <Stack alignItems="center" justifyContent="center" py={8}>
            <CircularProgress />
          </Stack>
        ) : null}

        {!timelineQuery.isLoading && !timelineItems.length ? (
          <Card
            variant="outlined"
            sx={(theme) => ({
              borderStyle: "dashed",
              borderColor: alpha(theme.palette.primary.main, 0.2),
              bgcolor: alpha(theme.palette.primary.main, 0.04),
            })}
          >
            <CardContent>
              <Stack alignItems="center" py={4} spacing={1.5} textAlign="center">
                <HistoryRoundedIcon color="primary" />
                <Typography variant="h6">No recipe activity yet</Typography>
                <Typography color="text.secondary" maxWidth={420}>
                  Create or update a recipe to start building the household timeline.
                </Typography>
              </Stack>
            </CardContent>
          </Card>
        ) : null}

        {timelineItems.length ? (
          <Stack spacing={3}>
            {timelineItems.map(({ event, timestamp }) => {
              const title = event.recipeName ?? event.subject ?? "Untitled recipe";
              const imageSrc = recipeImageUrl(event.recipeId, event.recipeImage ?? null, "min-original.webp");
              const recipeHref = event.recipeSlug
                ? apiClient.resolvePath(`/g/${groupSlug}/r/${event.recipeSlug}`)
                : null;
              const supportingText = event.eventMessage ?? event.eventType ?? "Recipe activity";
              const secondaryText = event.recipeDescription ?? supportingText;

              return (
                <Stack
                  key={event.id}
                  direction={{ xs: "column", md: "row" }}
                  spacing={{ xs: 1.5, md: 2.5 }}
                  alignItems="stretch"
                >
                  <Stack
                    alignItems="center"
                    spacing={1}
                    sx={{
                      display: { xs: "none", md: "flex" },
                      width: 72,
                      flexShrink: 0,
                      position: "relative",
                    }}
                  >
                    <Box
                      sx={(theme) => ({
                        position: "absolute",
                        top: -24,
                        bottom: -24,
                        left: "50%",
                        transform: "translateX(-50%)",
                        width: 2,
                        bgcolor: alpha(theme.palette.primary.main, 0.18),
                      })}
                    />
                    <Box
                      sx={(theme) => ({
                        zIndex: 1,
                        width: 18,
                        height: 18,
                        borderRadius: "50%",
                        display: "grid",
                        placeItems: "center",
                        color: theme.palette.primary.contrastText,
                        bgcolor: theme.palette.primary.main,
                        boxShadow: `0 0 0 6px ${alpha(theme.palette.background.default, 0.96)}`,
                      })}
                    >
                      <FiberManualRecordRoundedIcon sx={{ fontSize: 10 }} />
                    </Box>
                    <Typography
                      variant="caption"
                      color="text.secondary"
                      sx={{ textAlign: "center", position: "relative", zIndex: 1 }}
                    >
                      {formatTimelineDate(timestamp)}
                    </Typography>
                  </Stack>

                  <Card
                    variant="outlined"
                    sx={(theme) => ({
                      overflow: "hidden",
                      flex: 1,
                      borderColor: alpha(theme.palette.primary.main, 0.12),
                      bgcolor: alpha(theme.palette.background.paper, 0.96),
                      boxShadow: `0 10px 24px ${alpha(theme.palette.common.black, 0.08)}`,
                    })}
                  >
                    <CardActionArea
                      component={recipeHref ? "a" : "div"}
                      href={recipeHref ?? undefined}
                      sx={{ height: "100%" }}
                    >
                      <Stack direction={{ xs: "column", sm: "row" }} sx={{ minHeight: { sm: 180 } }}>
                        <Box
                          sx={{
                            width: { xs: "100%", sm: 220 },
                            minHeight: { xs: 180, sm: "100%" },
                            flexShrink: 0,
                          }}
                        >
                          <RecipeImage
                            alt={`${title} recipe image`}
                            src={imageSrc}
                            wrapperSx={{ height: "100%" }}
                          />
                        </Box>

                        <CardContent sx={{ flex: 1, p: 2.5 }}>
                          <Stack spacing={1.5} sx={{ height: "100%" }}>
                            <Stack
                              direction={{ xs: "column", sm: "row" }}
                              spacing={1}
                              justifyContent="space-between"
                              alignItems={{ xs: "flex-start", sm: "center" }}
                            >
                              <Stack spacing={0.5}>
                                <Typography variant="h5">{title}</Typography>
                                <Typography color="text.secondary">{supportingText}</Typography>
                              </Stack>
                              <Chip
                                icon={<AccessTimeRoundedIcon />}
                                label={formatTimelineDate(timestamp)}
                                size="small"
                                variant="outlined"
                              />
                            </Stack>

                            <Typography
                              color="text.secondary"
                              sx={{
                                display: "-webkit-box",
                                overflow: "hidden",
                                WebkitLineClamp: 2,
                                WebkitBoxOrient: "vertical",
                              }}
                            >
                              {secondaryText}
                            </Typography>

                            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap sx={{ mt: "auto" }}>
                              {event.recipeTotalTime ? (
                                <Chip size="small" label={formatRecipeDuration(event.recipeTotalTime)} />
                              ) : null}
                              {event.recipeRating ? <Chip size="small" label={`★ ${event.recipeRating}`} /> : null}
                              {recipeHref ? <Chip size="small" color="primary" label="Open recipe" /> : null}
                            </Stack>
                          </Stack>
                        </CardContent>
                      </Stack>
                    </CardActionArea>
                  </Card>
                </Stack>
              );
            })}
          </Stack>
        ) : null}
      </Stack>
    </AppShell>
  );
}
