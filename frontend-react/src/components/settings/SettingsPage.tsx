import type { ReactNode } from "react";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { AppShell } from "@/components/layout/AppShell";
import type { PrivateUser } from "@/lib/api/contracts";

type Props = {
  user?: PrivateUser | null;
  title: string;
  description?: ReactNode;
  actions?: ReactNode;
  children: ReactNode;
};

export function SettingsPage({ user, title, description, actions, children }: Props) {
  if (!user) {
    return (
      <Box sx={{ display: "grid", placeItems: "center", minHeight: "40vh" }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <AppShell groupSlug={user.groupSlug} userName={user.fullName} title={title}>
      <Stack spacing={3}>
        <Stack
          direction={{ xs: "column", md: "row" }}
          spacing={2}
          alignItems={{ md: "center" }}
          justifyContent="space-between"
        >
          <Stack spacing={1}>
            <Typography variant="h4">{title}</Typography>
            {description ? (
              <Typography color="text.secondary">
                {description}
              </Typography>
            ) : null}
          </Stack>
          {actions ? <Box>{actions}</Box> : null}
        </Stack>
        {children}
      </Stack>
    </AppShell>
  );
}
