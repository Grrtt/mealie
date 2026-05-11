import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Checkbox from "@mui/material/Checkbox";
import FormControlLabel from "@mui/material/FormControlLabel";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { fetchHouseholdMembers, updateHouseholdPermissions } from "@/features/settings/api";
import type { UserOut } from "@/lib/api/types/user";

export function HouseholdMembersRouteComponent() {
  const { data: user } = useCurrentUser();
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const membersQuery = useQuery({
    queryKey: ["household-members"],
    queryFn: fetchHouseholdMembers,
  });
  const [members, setMembers] = useState<UserOut[]>([]);

  useEffect(() => {
    setMembers(membersQuery.data?.items ?? []);
  }, [membersQuery.data?.items]);

  const mutation = useMutation({
    mutationFn: async (member: UserOut) => await updateHouseholdPermissions({
      userId: member.id,
      canInvite: Boolean(member.canInvite),
      canManage: Boolean(member.canManage),
      canManageHousehold: Boolean(member.canManageHousehold),
      canOrganize: Boolean(member.canOrganize),
    }),
    onSuccess: async () => {
      setStatus("Permissions updated.");
      setError(null);
      await membersQuery.refetch();
    },
    onError: mutationError => {
      setError(mutationError instanceof Error ? mutationError.message : "Unable to update permissions.");
    },
  });

  const updateMember = (memberId: string, key: keyof UserOut, value: boolean) => {
    setMembers(current => current.map(member => member.id === memberId ? { ...member, [key]: value } : member));
  };

  return (
    <SettingsPage
      user={user}
      title="Household members"
      description="Review and update household-level permissions for invites, organizing, management, and household administration."
    >
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}
      <Stack spacing={2}>
        {members.map(member => {
          const disabled = member.id === user?.id || Boolean(member.admin);
          return (
            <Card key={member.id} variant="outlined">
              <CardContent>
                <Stack spacing={1.5}>
                  <Typography variant="h6">{member.fullName ?? member.username ?? member.email}</Typography>
                  <Typography color="text.secondary">{member.email}</Typography>
                  <Stack direction={{ xs: "column", md: "row" }} spacing={2} flexWrap="wrap" useFlexGap>
                    <FormControlLabel
                      control={(
                        <Checkbox
                          checked={Boolean(member.canManageHousehold)}
                          disabled={disabled}
                          onChange={event => updateMember(member.id, "canManageHousehold", event.target.checked)}
                        />
                      )}
                      label="Manage household"
                    />
                    <FormControlLabel
                      control={(
                        <Checkbox
                          checked={Boolean(member.canManage)}
                          disabled={disabled}
                          onChange={event => updateMember(member.id, "canManage", event.target.checked)}
                        />
                      )}
                      label="Manage group"
                    />
                    <FormControlLabel
                      control={(
                        <Checkbox
                          checked={Boolean(member.canOrganize)}
                          disabled={disabled}
                          onChange={event => updateMember(member.id, "canOrganize", event.target.checked)}
                        />
                      )}
                      label="Organize data"
                    />
                    <FormControlLabel
                      control={(
                        <Checkbox
                          checked={Boolean(member.canInvite)}
                          disabled={disabled}
                          onChange={event => updateMember(member.id, "canInvite", event.target.checked)}
                        />
                      )}
                      label="Invite users"
                    />
                  </Stack>
                  <Stack direction="row" justifyContent="flex-end">
                    <Typography
                      component="button"
                      type="button"
                      style={{ border: 0, background: "none", color: "#1976d2", cursor: "pointer" }}
                      onClick={() => mutation.mutate(member)}
                    >
                      Save member permissions
                    </Typography>
                  </Stack>
                </Stack>
              </CardContent>
            </Card>
          );
        })}
      </Stack>
    </SettingsPage>
  );
}
