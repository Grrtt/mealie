import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import CssBaseline from "@mui/material/CssBaseline";
import Link from "@mui/material/Link";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useTranslation } from "react-i18next";
import { Outlet } from "@tanstack/react-router";

export function RootRouteComponent() {
  return (
    <>
      <CssBaseline />
      <Outlet />
    </>
  );
}

export function RoutePendingComponent() {
  return (
    <Box sx={{ display: "grid", placeItems: "center", minHeight: "50vh" }}>
      <CircularProgress />
    </Box>
  );
}

export function NotFoundRouteComponent() {
  const { t } = useTranslation();

  return (
    <Box sx={{ display: "grid", placeItems: "center", minHeight: "50vh", px: 3 }}>
      <Stack spacing={2} alignItems="center" textAlign="center">
        <Typography variant="h4">{t("page.404-not-found")}</Typography>
        <Typography color="text.secondary">{t("page.404-page-not-found")}</Typography>
        <Link href="/" underline="hover">
          Mealie
        </Link>
      </Stack>
    </Box>
  );
}
