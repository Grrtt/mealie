const PUBLIC_ROUTES = [
  "/login",
  "/register",
  "/forgot-password",
  "/reset-password",
];

export default defineNuxtRouteMiddleware((to) => {
  // Allow the index page to handle its own redirect logic
  if (to.path === "/") return;

  // Allow public routes
  if (PUBLIC_ROUTES.some((r) => to.path.startsWith(r))) return;

  // Allow shared recipe pages (no auth required)
  if (to.path.includes("/shared/")) return;

  // Allow admin setup (has its own admin-only middleware)
  if (to.path === "/admin/setup") return;

  const { loggedIn } = useLoggedInState();
  if (!loggedIn.value) {
    return navigateTo("/login");
  }
});
