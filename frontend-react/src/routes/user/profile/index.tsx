import { useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardActions from "@mui/material/CardActions";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Dialog, DialogActions, DialogContent, DialogTitle } from "@/components/dialogs";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createHouseholdInvite, fetchHouseholdStatistics, sendHouseholdInvitationEmail } from "@/features/settings/api";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { apiClient } from "@/lib/api/client";

class InviteEmailError extends Error {
  constructor(message: string, public readonly token: string) {
    super(message);
    this.name = "InviteEmailError";
  }
}

export function UserProfileRouteComponent() {
  const { data: user } = useCurrentUser();
  const [inviteToken, setInviteToken] = useState<string | null>(null);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false);
  const [inviteStatus, setInviteStatus] = useState<string | null>(null);
  const [inviteError, setInviteError] = useState<string | null>(null);
  const statsQuery = useQuery({
    queryKey: ["profile-household-stats"],
    queryFn: fetchHouseholdStatistics,
    enabled: Boolean(user),
  });

  const inviteMutation = useMutation({
    mutationFn: async (email: string) => {
      const invite = await createHouseholdInvite();

      try {
        await sendHouseholdInvitationEmail({ email, token: invite.token });
      } catch (error) {
        const message = error instanceof Error ? error.message : "Unable to send invite email.";
        throw new InviteEmailError(message, invite.token);
      }

      return { email, token: invite.token };
    },
    onSuccess: result => {
      setInviteError(null);
      setInviteToken(null);
      setInviteStatus(`Invitation email sent to ${result.email}.`);
      setInviteEmail("");
      setInviteDialogOpen(false);
    },
    onError: error => {
      setInviteToken(error instanceof InviteEmailError ? error.token : null);
      setInviteStatus(null);
      setInviteError(error instanceof Error ? error.message : "Unable to send invite email.");
    },
  });

  const links = [
    {
      title: "Edit profile",
      description: "Update account details, password, and personal preferences.",
      href: "/user/profile/edit",
      show: true,
    },
    {
      title: "Favorites",
      description: "Review the recipes you have favorited in the current group.",
      href: user ? `/user/${user.id}/favorites` : "/user/profile",
      show: true,
    },
    {
      title: "API tokens",
      description: "Create and revoke long-lived API tokens for integrations.",
      href: "/user/profile/api-tokens",
      show: Boolean(user?.advanced),
    },
    {
      title: "Household settings",
      description: "Adjust shared household preferences and announcements.",
      href: "/household",
      show: Boolean(user?.canManageHousehold),
    },
    {
      title: "Household members",
      description: "Review member permissions for management, organizing, and invites.",
      href: "/household/members",
      show: Boolean(user?.canManage),
    },
    {
      title: "Household notifiers",
      description: "Configure Apprise-based event notifications.",
      href: "/household/notifiers",
      show: Boolean(user?.advanced),
    },
    {
      title: "Household webhooks",
      description: "Manage household webhook deliveries and schedule settings.",
      href: "/household/webhooks",
      show: Boolean(user?.advanced),
    },
    {
      title: "Group settings",
      description: "Adjust group privacy and announcement preferences.",
      href: "/group",
      show: Boolean(user?.canManage),
    },
    {
      title: "Group data",
      description: "Open organizer, report, and migration helper surfaces.",
      href: "/group/data",
      show: Boolean(user?.canOrganize || user?.canManage),
    },
    {
      title: "Admin settings",
      description: "Open the admin maintenance and management surfaces.",
      href: "/admin/site-settings",
      show: Boolean(user?.admin),
    },
  ].filter(link => link.show);

  return (
    <SettingsPage
      user={user}
      title="Profile"
      description="Manage the same personal, household, group, and administrative destinations that are available in the legacy frontend."
      actions={user?.canInvite ? (
        <Button
          variant="contained"
          onClick={() => {
            setInviteError(null);
            setInviteStatus(null);
            setInviteToken(null);
            setInviteDialogOpen(true);
          }}
          disabled={inviteMutation.isPending}
        >
          Send invite email
        </Button>
      ) : undefined}
    >
      {inviteStatus ? (
        <Alert severity="success" onClose={() => setInviteStatus(null)}>
          {inviteStatus}
        </Alert>
      ) : null}
      {inviteToken ? (
        <Alert severity="warning" onClose={() => setInviteToken(null)}>
          Invite token created but email delivery failed: <strong>{inviteToken}</strong>
        </Alert>
      ) : null}
      {inviteError ? (
        <Alert severity="error" onClose={() => setInviteError(null)}>
          {inviteError}
        </Alert>
      ) : null}

      <Grid container spacing={2}>
        {[
          { label: "Recipes", value: statsQuery.data?.totalRecipes ?? 0 },
          { label: "Members", value: statsQuery.data?.totalUsers ?? 0 },
          { label: "Categories", value: statsQuery.data?.totalCategories ?? 0 },
          { label: "Tags", value: statsQuery.data?.totalTags ?? 0 },
          { label: "Tools", value: statsQuery.data?.totalTools ?? 0 },
        ].map(stat => (
          <Grid key={stat.label} size={{ xs: 12, sm: 6, md: 2.4 }}>
            <Card variant="outlined">
              <CardContent>
                <Typography color="text.secondary" variant="body2">{stat.label}</Typography>
                <Typography variant="h4">{stat.value}</Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
        {user?.admin ? <Chip color="secondary" label="Admin" /> : null}
        {user?.canManage ? <Chip label="Group manager" /> : null}
        {user?.canOrganize ? <Chip label="Organizer" /> : null}
        {user?.canManageHousehold ? <Chip label="Household manager" /> : null}
        {user?.advanced ? <Chip label="Advanced mode" /> : null}
      </Stack>

      <Grid container spacing={2}>
        {links.map(link => (
          <Grid key={link.href} size={{ xs: 12, md: 6 }}>
            <Card sx={{ height: "100%" }}>
              <CardContent>
                <Stack spacing={1}>
                  <Typography variant="h6">{link.title}</Typography>
                  <Typography color="text.secondary">{link.description}</Typography>
                </Stack>
              </CardContent>
              <CardActions>
                <Button href={apiClient.resolvePath(link.href)}>Open</Button>
              </CardActions>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Dialog open={inviteDialogOpen} onClose={() => setInviteDialogOpen(false)} fullWidth maxWidth="sm">
        <DialogTitle>Send invite email</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <Typography color="text.secondary">
              Create a new invite token and email a secure registration link directly to the recipient.
            </Typography>
            <TextField
              autoFocus
              label="Recipient email"
              type="email"
              value={inviteEmail}
              onChange={event => setInviteEmail(event.target.value)}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setInviteDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => inviteMutation.mutate(inviteEmail.trim())}
            disabled={inviteMutation.isPending || !inviteEmail.trim()}
          >
            Send invite
          </Button>
        </DialogActions>
      </Dialog>
    </SettingsPage>
  );
}
