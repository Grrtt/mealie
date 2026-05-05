import { useParams } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { ShoppingListEditor } from "@/components/shopping/ShoppingListEditor";
import { useCurrentUser } from "@/features/auth/useCurrentUser";

export function ShoppingListDetailRouteComponent() {
  const { data: user } = useCurrentUser();
  const { id } = useParams({ from: "/shopping-lists/$id" });

  return (
    <AppShell groupSlug={user?.groupSlug ?? "home"} userName={user?.fullName} title="Shopping list">
      <ShoppingListEditor groupSlug={user?.groupSlug ?? "home"} listId={id} />
    </AppShell>
  );
}
