import { useEffect, useMemo, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardActions from "@mui/material/CardActions";
import CardContent from "@mui/material/CardContent";
import Checkbox from "@mui/material/Checkbox";
import CircularProgress from "@mui/material/CircularProgress";
import FormControlLabel from "@mui/material/FormControlLabel";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { Link, useNavigate, useSearch } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { FormTextField } from "@/components/forms";
import { getDefaultLandingRoute } from "@/features/auth/defaultLanding";
import { consumeIntendedDestination } from "@/features/auth/redirectStore";
import {
  beginOidcSignIn,
  getAppInfo,
  getStartupInfo,
  signInWithOidcCallback,
  signInWithPassword,
} from "@/features/auth/session";
import { useZodForm } from "@/lib/forms/useZodForm";
import { loginSchema } from "@/lib/validation";

type LoginSearch = {
  redirect?: string;
  direct?: string;
  code?: string;
  error?: string;
};

function isCallback(search: LoginSearch) {
  return Boolean(search.code || search.error);
}

export function LoginRouteComponent() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const search = useSearch({ strict: false }) as LoginSearch;
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [appInfo, setAppInfo] = useState<Awaited<ReturnType<typeof getAppInfo>> | null>(null);
  const [startupInfo, setStartupInfo] = useState<Awaited<ReturnType<typeof getStartupInfo>> | null>(null);
  const [appInfoError, setAppInfoError] = useState<string | null>(null);

  const form = useZodForm(loginSchema, {
    defaultValues: {
      username: "",
      password: "",
      rememberMe: false,
    },
  });

  useEffect(() => {
    document.title = t("user.login");
  }, [t]);

  useEffect(() => {
    Promise.all([getAppInfo(), getStartupInfo()])
      .then(([nextAppInfo, nextStartupInfo]) => {
        setAppInfo(nextAppInfo);
        setStartupInfo(nextStartupInfo);

        if (nextStartupInfo.isFirstLogin) {
          form.reset({
            username: "changeme@example.com",
            password: "MyPassword",
            rememberMe: false,
          });
        }
      })
      .catch(err => setAppInfoError(err instanceof Error ? err.message : "Failed to load app info"));
  }, [form]);

  useEffect(() => {
    if (!appInfo || search.direct === "1" || isCallback(search)) return;
    if (appInfo.enableOidc && appInfo.oidcRedirect) {
      beginOidcSignIn();
    }
  }, [appInfo, search]);

  useEffect(() => {
    if (!isCallback(search)) return;

    setBusy(true);
    signInWithOidcCallback(window.location.search)
      .then(async user => {
        if (!user) {
          throw new Error("Failed to hydrate user");
        }

        await navigate({
          href: search.redirect
            ?? consumeIntendedDestination()
            ?? (await getDefaultLandingRoute(user)),
        });
      })
      .catch(err => {
        setError(err instanceof Error ? err.message : t("events.something-went-wrong"));
        navigate({
          href: "/login?direct=1",
          replace: true,
        }).catch(() => undefined);
      })
      .finally(() => setBusy(false));
  }, [navigate, search, t]);

  const allowPasswordLogin = appInfo?.allowPasswordLogin ?? true;
  const allowSignup = appInfo?.allowSignup ?? true;
  const enableOidc = appInfo?.enableOidc ?? false;
  const isFirstLogin = startupInfo?.isFirstLogin ?? false;

  const loading = useMemo(() => !appInfo && !appInfoError, [appInfo, appInfoError]);

  const submit = form.handleSubmit(async values => {
    setBusy(true);
    setError(null);

    try {
      const user = await signInWithPassword(values);
      if (!user) {
        throw new Error(t("user.invalid-credentials"));
      }

      await navigate({
        href: search.redirect
          ?? consumeIntendedDestination()
          ?? (await getDefaultLandingRoute(user)),
      });
    }
    catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : t("events.something-went-wrong"));
    }
    finally {
      setBusy(false);
    }
  });

  if (loading || busy && isCallback(search)) {
    return (
      <Box sx={{ display: "grid", placeItems: "center", minHeight: "100vh" }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ display: "grid", placeItems: "center", minHeight: "100vh", p: 2 }}>
      <Card sx={{ width: "100%", maxWidth: 560 }}>
        <CardContent>
          <Stack spacing={3}>
            <Typography variant="h4" textAlign="center">
              Mealie
            </Typography>
            <Typography variant="h5" textAlign="center">
              {t("user.sign-in")}
            </Typography>
            {error ? <Alert severity="error">{error}</Alert> : null}
            {appInfoError ? <Alert severity="warning">{appInfoError}</Alert> : null}
            {isFirstLogin ? (
              <Alert severity="info">
                First-time setup detected. Sign in with the default admin account to continue setup:
                <br />
                changeme@example.com / MyPassword
              </Alert>
            ) : null}
            <Stack component="form" spacing={2} onSubmit={submit}>
              {allowPasswordLogin ? (
                <>
                  <FormTextField control={form.control} name="username" label={t("user.email-or-username")} autoFocus />
                  <FormTextField
                    control={form.control}
                    name="password"
                    label={t("user.password")}
                    type="password"
                  />
                  <FormControlLabel
                    control={
                      <Checkbox
                        checked={form.watch("rememberMe")}
                        onChange={(_, checked) => form.setValue("rememberMe", checked)}
                      />
                    }
                    label={t("user.remember-me")}
                  />
                  <Button variant="contained" size="large" type="submit" disabled={busy}>
                    {t("user.login")}
                  </Button>
                </>
              ) : null}
              {enableOidc ? (
                <Button variant="outlined" size="large" onClick={beginOidcSignIn}>
                  {t("user.login-oidc")} {appInfo?.oidcProviderName}
                </Button>
              ) : null}
            </Stack>
          </Stack>
        </CardContent>
        <CardActions sx={{ justifyContent: "space-between", px: 3, pb: 3 }}>
          {allowSignup ? <Button component={Link} to="/register">{t("user.register")}</Button> : <span />}
          <Button component={Link} to="/forgot-password">{t("user.reset-password")}</Button>
        </CardActions>
      </Card>
    </Box>
  );
}
