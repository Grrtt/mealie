import { useEffect, useMemo, useState } from "react";
import AdminPanelSettingsRounded from "@mui/icons-material/AdminPanelSettingsRounded";
import AutoAwesomeRounded from "@mui/icons-material/AutoAwesomeRounded";
import CheckCircleRounded from "@mui/icons-material/CheckCircleRounded";
import LockRounded from "@mui/icons-material/LockRounded";
import PublicRounded from "@mui/icons-material/PublicRounded";
import SettingsSuggestRounded from "@mui/icons-material/SettingsSuggestRounded";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Checkbox from "@mui/material/Checkbox";
import Chip from "@mui/material/Chip";
import CircularProgress from "@mui/material/CircularProgress";
import Divider from "@mui/material/Divider";
import FormControlLabel from "@mui/material/FormControlLabel";
import Grid from "@mui/material/Grid";
import LinearProgress from "@mui/material/LinearProgress";
import List from "@mui/material/List";
import ListItem from "@mui/material/ListItem";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Stack from "@mui/material/Stack";
import Step from "@mui/material/Step";
import StepLabel from "@mui/material/StepLabel";
import Stepper from "@mui/material/Stepper";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useNavigate } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { currentUserQueryKey, getStartupInfo } from "@/features/auth/session";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import {
  changeOwnPassword,
  fetchGroupPreferences,
  fetchHouseholdPreferences,
  seedFoods,
  seedLabels,
  seedUnits,
  updateCurrentUser,
  updateGroupPreferences,
  updateHouseholdPreferences,
} from "@/features/settings/api";
import { fallbackLocale } from "@/lib/i18n/persistedLocale";
import { queryClient } from "@/lib/query/queryClient";

const DEFAULT_EMAIL = "changeme@example.com";
const DEFAULT_PASSWORD = "MyPassword";
const steps = ["Start", "Account Details", "Site Settings", "Summary", "Complete"] as const;
const setupChecklist = [
  "Replace the temporary admin account details.",
  "Choose a new password for the real admin account.",
  "Decide whether recipes should start public or private.",
  "Optionally seed foods, units, and labels for a faster start.",
] as const;

