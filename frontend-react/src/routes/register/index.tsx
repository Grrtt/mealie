import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { Link, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { FormTextField } from "@/components/forms";
import { getAppInfo, registerUser } from "@/features/auth/session";
import { useZodForm } from "@/lib/forms/useZodForm";
import { registerSchema } from "@/lib/validation";

export function RegisterRouteComponent() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [signupAllowed, setSignupAllowed] = useState(true);

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

  const submit = form.handleSubmit(async values => {
    setBusy(true);
    setError(null);
    setSuccess(null);

    try {
      await registerUser({
        email: values.email,
        username: values.username,
        fullName: values.fullName,
        password: values.password,
        groupToken: values.groupToken || undefined,
      });
      setSuccess(t("user-registration.registration-success"));
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
            {error ? <Alert severity="error">{error}</Alert> : null}
            {success ? <Alert severity="success">{success}</Alert> : null}
            <FormTextField control={form.control} name="email" label={t("user.email")} autoFocus />
            <FormTextField control={form.control} name="fullName" label={t("user.full-name")} />
            <FormTextField control={form.control} name="username" label={t("user.username")} />
            <FormTextField control={form.control} name="groupToken" label={t("group.group-token")} />
            <FormTextField control={form.control} name="password" label={t("user.password")} type="password" />
            <FormTextField
              control={form.control}
              name="passwordConfirm"
              label={t("user.confirm-password")}
              type="password"
            />
            <Button variant="contained" type="submit" disabled={busy || !signupAllowed}>
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
