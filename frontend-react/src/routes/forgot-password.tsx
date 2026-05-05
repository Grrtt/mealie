import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { FormTextField } from "@/components/forms";
import { sendForgotPassword } from "@/features/auth/session";
import { useZodForm } from "@/lib/forms/useZodForm";
import { forgotPasswordSchema } from "@/lib/validation";

export function ForgotPasswordRouteComponent() {
  const { t } = useTranslation();
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const form = useZodForm(forgotPasswordSchema, {
    defaultValues: {
      email: "",
    },
  });

  useEffect(() => {
    document.title = t("user.forgot-password");
  }, [t]);

  const submit = form.handleSubmit(async values => {
    setError(null);
    setSuccess(null);

    try {
      const response = await sendForgotPassword(values.email);
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
              {t("user.forgot-password")}
            </Typography>
            {error ? <Alert severity="error">{error}</Alert> : null}
            {success ? <Alert severity="success">{success}</Alert> : null}
            <FormTextField control={form.control} name="email" label={t("user.email")} autoFocus />
            <Button variant="contained" type="submit">
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
