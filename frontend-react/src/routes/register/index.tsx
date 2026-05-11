import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { Link, useNavigate, useSearch } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { FormTextField } from "@/components/forms";
import { getDefaultLandingRoute } from "@/features/auth/defaultLanding";
import { getAppInfo, registerUser, resolveRegistrationInvite } from "@/features/auth/session";
import { useZodForm } from "@/lib/forms/useZodForm";
import { registerSchema } from "@/lib/validation";

export function RegisterRouteComponent() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const search = useSearch({ strict: false }) as { invite?: string; token?: string; email?: string };
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [signupAllowed, setSignupAllowed] = useState(true);
  const [secureInviteLoading, setSecureInviteLoading] = useState(false);
  const [secureInviteReady, setSecureInviteReady] = useState(false);
  const [secureInviteError, setSecureInviteError] = useState<string | null>(null);

  const form = useZodForm(registerSchema, {
    defaultValues: {
      email: "",
      username: "",
      fullName: "",
      password: "",
      passwordConfirm: "",
      groupToken: "",
    },
  });

  useEffect(() => {
    document.title = t("user.register");
  }, [t]);

  useEffect(() => {
    getAppInfo()
      .then(info => setSignupAllowed(info.allowSignup))
      .catch(() => setSignupAllowed(true));
  }, []);

  useEffect(() => {
    if (search.invite || !search.token) {
      return;
    }

    form.setValue("groupToken", search.token);
  }, [form, search.invite, search.token]);

  useEffect(() => {
    if (search.invite || !search.email) {
      return;
    }

    form.setValue("email", search.email);
  }, [form, search.email, search.invite]);

  useEffect(() => {
    if (!search.invite) {
      setSecureInviteLoading(false);
      setSecureInviteReady(false);
      setSecureInviteError(null);
      return;
    }

    let cancelled = false;
    setSecureInviteLoading(true);
    setSecureInviteReady(false);
    setSecureInviteError(null);

    resolveRegistrationInvite(search.invite)
      .then(prefill => {
        if (cancelled) {
          return;
        }

        form.setValue("email", prefill.email);
        form.setValue("groupToken", "");
        setSecureInviteReady(true);
      })
      .catch(inviteError => {
        if (cancelled) {
          return;
        }

        setSecureInviteError(inviteError instanceof Error ? inviteError.message : t("events.something-went-wrong"));
      })
      .finally(() => {
        if (!cancelled) {
          setSecureInviteLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [form, search.invite, t]);

  const submit = form.handleSubmit(async values => {
    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      const result = await registerUser({
        email: values.email,
        username: values.username,
        fullName: values.fullName,
        password: values.password,
        groupToken: secureInviteReady ? undefined : values.groupToken || undefined,
        invite: secureInviteReady ? search.invite : undefined,
      });
      setSuccess(result.detail);

      if (result.authenticated && result.user) {
        await navigate({ href: await getDefaultLandingRoute(result.user) });
        return;
      }

      await navigate({ to: "/login" });
    }
    catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : t("events.something-went-wrong"));
    }
    finally {
      setBusy(false);
    }
  });

  return (
    <Box sx={{ display: "grid", placeItems: "center", minHeight: "100vh", p: 2 }}>
      <Card sx={{ width: "100%", maxWidth: 640 }}>
        <CardContent>
          <Stack spacing={3} component="form" onSubmit={submit}>
            <Typography variant="h4" textAlign="center">
              {t("user-registration.user-registration")}
            </Typography>
            {!signupAllowed ? <Alert severity="warning">{t("user.invite-only")}</Alert> : null}
            {secureInviteReady ? <Alert severity="info">{t("user-registration.secure-invite-loaded")}</Alert> : null}
            {secureInviteError ? <Alert severity="warning">{secureInviteError}</Alert> : null}
            {error ? <Alert severity="error">{error}</Alert> : null}
            {success ? <Alert severity="success">{success}</Alert> : null}
            <FormTextField
              control={form.control}
              name="email"
              label={t("user.email")}
              autoFocus={!secureInviteReady}
              disabled={secureInviteReady}
              helperText={secureInviteReady ? t("user-registration.secure-invite-email-locked") : undefined}
            />
            <FormTextField control={form.control} name="fullName" label={t("user.full-name")} autoFocus={secureInviteReady} />
            <FormTextField control={form.control} name="username" label={t("user.username")} />
            {!secureInviteReady ? (
              <FormTextField control={form.control} name="groupToken" label={t("group.group-token")} />
            ) : null}
            <FormTextField control={form.control} name="password" label={t("user.password")} type="password" />
            <FormTextField
              control={form.control}
              name="passwordConfirm"
              label={t("user.confirm-password")}
              type="password"
            />
            <Button variant="contained" type="submit" disabled={busy || !signupAllowed || secureInviteLoading}>
              {t("user.register")}
            </Button>
            <Button component={Link} to="/login">
              {t("user.login")}
            </Button>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
}
