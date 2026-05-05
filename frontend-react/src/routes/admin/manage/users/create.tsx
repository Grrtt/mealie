import { useMemo, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createAdminUser, fetchAdminGroups, fetchAdminHouseholds } from "@/features/settings/api";

export function AdminCreateUserRouteComponent() {
  const { data: user } = useCurrentUser();
  const groupsQuery = useQuery({ queryKey: ["admin-groups"], queryFn: fetchAdminGroups });
  const householdsQuery = useQuery({ queryKey: ["admin-households"], queryFn: fetchAdminHouseholds });
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({
    username: "",
    fullName: "",
    email: "",
    password: "",
    group: "",
    household: "",
  });
  const filteredHouseholds = useMemo(
    () => (householdsQuery.data?.items ?? []).filter(item => !form.group || item.groupId === form.group),
    [form.group, householdsQuery.data?.items],
  );
  const mutation = useMutation({
    mutationFn: async () => await createAdminUser({
      username: form.username,
      fullName: form.fullName,
      email: form.email,
      password: form.password,
      group: groupsQuery.data?.items.find(item => item.id === form.group)?.name ?? form.group,
      household: householdsQuery.data?.items.find(item => item.id === form.household)?.name ?? form.household,
      admin: false,
      advanced: true,
    }),
    onSuccess: created => {
      setStatus(`User created: ${created.username ?? created.email}`);
      setError(null);
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to create user.");
    },
  });

  return (
    <SettingsPage user={user} title="Create user" description="Create a new user with group and household assignments.">
      {status ? <Alert severity="success">{status}</Alert> : null}
      {error ? <Alert severity="error">{error}</Alert> : null}
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <TextField label="Username" value={form.username} onChange={event => setForm(current => ({ ...current, username: event.target.value }))} />
            <TextField label="Full name" value={form.fullName} onChange={event => setForm(current => ({ ...current, fullName: event.target.value }))} />
            <TextField label="Email" type="email" value={form.email} onChange={event => setForm(current => ({ ...current, email: event.target.value }))} />
            <TextField label="Password" type="password" value={form.password} onChange={event => setForm(current => ({ ...current, password: event.target.value }))} />
            <TextField select label="Group" value={form.group} onChange={event => setForm(current => ({ ...current, group: event.target.value, household: "" }))}>
              {(groupsQuery.data?.items ?? []).map(item => (
                <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>
              ))}
            </TextField>
            <TextField select label="Household" value={form.household} onChange={event => setForm(current => ({ ...current, household: event.target.value }))}>
              {filteredHouseholds.map(item => (
                <MenuItem key={item.id} value={item.id}>{item.name}</MenuItem>
              ))}
            </TextField>
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => mutation.mutate()} disabled={mutation.isPending}>
                Create user
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </SettingsPage>
  );
}
