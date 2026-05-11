import { useMemo, useState } from "react";
import ErrorOutlineRoundedIcon from "@mui/icons-material/ErrorOutlineRounded";
import ExpandMoreRoundedIcon from "@mui/icons-material/ExpandMoreRounded";
import FilterAltRoundedIcon from "@mui/icons-material/FilterAltRounded";
import InboxRoundedIcon from "@mui/icons-material/InboxRounded";
import NotesRoundedIcon from "@mui/icons-material/NotesRounded";
import RefreshRoundedIcon from "@mui/icons-material/RefreshRounded";
import ScheduleRoundedIcon from "@mui/icons-material/ScheduleRounded";
import WarningAmberRoundedIcon from "@mui/icons-material/WarningAmberRounded";
import WarningRoundedIcon from "@mui/icons-material/WarningRounded";
import Accordion from "@mui/material/Accordion";
import AccordionDetails from "@mui/material/AccordionDetails";
import AccordionSummary from "@mui/material/AccordionSummary";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Grid from "@mui/material/Grid";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { alpha } from "@mui/material/styles";
import { useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import {
  fetchAdminLogs,
  type AdminLogEntryResponse,
  type AdminLogMinimumLevel,
} from "@/features/settings/api";

const limitOptions = [50, 100, 200];

const timestampFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: "medium",
  timeStyle: "medium",
});

const relativeFormatter = new Intl.RelativeTimeFormat(undefined, {
  numeric: "auto",
});

function entryColor(level: string): "warning" | "error" {
  return level === "Warning" ? "warning" : "error";
}

function formatRelativeDate(timestamp: string) {
  const timestampMs = new Date(timestamp).getTime();
  const diffMs = timestampMs - Date.now();
  const diffMinutes = Math.round(diffMs / 60_000);

  if (Math.abs(diffMinutes) < 60) {
    return relativeFormatter.format(diffMinutes, "minute");
  }

  const diffHours = Math.round(diffMs / 3_600_000);
  if (Math.abs(diffHours) < 24) {
    return relativeFormatter.format(diffHours, "hour");
  }

  const diffDays = Math.round(diffMs / 86_400_000);
  return relativeFormatter.format(diffDays, "day");
}

function entrySummary(entry: AdminLogEntryResponse) {
  return entry.message.split("\n").find(line => line.trim().length > 0) ?? entry.message;
}

function requestLabel(entry: AdminLogEntryResponse) {
  if (entry.requestMethod && entry.requestPath) {
    return `${entry.requestMethod} ${entry.requestPath}`;
  }

  return entry.requestPath;
}

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <Stack spacing={0.5}>
      <Typography color="text.secondary" variant="overline" sx={{ lineHeight: 1.2 }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ wordBreak: "break-word" }}>
        {value}
      </Typography>
    </Stack>
  );
}

