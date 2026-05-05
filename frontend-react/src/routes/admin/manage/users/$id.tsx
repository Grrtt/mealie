import { useEffect, useMemo, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import { useParams } from "@tanstack/react-router";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import {
  fetchAdminGroups,
  fetchAdminHouseholds,
  fetchAdminUser,
  generatePasswordResetToken,
  updateAdminUser,
} from "@/features/settings/api";

export function AdminManageUserDetailRouteComponent() {
  const { id } = useParams({ from: "/admin/manage/users/$id" });
  const { data: user } = useCurrentUser();
  const userQuery = useQuery({ queryKey: ["admin-user", id], queryFn: async () => await fetchAdminUser(id) });
  const groupsQuery = useQuery({ queryKey: ["admin-groups"], queryFn: fetchAdminGroups });
  const householdsQuery = useQuery({ queryKey: ["admin-households"], queryFn: fetchAdminHouseholds });
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [resetToken, setResetToken] = useState<string | null>(null);
  const [form, setForm] = useState<Record<string, string | boolean>>({});

  useEffect(() => {
    if (userQuery.data) {
      setForm({
        username: userQuery.data.username ?? "",
        fullName: userQuery.data.fullName ?? "",
        email: userQuery.data.email,
        group: userQuery.data.group,
        household: userQuery.data.household,
        admin: Boolean(userQuery.data.admin),
        advanced: Boolean(userQuery.data.advanced),
      });
    }
  }, [userQuery.data]);

  const mutation = useMutation({
    mutationFn: async () => await updateAdminUser(id, {
      ...userQuery.data,
      username: String(form.username ?? ""),
      fullName: String(form.fullName ?? ""),
      email: String(form.email ?? ""),
      group: String(form.group ?? ""),
      household: String(form.household ?? ""),
      admin: Boolean(form.admin),
      advanced: Boolean(form.advanced),
    }),
    onSuccess: async () => {
      setStatus("User updated.");
      setError(null);
      await userQuery.refetch();
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to update user.");
    },
  });
  const resetMutation = useMutation({
    mutationFn: async () => await generatePasswordResetToken(String(form.email ?? "")),
    onSuccess: result => setResetToken(`${window.location.origin}/reset-password?token=${result.token}`),
  });

  const groups = useMemo(() => groupsQuery.data?.items ?? [], [groupsQuery.data?.items]);
  const households = useMemo(() => householdsQuery.data?.items ?? [], [householdsQuery.data?.items]);

  return (
    <SettingsPage user={user} title="Edit user" description="Update user assignments and generate a password reset token.">
      {status ? <Alert severity="success">{status}</Alert> : null}
      {error ? <Alert severity="error">{error}</Alert> : null}
      {resetToken ? <Alert severity="info">{resetToken}</Alert> : null}
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <TextField label="Username" value={String(form.username ?? "")} onChange={event => setForm(current => ({ ...current, username: event.target.value }))} />
            <TextField label="Full name" value={String(form.fullName ?? "")} onChange={event => setForm(current => ({ ...current, fullName: event.target.value }))} />
            <TextField label="Email" value={String(form.email ?? "")} onChange={event => setForm(current => ({ ...current, email: event.target.value }))} />
            <TextField select label="Group" value={String(form.group ?? "")} onChange={event => setForm(current => ({ ...current, group: event.target.value }))}>
              {groups.map(item => <MenuItem key={item.id} value={item.name}>{item.name}</MenuItem>)}
            </TextField>
            <TextField select label="Household" value={String(form.household ?? "")} onChange={event => setForm(current => ({ ...current, household: event.target.value }))}>
              {households.map(item => <MenuItem key={item.id} value={item.name}>{item.name}</MenuItem>)}
            </TextField>
            <TextField select label="Admin" value={String(Boolean(form.admin))} onChange={event => setForm(current => ({ ...current, admin: event.target.value === "true" }))}>
              <MenuItem value="true">Yes</MenuItem>
              <MenuItem value="false">No</MenuItem>
            </TextField>
            <TextField select label="Advanced mode" value={String(Boolean(form.advanced))} onChange={event => setForm(current => ({ ...current, advanced: event.target.value === "true" }))}>
              <MenuItem value="true">Enabled</MenuItem>
              <MenuItem value="false">Disabled</MenuItem>
            </TextField>
            <Stack direction="row" spacing={1} justifyContent="flex-end">
              <Button variant="outlined" onClick={() => resetMutation.mutate()}>Generate reset link</Button>
              <Button variant="contained" onClick={() => mutation.mutate()} disabled={mutation.isPending}>Save user</Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </SettingsPage>
  );
}
