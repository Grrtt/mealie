import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { Link, useSearch } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { FormTextField } from "@/components/forms";
import { resetPassword } from "@/features/auth/session";
import { useZodForm } from "@/lib/forms/useZodForm";
import { resetPasswordSchema } from "@/lib/validation";

type ResetSearch = {
  token?: string;
};

export function ResetPasswordRouteComponent() {
  const { t } = useTranslation();
  const search = useSearch({ strict: false }) as ResetSearch;
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const form = useZodForm(resetPasswordSchema, {
    defaultValues: {
      email: "",
      password: "",
      passwordConfirm: "",
      token: search.token ?? "",
    },
  });

  useEffect(() => {
    document.title = t("user.reset-password");
  }, [t]);

  useEffect(() => {
    form.setValue("token", search.token ?? "");
  }, [form, search.token]);

  const submit = form.handleSubmit(async values => {
    setError(null);
    setSuccess(null);

    try {
      const response = await resetPassword(values.token, values.password);
      setSuccess(response.detail);
    }
    catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : t("events.something-went-wrong"));
    }
  });

  return (
    <Box sx={{ display: "grid", placeItems: "center", minHeight: "100vh", p: 2 }}>
      <Card sx={{ width: "100%", maxWidth: 560 }}>
        <CardContent>
          <Stack spacing={3} component="form" onSubmit={submit}>
            <Typography variant="h4" textAlign="center">
              {t("user.reset-password")}
            </Typography>
            {!search.token ? <Alert severity="warning">Token required</Alert> : null}
            {error ? <Alert severity="error">{error}</Alert> : null}
            {success ? <Alert severity="success">{success}</Alert> : null}
            <FormTextField control={form.control} name="email" label={t("user.email")} />
            <FormTextField control={form.control} name="password" label={t("user.password")} type="password" />
            <FormTextField
              control={form.control}
              name="passwordConfirm"
              label={t("user.confirm-password")}
              type="password"
            />
            <Button variant="contained" type="submit" disabled={!search.token}>
              {t("user.reset-password")}
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