export function AdminLogsRouteComponent() {
  const { data: user } = useCurrentUser();
  const [minimumLevel, setMinimumLevel] = useState<AdminLogMinimumLevel>("Warning");
  const [limit, setLimit] = useState(100);

  const logsQuery = useQuery({
    queryKey: ["admin-logs", minimumLevel, limit],
    queryFn: async () => await fetchAdminLogs({ minimumLevel, limit }),
  });

  const description = useMemo(
    () => "Review recent warning and error events captured by the running backend process.",
    [],
  );

  const summary = useMemo(() => {
    const entries = logsQuery.data?.entries ?? [];
    const latestTimestamp = entries.reduce<string | null>((latest, entry) => {
      if (!latest) {
        return entry.timestamp;
      }

      return new Date(entry.timestamp).getTime() > new Date(latest).getTime() ? entry.timestamp : latest;
    }, null);

    return {
      shownCount: entries.length,
      totalCount: logsQuery.data?.totalCount ?? 0,
      warningCount: entries.filter(entry => entry.level === "Warning").length,
      errorCount: entries.filter(entry => entry.level === "Error").length,
      latestTimestamp,
    };
  }, [logsQuery.data]);

  const statCards = [
    {
      label: "Captured",
      value: String(summary.totalCount),
      helper: "Entries currently retained in memory.",
      icon: <NotesRoundedIcon color="primary" sx={{ fontSize: 28 }} />,
    },
    {
      label: "Visible",
      value: String(summary.shownCount),
      helper: "Rows matching the active filters.",
      icon: <FilterAltRoundedIcon color="primary" sx={{ fontSize: 28 }} />,
    },
    {
      label: "Warnings",
      value: String(summary.warningCount),
      helper: "Warning-level events in the current view.",
      icon: <WarningAmberRoundedIcon color="warning" sx={{ fontSize: 28 }} />,
    },
    {
      label: "Errors",
      value: String(summary.errorCount),
      helper: "Error-level events in the current view.",
      icon: <ErrorOutlineRoundedIcon color="error" sx={{ fontSize: 28 }} />,
    },
    {
      label: "Latest event",
      value: summary.latestTimestamp ? formatRelativeDate(summary.latestTimestamp) : "No events",
      helper: summary.latestTimestamp
        ? timestampFormatter.format(new Date(summary.latestTimestamp))
        : "Waiting for the next warning or error.",
      icon: <ScheduleRoundedIcon color="primary" sx={{ fontSize: 28 }} />,
    },
  ];

  return (
    <SettingsPage
      user={user}
      title="Logs"
      description={description}
      actions={(
        <Button
          variant="outlined"
          startIcon={<RefreshRoundedIcon />}
          onClick={() => logsQuery.refetch()}
          disabled={logsQuery.isFetching}
        >
          {logsQuery.isFetching ? "Refreshing" : "Refresh"}
        </Button>
      )}
    >
      <Alert icon={<WarningRoundedIcon fontSize="inherit" />} severity="info">
        Logs are sourced from the active backend process and reset when that process restarts.
      </Alert>

      <Card variant="outlined">
        <CardContent>
          <Stack spacing={2.5}>
            <Stack
              direction={{ xs: "column", lg: "row" }}
              spacing={2}
              justifyContent="space-between"
              alignItems={{ lg: "flex-start" }}
            >
              <Stack direction="row" spacing={1.5} alignItems="flex-start">
                <FilterAltRoundedIcon color="primary" sx={{ mt: 0.25, fontSize: 28 }} />
                <Stack spacing={0.5}>
                  <Typography variant="h6">Inspect server events</Typography>
                  <Typography color="text.secondary" variant="body2">
                    Narrow the log stream by severity and entry count to focus on the most relevant runtime issues.
                  </Typography>
                </Stack>
              </Stack>
              {logsQuery.data ? (
                <Chip
                  label={`${summary.shownCount} visible of ${summary.totalCount}`}
                  variant="outlined"
                />
              ) : null}
            </Stack>

            <Stack direction={{ xs: "column", lg: "row" }} spacing={2} alignItems={{ lg: "flex-end" }}>
              <TextField
                select
                size="small"
                label="Severity"
                value={minimumLevel}
                onChange={event => setMinimumLevel(event.target.value as AdminLogMinimumLevel)}
                sx={{ minWidth: { xs: "100%", sm: 220 } }}
              >
                <MenuItem value="Warning">Warnings and errors</MenuItem>
                <MenuItem value="Error">Errors only</MenuItem>
              </TextField>
              <TextField
                select
                size="small"
                label="Entries"
                value={String(limit)}
                onChange={event => setLimit(Number(event.target.value))}
                sx={{ minWidth: { xs: "100%", sm: 140 } }}
              >
                {limitOptions.map(option => (
                  <MenuItem key={option} value={String(option)}>
                    {option}
                  </MenuItem>
                ))}
              </TextField>
              <Typography color="text.secondary" variant="body2" sx={{ minWidth: 0 }}>
                {logsQuery.data
                  ? `Showing ${summary.shownCount} matching entries from the ${summary.totalCount} most recent captured events.`
                  : "Adjust filters to inspect recent server events."}
              </Typography>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      {logsQuery.error ? (
        <Alert severity="error">
          {logsQuery.error instanceof Error ? logsQuery.error.message : "Unable to load admin logs."}
        </Alert>
      ) : null}

      {logsQuery.isLoading ? (
        <Box sx={{ display: "grid", placeItems: "center", minHeight: 240 }}>
          <CircularProgress />
        </Box>
      ) : null}

      {logsQuery.data && !logsQuery.isLoading ? (
        <>
          <Grid container spacing={2}>
            {statCards.map(card => (
              <Grid key={card.label} size={{ xs: 12, sm: 6, xl: 3 }}>
                <Card variant="outlined" sx={{ height: "100%" }}>
                  <CardContent>
                    <Stack spacing={1.5}>
                      {card.icon}
                      <Stack spacing={0.5}>
                        <Typography color="text.secondary" variant="body2">
                          {card.label}
                        </Typography>
                        <Typography variant="h4">{card.value}</Typography>
                        <Typography color="text.secondary" variant="body2">
                          {card.helper}
                        </Typography>
                      </Stack>
                    </Stack>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>

          {logsQuery.data.entries.length ? (
            <Stack spacing={2.5}>
              {logsQuery.data.entries.map((entry, index) => {
                const request = requestLabel(entry);
                const metaChips = [
                  entry.sourceContext,
                  request,
                  entry.statusCode != null ? `Status ${entry.statusCode}` : null,
                  entry.correlationId ? `Correlation ${entry.correlationId}` : null,
                ].filter(Boolean) as string[];

                return (
                  <Accordion
                    key={`${entry.timestamp}-${entry.level}-${index}`}
                    defaultExpanded={index === 0}
                    disableGutters
                    sx={theme => ({
                      border: `1px solid ${theme.palette.divider}`,
                      borderLeftWidth: 4,
                      borderLeftColor: entry.level === "Error" ? theme.palette.error.main : theme.palette.warning.main,
                      borderRadius: 3,
                      overflow: "hidden",
                      backgroundColor: alpha(
                        entry.level === "Error" ? theme.palette.error.main : theme.palette.warning.main,
                        0.04,
                      ),
                      boxShadow: "none",
                      "&::before": {
                        display: "none",
                      },
                      "&.Mui-expanded": {
                        margin: 0,
                      },
                    })}
                  >
                    <AccordionSummary
                      expandIcon={<ExpandMoreRoundedIcon />}
                      sx={{
                        px: { xs: 2, md: 3 },
                        py: 2,
                        "& .MuiAccordionSummary-content": {
                          my: 0,
                        },
                      }}
                    >
                      <Stack spacing={1.5} sx={{ width: "100%", minWidth: 0 }}>
                        <Stack
                          direction={{ xs: "column", md: "row" }}
                          spacing={1.5}
                          alignItems={{ md: "center" }}
                          justifyContent="space-between"
                        >
                          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                            <Chip color={entryColor(entry.level)} label={entry.level} size="small" />
                            <Typography component="time" color="text.secondary" dateTime={entry.timestamp} variant="body2">
                              {formatRelativeDate(entry.timestamp)}
                            </Typography>
                          </Stack>
                          {entry.statusCode != null ? (
                            <Chip label={`HTTP ${entry.statusCode}`} size="small" variant="outlined" />
                          ) : null}
                        </Stack>

                        <Typography
                          sx={{
                            fontWeight: 600,
                            minWidth: 0,
                            overflow: "hidden",
                            display: "-webkit-box",
                            WebkitLineClamp: 2,
                            WebkitBoxOrient: "vertical",
                          }}
                          variant="subtitle1"
                        >
                          {entrySummary(entry)}
                        </Typography>

                        {metaChips.length ? (
                          <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                            {metaChips.map(item => (
                              <Chip key={item} label={item} size="small" variant="outlined" />
                            ))}
                          </Stack>
                        ) : null}
                      </Stack>
                    </AccordionSummary>

                    <AccordionDetails sx={{ px: { xs: 2, md: 3 }, pb: { xs: 2.5, md: 3 }, pt: 0 }}>
                      <Grid container spacing={2}>
                        <Grid size={{ xs: 12, lg: 7 }}>
                          <Stack spacing={2}>
                            <Box
                              sx={theme => ({
                                p: 2,
                                borderRadius: 2.5,
                                border: `1px solid ${theme.palette.divider}`,
                                backgroundColor: theme.palette.background.paper,
                              })}
                            >
                              <Typography color="text.secondary" variant="overline">
                                Message
                              </Typography>
                              <Typography sx={{ mt: 1, whiteSpace: "pre-wrap", wordBreak: "break-word" }}>
                                {entry.message}
                              </Typography>
                            </Box>

                            {entry.exception ? (
                              <Box
                                sx={theme => ({
                                  p: 2,
                                  borderRadius: 2.5,
                                  border: `1px solid ${alpha(theme.palette.error.main, 0.28)}`,
                                  backgroundColor: alpha(theme.palette.error.main, 0.06),
                                })}
                              >
                                <Typography color="error.main" variant="overline">
                                  Exception
                                </Typography>
                                <Box
                                  component="pre"
                                  sx={{
                                    m: 0,
                                    mt: 1,
                                    fontFamily: "monospace",
                                    fontSize: 12,
                                    overflowX: "auto",
                                    whiteSpace: "pre-wrap",
                                    wordBreak: "break-word",
                                  }}
                                >
                                  {entry.exception}
                                </Box>
                              </Box>
                            ) : null}
                          </Stack>
                        </Grid>

                        <Grid size={{ xs: 12, lg: 5 }}>
                          <Box
                            sx={theme => ({
                              p: 2,
                              borderRadius: 2.5,
                              border: `1px solid ${theme.palette.divider}`,
                              backgroundColor: theme.palette.background.paper,
                              height: "100%",
                            })}
                          >
                            <Stack spacing={2}>
                              <Typography variant="subtitle2">Event details</Typography>
                              <DetailItem
                                label="Occurred"
                                value={timestampFormatter.format(new Date(entry.timestamp))}
                              />
                              <DetailItem
                                label="Relative time"
                                value={formatRelativeDate(entry.timestamp)}
                              />
                              {entry.sourceContext ? (
                                <DetailItem label="Source" value={entry.sourceContext} />
                              ) : null}
                              {request ? (
                                <DetailItem label="Request" value={request} />
                              ) : null}
                              {entry.statusCode != null ? (
                                <DetailItem label="Status code" value={String(entry.statusCode)} />
                              ) : null}
                              {entry.correlationId ? (
                                <DetailItem label="Correlation ID" value={entry.correlationId} />
                              ) : null}
                            </Stack>
                          </Box>
                        </Grid>
                      </Grid>
                    </AccordionDetails>
                  </Accordion>
                );
              })}
            </Stack>
          ) : (
            <Card variant="outlined">
              <CardContent>
                <Stack spacing={1.5} alignItems="center" textAlign="center" sx={{ py: 4 }}>
                  <InboxRoundedIcon color="disabled" sx={{ fontSize: 48 }} />
                  <Typography variant="h6">No matching logs</Typography>
                  <Typography color="text.secondary" sx={{ maxWidth: 520 }}>
                    The server has not emitted any entries for the selected severity yet. Try widening the filters or
                    refreshing after reproducing the issue you want to inspect.
                  </Typography>
                </Stack>
              </CardContent>
            </Card>
          )}
        </>
      ) : null}
    </SettingsPage>
  );
}
