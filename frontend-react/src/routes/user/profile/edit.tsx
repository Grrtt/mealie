import { useEffect, useMemo, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Checkbox from "@mui/material/Checkbox";
import FormControlLabel from "@mui/material/FormControlLabel";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { getStoredDefaultActivity, setStoredDefaultActivity } from "@/features/auth/defaultLanding";
import { currentUserQueryKey } from "@/features/auth/session";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { changeOwnPassword, updateCurrentUser } from "@/features/settings/api";
import { queryClient } from "@/lib/query/queryClient";
import type { ActivityKey } from "@/lib/api/contracts";

type ProfileForm = {
  username: string;
  fullName: string;
  email: string;
  showAnnouncements: boolean;
  advanced: boolean;
  defaultActivity: ActivityKey;
};

export function UserProfileEditRouteComponent() {
  const { data: user } = useCurrentUser();
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [passwordStatus, setPasswordStatus] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [passwordForm, setPasswordForm] = useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  });
  const initialForm = useMemo<ProfileForm>(() => ({
    username: user?.username ?? "",
    fullName: user?.fullName ?? "",
    email: user?.email ?? "",
    showAnnouncements: Boolean(user?.showAnnouncements),
    advanced: Boolean(user?.advanced),
    defaultActivity: getStoredDefaultActivity(),
  }), [user]);
  const [form, setForm] = useState<ProfileForm>(initialForm);

  useEffect(() => {
    setForm(initialForm);
  }, [initialForm]);

  const profileMutation = useMutation({
    mutationFn: async () => {
      if (!user) throw new Error("Missing current user.");
      return await updateCurrentUser(user.id, {
        username: form.username,
        fullName: form.fullName,
        email: form.email,
        showAnnouncements: form.showAnnouncements,
        advanced: form.advanced,
      });
    },
    onSuccess: async () => {
      setStoredDefaultActivity(form.defaultActivity);
      setStatus("Profile updated.");
      setError(null);
      await queryClient.invalidateQueries({ queryKey: currentUserQueryKey });
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to update profile.");
    },
  });

  const passwordMutation = useMutation({
    mutationFn: async () => {
      if (passwordForm.newPassword !== passwordForm.confirmPassword) {
        throw new Error("New passwords must match.");
      }
      return await changeOwnPassword({
        currentPassword: passwordForm.currentPassword,
        newPassword: passwordForm.newPassword,
      });
    },
    onSuccess: () => {
      setPasswordStatus("Password updated.");
      setPasswordError(null);
      setPasswordForm({ currentPassword: "", newPassword: "", confirmPassword: "" });
    },
    onError: mutationError => {
      setPasswordError(mutationError instanceof Error ? mutationError.message : "Unable to change password.");
    },
  });

  return (
    <SettingsPage
      user={user}
      title="Edit profile"
      description="Update your account details, password, advanced mode, announcement preference, and default landing activity."
    >
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h6">Account details</Typography>
            <TextField
              label="Username"
              value={form.username}
              onChange={event => setForm(current => ({ ...current, username: event.target.value }))}
            />
            <TextField
              label="Full name"
              value={form.fullName}
              onChange={event => setForm(current => ({ ...current, fullName: event.target.value }))}
            />
            <TextField
              label="Email"
              type="email"
              value={form.email}
              onChange={event => setForm(current => ({ ...current, email: event.target.value }))}
            />
            <TextField
              select
              label="Default activity"
              value={form.defaultActivity}
              onChange={event => setForm(current => ({ ...current, defaultActivity: event.target.value as ActivityKey }))}
            >
              <MenuItem value="recipes">Recipes</MenuItem>
              <MenuItem value="mealplanner">Meal planner</MenuItem>
              <MenuItem value="shopping_list">Shopping lists</MenuItem>
            </TextField>
            <FormControlLabel
              control={(
                <Checkbox
                  checked={form.showAnnouncements}
                  onChange={event => setForm(current => ({ ...current, showAnnouncements: event.target.checked }))}
                />
              )}
              label="Show announcements from Mealie"
            />
            <FormControlLabel
              control={(
                <Checkbox
                  checked={form.advanced}
                  onChange={event => setForm(current => ({ ...current, advanced: event.target.checked }))}
                />
              )}
              label="Enable advanced mode"
            />
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => profileMutation.mutate()} disabled={profileMutation.isPending}>
                Save profile
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      {passwordStatus ? <Alert severity="success" onClose={() => setPasswordStatus(null)}>{passwordStatus}</Alert> : null}
      {passwordError ? <Alert severity="error" onClose={() => setPasswordError(null)}>{passwordError}</Alert> : null}

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h6">Change password</Typography>
            <TextField
              label="Current password"
              type="password"
              value={passwordForm.currentPassword}
              onChange={event => setPasswordForm(current => ({ ...current, currentPassword: event.target.value }))}
            />
            <TextField
              label="New password"
              type="password"
              value={passwordForm.newPassword}
              onChange={event => setPasswordForm(current => ({ ...current, newPassword: event.target.value }))}
            />
            <TextField
              label="Confirm new password"
              type="password"
              value={passwordForm.confirmPassword}
              onChange={event => setPasswordForm(current => ({ ...current, confirmPassword: event.target.value }))}
            />
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="outlined" onClick={() => passwordMutation.mutate()} disabled={passwordMutation.isPending}>
                Update password
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </SettingsPage>
  );
}