export function AdminSetupRouteComponent() {
  const navigate = useNavigate();
  const { data: user } = useCurrentUser();
  const startupInfoQuery = useQuery({ queryKey: ["startup-info"], queryFn: getStartupInfo });
  const groupPreferencesQuery = useQuery({ queryKey: ["group-preferences"], queryFn: fetchGroupPreferences });
  const householdPreferencesQuery = useQuery({ queryKey: ["household-preferences"], queryFn: fetchHouseholdPreferences });
  const [activeStep, setActiveStep] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitStatus, setSubmitStatus] = useState<string | null>(null);
  const [accountForm, setAccountForm] = useState({
    email: "",
    username: "",
    fullName: "",
    advanced: true,
    password: "",
    confirmPassword: "",
  });
  const [settingsForm, setSettingsForm] = useState({
    makeGroupRecipesPublic: false,
    useSeedData: true,
  });

  useEffect(() => {
    if (!user) {
      return;
    }

    setAccountForm(current => ({
      ...current,
      email: current.email || user.email || DEFAULT_EMAIL,
      username: current.username || user.username || "changeme",
      fullName: current.fullName || user.fullName || "Change Me",
      advanced: current.advanced ?? Boolean(user.advanced),
    }));
  }, [user]);

  useEffect(() => {
    if (!householdPreferencesQuery.data) {
      return;
    }

    setSettingsForm(current => ({
      ...current,
      makeGroupRecipesPublic: Boolean(householdPreferencesQuery.data.recipePublic),
    }));
  }, [householdPreferencesQuery.data]);

  const summaryItems = useMemo(() => [
    ["Email", accountForm.email],
    ["Username", accountForm.username],
    ["Full name", accountForm.fullName],
    ["Advanced mode", accountForm.advanced ? "Enabled" : "Disabled"],
    ["Public recipes", settingsForm.makeGroupRecipesPublic ? "Enabled" : "Disabled"],
    ["Seed foods, units, and labels", settingsForm.useSeedData ? "Enabled" : "Disabled"],
  ], [accountForm, settingsForm]);
  const progressValue = ((activeStep + 1) / steps.length) * 100;
  const selectionChips = useMemo(() => [
    accountForm.advanced ? "Advanced mode enabled" : "Advanced mode disabled",
    settingsForm.makeGroupRecipesPublic ? "Recipes start public" : "Recipes start private",
    settingsForm.useSeedData ? "Seed starter data" : "Skip seed data",
  ], [accountForm.advanced, settingsForm.makeGroupRecipesPublic, settingsForm.useSeedData]);

  function validateAccountStep() {
    if (!accountForm.email || !accountForm.username || !accountForm.fullName) {
      return "Enter your email, username, and full name.";
    }

    if (accountForm.password.length === 0 || accountForm.confirmPassword.length === 0) {
      return "Choose a new password and confirm it.";
    }

    if (accountForm.password !== accountForm.confirmPassword) {
      return "New passwords must match.";
    }

    if (accountForm.email.trim().toLowerCase() === DEFAULT_EMAIL) {
      return "Change the default email to finish first-time setup.";
    }

    if (accountForm.password === DEFAULT_PASSWORD) {
      return "Choose a password other than the default first-time login password.";
    }

    return null;
  }

  async function submitSetup() {
    if (!user) {
      throw new Error("Missing current user.");
    }

    await updateGroupPreferences({
      privateGroup: !settingsForm.makeGroupRecipesPublic,
    });

    await updateHouseholdPreferences({
      privateHousehold: !settingsForm.makeGroupRecipesPublic,
      recipePublic: settingsForm.makeGroupRecipesPublic,
    });

    if (settingsForm.useSeedData) {
      const payload = { locale: fallbackLocale };
      await seedLabels(payload);
      await Promise.all([seedFoods(payload), seedUnits(payload)]);
    }

    await changeOwnPassword({
      currentPassword: DEFAULT_PASSWORD,
      newPassword: accountForm.password,
    });

    await updateCurrentUser(user.id, {
      email: accountForm.email.trim(),
      username: accountForm.username.trim(),
      fullName: accountForm.fullName.trim(),
      advanced: accountForm.advanced,
    });

    await queryClient.invalidateQueries({ queryKey: currentUserQueryKey });
    await queryClient.invalidateQueries({ queryKey: ["startup-info"] });
    await startupInfoQuery.refetch();
  }

  async function handleNext() {
    if (submitting) {
      return;
    }

    setSubmitError(null);

    if (activeStep === 1) {
      const validationError = validateAccountStep();
      if (validationError) {
        setSubmitError(validationError);
        return;
      }
    }

    if (activeStep === 3) {
      setSubmitting(true);
      try {
        await submitSetup();
        setSubmitStatus("Setup complete. Your account and starter data are ready.");
        setActiveStep(4);
      }
      catch (error) {
        setSubmitError(error instanceof Error ? error.message : "Unable to complete first-time setup.");
      }
      finally {
        setSubmitting(false);
      }
      return;
    }

    if (activeStep === 4) {
      await navigate({ href: user?.groupSlug ? `/g/${user.groupSlug}` : "/g/home" });
      return;
    }

    setActiveStep(current => Math.min(current + 1, steps.length - 1));
  }

  function handleBack() {
    if (submitting) {
      return;
    }

    setSubmitError(null);
    setActiveStep(current => Math.max(current - 1, 0));
  }

  if (!user || startupInfoQuery.isLoading || groupPreferencesQuery.isLoading || householdPreferencesQuery.isLoading) {
    return (
      <Box sx={{ display: "grid", placeItems: "center", minHeight: "100vh" }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!startupInfoQuery.data?.isFirstLogin && activeStep < 4) {
    return (
      <Box
        sx={{
          minHeight: "100vh",
          p: { xs: 2, md: 3 },
          background: theme => `linear-gradient(180deg, ${theme.palette.primary.main}12 0%, ${theme.palette.background.default} 22%)`,
        }}
      >
        <Card sx={{ width: "100%", maxWidth: 980, mx: "auto", overflow: "hidden", borderRadius: 4 }}>
          <Box
            sx={{
              px: { xs: 3, md: 4 },
              py: { xs: 3, md: 4 },
              color: "common.white",
              background: theme => `linear-gradient(135deg, ${theme.palette.primary.dark} 0%, ${theme.palette.secondary.main} 58%, ${theme.palette.primary.main} 100%)`,
            }}
          >
            <Stack spacing={1.5}>
              <Typography variant="overline" sx={{ opacity: 0.9, letterSpacing: 1.2 }}>
                Mealie onboarding
              </Typography>
              <Typography variant="h3" sx={{ fontSize: { xs: "2rem", md: "2.4rem" } }}>
                Setup already completed
              </Typography>
              <Typography sx={{ maxWidth: 720, opacity: 0.92 }}>
                This instance has already moved past first-login mode, so the onboarding wizard is locked and the regular admin workspace takes over.
              </Typography>
            </Stack>
          </Box>
          <Box sx={{ p: { xs: 2, md: 3 } }}>
            <Grid container spacing={3}>
              <Grid size={{ xs: 12, md: 8 }}>
                <Card variant="outlined" sx={{ borderRadius: 3 }}>
                  <CardContent>
                    <Stack spacing={3}>
                      <Alert severity="info">
                        The first-time setup flow is only available while the default seeded admin account is still active.
                      </Alert>
                      <List disablePadding>
                        {[
                          "The temporary setup account has already been replaced.",
                          "Mealie is now using the normal admin and group settings flows.",
                          "You can still adjust site defaults, backups, and user management from the admin area.",
                        ].map(item => (
                          <ListItem key={item} disableGutters>
                            <ListItemIcon sx={{ minWidth: 36 }}>
                              <CheckCircleRounded color="success" fontSize="small" />
                            </ListItemIcon>
                            <ListItemText primary={item} />
                          </ListItem>
                        ))}
                      </List>
                      <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} justifyContent="flex-end">
                        <Button variant="text" onClick={() => void navigate({ href: user.groupSlug ? `/g/${user.groupSlug}` : "/g/home" })}>
                          Go to home
                        </Button>
                        <Button variant="contained" onClick={() => void navigate({ href: "/admin/site-settings" })}>
                          Open admin settings
                        </Button>
                      </Stack>
                    </Stack>
                  </CardContent>
                </Card>
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <Card variant="outlined" sx={{ borderRadius: 3, height: "100%" }}>
                  <CardContent>
                    <Stack spacing={2}>
                      <Typography variant="h6">Available now</Typography>
                      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                        <Chip label="Site settings" color="primary" variant="outlined" />
                        <Chip label="Backups" color="primary" variant="outlined" />
                        <Chip label="Users" color="primary" variant="outlined" />
                        <Chip label="Groups & households" color="primary" variant="outlined" />
                      </Stack>
                      <Typography color="text.secondary">
                        Use the normal admin pages for any follow-up configuration now that onboarding is done.
                      </Typography>
                    </Stack>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Box>
        </Card>
      </Box>
    );
  }

  return (
    <Box
      sx={{
        minHeight: "100vh",
        p: { xs: 2, md: 3 },
        background: theme => `linear-gradient(180deg, ${theme.palette.primary.main}12 0%, ${theme.palette.background.default} 22%)`,
      }}
    >
      <Card
        sx={{
          width: "100%",
          maxWidth: 1180,
          mx: "auto",
          overflow: "hidden",
          borderRadius: 4,
          boxShadow: theme => theme.shadows[8],
        }}
      >
        <Box
          sx={{
            px: { xs: 3, md: 4 },
            py: { xs: 3, md: 4 },
            color: "common.white",
            background: theme => `linear-gradient(135deg, ${theme.palette.primary.dark} 0%, ${theme.palette.secondary.main} 58%, ${theme.palette.primary.main} 100%)`,
          }}
        >
          <Stack spacing={2}>
            <Stack
              direction={{ xs: "column", md: "row" }}
              spacing={2}
              justifyContent="space-between"
              alignItems={{ md: "flex-start" }}
            >
              <Stack spacing={1}>
                <Typography variant="overline" sx={{ opacity: 0.9, letterSpacing: 1.2 }}>
                  Mealie onboarding
                </Typography>
                <Typography variant="h3" sx={{ fontSize: { xs: "2rem", md: "2.6rem" } }}>
                  First Time Setup
                </Typography>
                <Typography sx={{ maxWidth: 720, opacity: 0.92 }}>
                  Turn the seeded admin account into your real account, choose sensible defaults, and optionally import starter data so the instance is ready to use immediately.
                </Typography>
              </Stack>
              <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                <Chip
                  icon={<AdminPanelSettingsRounded />}
                  label={`Signed in as ${user.fullName || "Change Me"}`}
                  sx={{
                    bgcolor: "rgba(255,255,255,0.94)",
                    color: "text.primary",
                    border: "1px solid",
                    borderColor: "rgba(255,255,255,0.7)",
                    "& .MuiChip-icon": { color: "primary.dark" },
                  }}
                />
                <Chip
                  icon={<AutoAwesomeRounded />}
                  label="First-login mode active"
                  sx={{
                    bgcolor: "rgba(255,255,255,0.94)",
                    color: "text.primary",
                    border: "1px solid",
                    borderColor: "rgba(255,255,255,0.7)",
                    "& .MuiChip-icon": { color: "secondary.dark" },
                  }}
                />
              </Stack>
            </Stack>
            <Box>
              <Stack direction="row" justifyContent="space-between" sx={{ mb: 1 }}>
                <Typography variant="body2" sx={{ opacity: 0.92 }}>
                  Step {activeStep + 1} of {steps.length}
                </Typography>
                <Typography variant="body2" sx={{ opacity: 0.92 }}>
                  {Math.round(progressValue)}%
                </Typography>
              </Stack>
              <LinearProgress
                variant="determinate"
                value={progressValue}
                sx={{
                  height: 10,
                  borderRadius: 999,
                  bgcolor: "rgba(0,0,0,0.18)",
                  "& .MuiLinearProgress-bar": { borderRadius: 999, bgcolor: "common.white" },
                }}
              />
            </Box>
          </Stack>
        </Box>

        <Box sx={{ p: { xs: 2, md: 3 } }}>
          <Grid container spacing={3}>
            <Grid size={{ xs: 12, md: 8 }}>
              <Card variant="outlined" sx={{ borderRadius: 3 }}>
                <CardContent sx={{ p: { xs: 2.5, md: 3 } }}>
                  <Stack spacing={3}>
                    <Stepper activeStep={activeStep} alternativeLabel sx={{ px: { md: 2 } }}>
                      {steps.map(label => (
                        <Step key={label}>
                          <StepLabel>{label}</StepLabel>
                        </Step>
                      ))}
                    </Stepper>

                    {submitStatus ? <Alert severity="success">{submitStatus}</Alert> : null}
                    {submitError ? <Alert severity="error">{submitError}</Alert> : null}

                    {activeStep === 0 ? (
                      <Stack spacing={3}>
                        <Stack spacing={1}>
                          <Typography variant="h5">Welcome to Mealie</Typography>
                          <Typography color="text.secondary">
                            This quick setup makes the instance feel production-ready before anyone else uses it.
                          </Typography>
                        </Stack>
                        <Alert severity="info">
                          You are signed in as the temporary admin account. Completing this flow will disable first-login mode and keep the real React onboarding path as the only setup experience.
                        </Alert>
                        <Grid container spacing={2}>
                          {[
                            {
                              icon: <LockRounded color="primary" />,
                              title: "Secure the admin account",
                              description: "Replace the default email and password with real credentials.",
                            },
                            {
                              icon: <SettingsSuggestRounded color="primary" />,
                              title: "Pick starting defaults",
                              description: "Choose whether recipes begin public and whether to keep advanced mode enabled.",
                            },
                            {
                              icon: <AutoAwesomeRounded color="primary" />,
                              title: "Seed starter data",
                              description: "Import foods, units, and labels so recipe organization works out of the box.",
                            },
                          ].map(item => (
                            <Grid key={item.title} size={{ xs: 12, md: 4 }}>
                              <Card variant="outlined" sx={{ height: "100%", borderRadius: 3 }}>
                                <CardContent>
                                  <Stack spacing={1.5}>
                                    <Box>{item.icon}</Box>
                                    <Typography variant="h6">{item.title}</Typography>
                                    <Typography color="text.secondary">{item.description}</Typography>
                                  </Stack>
                                </CardContent>
                              </Card>
                            </Grid>
                          ))}
                        </Grid>
                      </Stack>
                    ) : null}

                    {activeStep === 1 ? (
                      <Stack spacing={3}>
                        <Stack spacing={1}>
                          <Typography variant="h5">Account Details</Typography>
                          <Typography color="text.secondary">
                            Replace the seeded admin identity now so the instance no longer depends on the temporary account.
                          </Typography>
                        </Stack>
                        <Grid container spacing={2}>
                          <Grid size={{ xs: 12, md: 6 }}>
                            <TextField
                              fullWidth
                              label="Email"
                              type="email"
                              value={accountForm.email}
                              helperText="Use the real admin email. The default seed email should not remain in use."
                              onChange={event => setAccountForm(current => ({ ...current, email: event.target.value }))}
                            />
                          </Grid>
                          <Grid size={{ xs: 12, md: 6 }}>
                            <TextField
                              fullWidth
                              label="Username"
                              value={accountForm.username}
                              helperText="This is the username shown across the admin workspace."
                              onChange={event => setAccountForm(current => ({ ...current, username: event.target.value }))}
                            />
                          </Grid>
                          <Grid size={{ xs: 12 }}>
                            <TextField
                              fullWidth
                              label="Full name"
                              value={accountForm.fullName}
                              helperText="Used for profile identity and audit-friendly admin navigation."
                              onChange={event => setAccountForm(current => ({ ...current, fullName: event.target.value }))}
                            />
                          </Grid>
                        </Grid>
                        <Card variant="outlined" sx={{ borderRadius: 3 }}>
                          <CardContent>
                            <Stack spacing={2}>
                              <Typography variant="subtitle1">Access and experience</Typography>
                              <FormControlLabel
                                control={(
                                  <Checkbox
                                    checked={accountForm.advanced}
                                    onChange={event => setAccountForm(current => ({ ...current, advanced: event.target.checked }))}
                                  />
                                )}
                                label="Enable advanced mode for the primary admin account"
                              />
                              <Divider />
                              <Grid container spacing={2}>
                                <Grid size={{ xs: 12, md: 6 }}>
                                  <TextField
                                    fullWidth
                                    label="New password"
                                    type="password"
                                    value={accountForm.password}
                                    helperText="Choose something other than the seeded default password."
                                    onChange={event => setAccountForm(current => ({ ...current, password: event.target.value }))}
                                  />
                                </Grid>
                                <Grid size={{ xs: 12, md: 6 }}>
                                  <TextField
                                    fullWidth
                                    label="Confirm new password"
                                    type="password"
                                    value={accountForm.confirmPassword}
                                    helperText="Repeat the new password exactly once to confirm it."
                                    onChange={event => setAccountForm(current => ({ ...current, confirmPassword: event.target.value }))}
                                  />
                                </Grid>
                              </Grid>
                            </Stack>
                          </CardContent>
                        </Card>
                      </Stack>
                    ) : null}

                    {activeStep === 2 ? (
                      <Stack spacing={3}>
                        <Stack spacing={1}>
                          <Typography variant="h5">Site Settings</Typography>
                          <Typography color="text.secondary">
                            Choose the defaults that make a fresh Mealie instance feel polished on day one.
                          </Typography>
                        </Stack>
                        <Grid container spacing={2}>
                          <Grid size={{ xs: 12, md: 6 }}>
                            <Card
                              variant="outlined"
                              sx={{
                                height: "100%",
                                borderRadius: 3,
                                borderColor: settingsForm.makeGroupRecipesPublic ? "primary.main" : undefined,
                                bgcolor: settingsForm.makeGroupRecipesPublic ? "primary.main" : "transparent",
                                color: settingsForm.makeGroupRecipesPublic ? "primary.contrastText" : "inherit",
                              }}
                            >
                              <CardContent>
                                <Stack spacing={2}>
                                  <Stack direction="row" spacing={1.5} alignItems="center">
                                    <PublicRounded color={settingsForm.makeGroupRecipesPublic ? "inherit" : "primary"} />
                                    <Typography variant="h6">Recipe visibility</Typography>
                                  </Stack>
                                  <Typography color={settingsForm.makeGroupRecipesPublic ? "inherit" : "text.secondary"}>
                                    Let the initial group publish recipes by default, or keep everything private until you decide otherwise.
                                  </Typography>
                                  <FormControlLabel
                                    control={(
                                      <Checkbox
                                        checked={settingsForm.makeGroupRecipesPublic}
                                        onChange={event => setSettingsForm(current => ({ ...current, makeGroupRecipesPublic: event.target.checked }))}
                                        sx={{
                                          color: settingsForm.makeGroupRecipesPublic ? "primary.contrastText" : undefined,
                                          "&.Mui-checked": {
                                            color: settingsForm.makeGroupRecipesPublic ? "primary.contrastText" : "primary.dark",
                                          },
                                        }}
                                      />
                                    )}
                                    label="Allow this group's recipes to be public"
                                  />
                                </Stack>
                              </CardContent>
                            </Card>
                          </Grid>
                          <Grid size={{ xs: 12, md: 6 }}>
                            <Card
                              variant="outlined"
                              sx={{
                                height: "100%",
                                borderRadius: 3,
                                borderColor: settingsForm.useSeedData ? "primary.main" : undefined,
                                bgcolor: settingsForm.useSeedData ? "primary.main" : "transparent",
                                color: settingsForm.useSeedData ? "primary.contrastText" : "inherit",
                              }}
                            >
                              <CardContent>
                                <Stack spacing={2}>
                                  <Stack direction="row" spacing={1.5} alignItems="center">
                                    <AutoAwesomeRounded color={settingsForm.useSeedData ? "inherit" : "primary"} />
                                    <Typography variant="h6">Starter data</Typography>
                                  </Stack>
                                  <Typography color={settingsForm.useSeedData ? "inherit" : "text.secondary"}>
                                    Seed foods, units, and labels so organizing recipes feels complete immediately after setup.
                                  </Typography>
                                  <FormControlLabel
                                    control={(
                                      <Checkbox
                                        checked={settingsForm.useSeedData}
                                        onChange={event => setSettingsForm(current => ({ ...current, useSeedData: event.target.checked }))}
                                        sx={{
                                          color: settingsForm.useSeedData ? "primary.contrastText" : undefined,
                                          "&.Mui-checked": {
                                            color: settingsForm.useSeedData ? "primary.contrastText" : "primary.dark",
                                          },
                                        }}
                                      />
                                    )}
                                    label="Import starter foods, units, and labels"
                                  />
                                </Stack>
                              </CardContent>
                            </Card>
                          </Grid>
                        </Grid>
                      </Stack>
                    ) : null}

                    {activeStep === 3 ? (
                      <Stack spacing={3}>
                        <Stack spacing={1}>
                          <Typography variant="h5">Summary</Typography>
                          <Typography color="text.secondary">
                            Review the final choices before Mealie updates the admin account and writes the new defaults.
                          </Typography>
                        </Stack>
                        <Card variant="outlined" sx={{ borderRadius: 3 }}>
                          <CardContent>
                            <List disablePadding>
                              {summaryItems.map(([label, value], index) => (
                                <Box key={label}>
                                  <ListItem disableGutters>
                                    <ListItemIcon sx={{ minWidth: 36 }}>
                                      <CheckCircleRounded color="success" fontSize="small" />
                                    </ListItemIcon>
                                    <ListItemText
                                      primary={label}
                                      secondary={value}
                                      primaryTypographyProps={{ fontWeight: 600 }}
                                    />
                                  </ListItem>
                                  {index < summaryItems.length - 1 ? <Divider component="li" /> : null}
                                </Box>
                              ))}
                            </List>
                          </CardContent>
                        </Card>
                        <Alert severity="info">
                          Completing setup updates preferences first, seeds data if selected, and only then replaces the seeded admin credentials.
                        </Alert>
                      </Stack>
                    ) : null}

                    {activeStep === 4 ? (
                      <Stack spacing={3}>
                        <Stack spacing={1} alignItems="flex-start">
                          <CheckCircleRounded color="success" sx={{ fontSize: 44 }} />
                          <Typography variant="h5">Setup Complete</Typography>
                          <Typography color="text.secondary">
                            Your admin account has been updated and Mealie is ready for normal use.
                          </Typography>
                        </Stack>
                        <Grid container spacing={2}>
                          {[
                            "The temporary admin credentials have been replaced.",
                            settingsForm.useSeedData
                              ? "Starter foods, units, and labels were queued for import."
                              : "No starter data was imported, so the database stays minimal.",
                            settingsForm.makeGroupRecipesPublic
                              ? "Group recipes will begin in public mode."
                              : "Group recipes will begin in private mode.",
                          ].map(item => (
                            <Grid key={item} size={{ xs: 12 }}>
                              <Alert severity="success" icon={<CheckCircleRounded fontSize="inherit" />}>
                                {item}
                              </Alert>
                            </Grid>
                          ))}
                        </Grid>
                      </Stack>
                    ) : null}

                    <Divider />

                    <Stack direction="row" justifyContent="space-between" alignItems="center">
                      <Button variant="text" disabled={activeStep === 0 || submitting} onClick={handleBack}>
                        Back
                      </Button>
                      <Button variant="contained" size="large" disabled={submitting} onClick={handleNext}>
                        {activeStep === 3 ? "Complete setup" : activeStep === 4 ? "Go to home" : "Next"}
                      </Button>
                    </Stack>
                  </Stack>
                </CardContent>
              </Card>
            </Grid>

            <Grid size={{ xs: 12, md: 4 }}>
              <Stack spacing={2}>
                <Card variant="outlined" sx={{ borderRadius: 3 }}>
                  <CardContent>
                    <Stack spacing={2}>
                      <Typography variant="h6">Setup checklist</Typography>
                      <List disablePadding>
                        {setupChecklist.map((item, index) => (
                          <ListItem key={item} disableGutters sx={{ alignItems: "flex-start" }}>
                            <ListItemIcon sx={{ minWidth: 36, mt: 0.25 }}>
                              <CheckCircleRounded color={index <= activeStep ? "success" : "disabled"} fontSize="small" />
                            </ListItemIcon>
                            <ListItemText
                              primary={item}
                              primaryTypographyProps={{ color: index <= activeStep ? "text.primary" : "text.secondary" }}
                            />
                          </ListItem>
                        ))}
                      </List>
                    </Stack>
                  </CardContent>
                </Card>

                <Card variant="outlined" sx={{ borderRadius: 3 }}>
                  <CardContent>
                    <Stack spacing={2}>
                      <Typography variant="h6">Current selections</Typography>
                      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                        {selectionChips.map(item => (
                          <Chip key={item} label={item} color="primary" variant="outlined" />
                        ))}
                      </Stack>
                      <Divider />
                      <Typography variant="subtitle2">Temporary admin account</Typography>
                      <Typography color="text.secondary">
                        {DEFAULT_EMAIL}
                      </Typography>
                      <Typography color="text.secondary">
                        Password: {DEFAULT_PASSWORD}
                      </Typography>
                    </Stack>
                  </CardContent>
                </Card>

                <Card
                  variant="outlined"
                  sx={{
                    borderRadius: 3,
                    bgcolor: theme => theme.palette.action.hover,
                  }}
                >
                  <CardContent>
                    <Stack spacing={1.5}>
                      <Stack direction="row" spacing={1.5} alignItems="center">
                        <AutoAwesomeRounded color="primary" />
                        <Typography variant="h6">Why this matters</Typography>
                      </Stack>
                      <Typography color="text.secondary">
                        A fresh Mealie install should feel intentional, not temporary. This wizard helps you land the admin account, content defaults, and starter data in one pass.
                      </Typography>
                    </Stack>
                  </CardContent>
                </Card>
              </Stack>
            </Grid>
          </Grid>
        </Box>
      </Card>
    </Box>
  );
}
