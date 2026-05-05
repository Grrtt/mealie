# Runtime Invariants Contract

## Purpose

This contract lists the behaviors that the React migration must preserve unless a later approved migration exception says otherwise.

## Backend/API Boundary

- Requests continue to target the existing backend API under `/api`.
- nginx continues to proxy `/api`, `/docs`, `/healthz`, `/swagger`, and `/mcp` to the backend service.
- No frontend phase may require backend-breaking API changes by default.
- `Accept-Language` behavior must continue to reflect the active UI locale on API requests.

## Session and Authorization

- The frontend continues to use the `mealie.access_token` cookie as the active session token.
- Session hydration continues to rely on the existing `/api/users/self` behavior.
- A 401 response still clears the active session and returns the user to `/login`.
- Public routes stay public, including shared/public recipe pages.
- Admin routes continue to require the same admin-only protection behavior.
- Moving between legacy and React surfaces must not itself cause a logout.

## Routing and Entry Points

- Current bookmarked/deep-linkable paths remain valid or receive an approved redirect.
- `/` keeps its auth-aware redirect behavior.
- `/login` continues to handle password login and OIDC callback query parameters when enabled.
- Public/shared recipe routes remain reachable from a cold browser start.
- `SUB_PATH` hosting remains supported for direct browser navigation and asset loading.

## Localization and Presentation

- The current supported locale set remains in scope.
- Locale detection, stored preference behavior, fallback to `en-US`, and date/time formatting remain intact.
- RTL locales must remain usable across migrated surfaces.
- New translation work continues to treat `en-US` as the editable source locale.

## Static Deployment

- The replacement frontend remains a static SPA build compatible with nginx `try_files ... /index.html`.
- The frontend image still exposes port 80.
- The frontend and backend remain compatible with `docker-compose.dotnet.yml`.
- Operator workflows in `Taskfile.yml` remain the baseline development and validation entry points.

## Public Metadata

- Pages that currently set route-level titles or public-sharing metadata must preserve meaningful equivalents.
- Public/shared routes may not regress to blank or generic titles during migration.
