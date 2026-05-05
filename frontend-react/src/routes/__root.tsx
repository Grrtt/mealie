import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import CssBaseline from "@mui/material/CssBaseline";
import Link from "@mui/material/Link";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { Outlet } from "@tanstack/react-router";
import { useEffect, useMemo } from "react";
import { resolveLegacyFallbackUrl } from "@/config/releaseVariant";

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

export function LegacyFallbackRouteComponent() {
  const fallbackUrl = useMemo(() => {
    if (typeof window === "undefined") {
      return "/";
    }

    return resolveLegacyFallbackUrl(`${window.location.pathname}${window.location.search}${window.location.hash}`);
  }, []);

  useEffect(() => {
    if (typeof window === "undefined" || fallbackUrl === window.location.href) {
      return;
    }

    window.location.replace(fallbackUrl);
  }, [fallbackUrl]);

  return (
    <Box sx={{ display: "grid", placeItems: "center", minHeight: "50vh", px: 3 }}>
      <Stack spacing={2} alignItems="center" textAlign="center">
        <CircularProgress />
        <Typography variant="h5">Redirecting to the legacy frontend</Typography>
        <Typography color="text.secondary">
          This route is still pinned to the legacy Nuxt experience for the active release variant.
        </Typography>
        <Link href={fallbackUrl} underline="hover">
          Continue to the legacy route
        </Link>
      </Stack>
    </Box>
  );
}
