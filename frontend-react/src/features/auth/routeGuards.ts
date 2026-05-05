import { redirect } from "@tanstack/react-router";
import { getDefaultLandingRoute } from "@/features/auth/defaultLanding";
import { consumeIntendedDestination, saveIntendedDestination } from "@/features/auth/redirectStore";
import { ensureCurrentUser } from "@/features/auth/session";
import type { PrivateUser } from "@/lib/api/contracts";

type RouteLocation = {
  pathname: string;
  searchStr?: string;
  href?: string;
};

function toDestination(location: RouteLocation) {
  return location.href ?? `${location.pathname}${location.searchStr ?? ""}`;
}

export async function requireAuth(location: RouteLocation) {
  const user = await ensureCurrentUser();

  if (!user) {
    const destination = toDestination(location);
    saveIntendedDestination(destination);
    throw redirect({
      href: `/login?redirect=${encodeURIComponent(destination)}`,
    });
  }

  return user;
}

export async function requireAdmin(location: RouteLocation) {
  const user = await requireAuth(location);

  if (!user.admin) {
    throw redirect({
      href: await getDefaultLandingRoute(user),
    });
  }

  return user;
}

async function requirePermission(
  location: RouteLocation,
  predicate: (user: PrivateUser) => boolean,
) {
  const user = await requireAuth(location);

  if (!predicate(user)) {
    throw redirect({
      href: await getDefaultLandingRoute(user),
    });
  }

  return user;
}

export async function requireAdvanced(location: RouteLocation) {
  return await requirePermission(location, user => Boolean(user.advanced));
}

export async function requireGroupManager(location: RouteLocation) {
  return await requirePermission(location, user => Boolean(user.canManage));
}

export async function requireHouseholdManager(location: RouteLocation) {
  return await requirePermission(location, user => Boolean(user.canManageHousehold));
}

export async function requireOrganizer(location: RouteLocation) {
  return await requirePermission(location, user => Boolean(user.canOrganize || user.canManage));
}

export async function redirectAuthenticatedUser() {
  const user = await ensureCurrentUser();

  if (!user) return null;

  throw redirect({
    href: consumeIntendedDestination() ?? (await getDefaultLandingRoute(user)),
  });
}
