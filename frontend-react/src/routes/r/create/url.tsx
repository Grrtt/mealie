import { useEffect } from "react";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { useCurrentUser } from "@/features/auth/useCurrentUser";

export function ShareTargetRecipeCreateUrlRouteComponent() {
  const navigate = useNavigate();
  const search = useSearch({ strict: false }) as Record<string, string | undefined>;
  const { data: user } = useCurrentUser();

  useEffect(() => {
    if (!user?.groupSlug) return;

    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(search)) {
      if (value) params.set(key, value);
    }

    navigate({
      href: `/g/${user.groupSlug}/r/create/url${params.toString() ? `?${params.toString()}` : ""}`,
      replace: true,
    }).catch(() => undefined);
  }, [navigate, search, user?.groupSlug]);

  return (
    <Box sx={{ display: "grid", placeItems: "center", minHeight: "50vh" }}>
      <CircularProgress />
    </Box>
  );
}
