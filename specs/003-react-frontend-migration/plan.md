# Implementation Plan: React Frontend Migration Parity

**Branch**: `003-migrate-react-frontend` | **Date**: 2026-05-03 | **Spec**: [`specs/003-react-frontend-migration/spec.md`](spec.md)
**Input**: Feature specification from `/specs/003-react-frontend-migration/spec.md`

## Summary

Replace the current Nuxt 4 / Vue 3 static SPA with a React-based static SPA without changing the backend API contract, auth/session behavior, base-path deployment model, nginx SPA serving pattern, or operator workflows in `docker-compose.dotnet.yml` and `Taskfile.yml`. The migration will use a strangler-style coexistence plan: keep the current `frontend/` app as the reference implementation, stand up a separate React app for phased route cutover, inventory every route/workflow before implementation, and require automated parity evidence before each surface is promoted.

## Technical Context

**Language/Version**: TypeScript 5.x on a React SPA, built under Node 24-alpine to match the current frontend container build baseline  
**Primary Dependencies**: React, Vite, TanStack Router, TanStack Query, React Hook Form, Zod, react-i18next, Material UI (RTL-capable component system), existing Mealie backend APIs behind nginx  
**Storage**: Browser-managed client state plus existing backend persistence only; no new frontend-owned durable storage beyond cookies/local storage already used for session, locale, theme, and cache metadata  
**Testing**: Existing `task ui:test` / Vitest baseline, existing Playwright e2e workspace under `tests/e2e`, plus new parity/route smoke suites for coexistence and cutover  
**Target Platform**: Browser-based static SPA served by nginx on port 80, proxied to the existing backend API on port 9000, including deployments behind `SUB_PATH`  
**Project Type**: Web application with parallel legacy and replacement frontends during migration  
**Performance Goals**: Match or improve current perceived responsiveness for primary flows; keep median completion time for top workflows within 10% of baseline and preserve direct-navigation success for 99% of tested routes  
**Constraints**: Preserve `/api`, `/docs`, `/healthz`, `/swagger`, and `/mcp` proxy behavior; preserve `mealie.access_token` cookie/session flow; preserve 42 locales plus RTL behavior; preserve public/shareable recipe access and meaningful page metadata; preserve static `dist/` deployment expectations unless an explicit migration exception is approved  
**Scale/Scope**: 60+ current page routes across auth, group, recipe, household, shopping-list, profile, and admin surfaces; 5 rollout phases; 100% route coverage required before final cutover

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The current `.specify/memory/constitution.md` file is still an unfilled template, so it does not introduce ratified project-specific gates beyond standard repository expectations.

### Pre-Research Gate Result

- **Pass**: Plan preserves the existing backend/API contracts by default.
- **Pass**: Plan keeps static SPA deployment via nginx and existing compose/task workflows as the baseline.
- **Pass**: Plan defines independently testable migration phases and parity evidence requirements.
- **Pass**: No unresolved clarifications remain; open choices were resolved in `research.md`.

### Post-Design Gate Result

- **Pass**: Design artifacts retain the existing auth/session, localization, deployment, and route-entry expectations.
- **Pass**: Any temporary complexity (parallel legacy + React frontends) is justified by rollback safety and phased validation.

## Project Structure

### Documentation (this feature)

```text
specs/003-react-frontend-migration/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── release-gates.yaml
│   ├── route-parity.yaml
│   └── runtime-invariants.md
└── tasks.md
```

### Source Code (repository root)

```text
backend-dotnet/
├── src/
└── docker/

frontend/
├── app/
│   ├── composables/
│   ├── lang/
│   ├── layouts/
│   ├── lib/
│   ├── middleware/
│   ├── pages/
│   └── plugins/
├── server/
├── Dockerfile
├── nginx.conf
├── nuxt.config.ts
└── package.json

tests/
└── e2e/

docker-compose.dotnet.yml
Taskfile.yml
```

**Structure Decision**: Keep the current Nuxt app in `frontend/` as the source-of-truth reference during migration. Build the replacement React SPA in a parallel frontend workspace in a later implementation phase so legacy behavior remains runnable for parity comparison, route-by-route cutover, and rollback. Deployment contracts continue to flow through the existing Docker/nginx entry points until full cutover is complete.

## Architecture Decisions

1. **Separate replacement frontend during coexistence**: do not rewrite `frontend/` in place. A parallel React workspace minimizes cutover risk, keeps the current Nuxt app available for baseline capture, and allows route subtree rollout behind nginx.
2. **Preserve runtime contracts first**: keep the current `/api` proxy, `mealie.access_token` cookie, locale behavior, `SUB_PATH`, and static `dist/` deployment model unchanged unless a migration exception is documented.
3. **Inventory before migration**: treat `frontend/app/pages/**/*.vue`, auth middleware, SEO metadata calls, and major workflow entry points as the route/workflow source of truth for planning and later task slicing.
4. **Typed client-side routing/data layer**: use a typed React routing/data stack to reduce regression risk across the large route surface and many deep links.
5. **Parity evidence gates every phase**: no surface is considered migrated until route availability, auth continuity, workflow completion, locale smoke coverage, and rollback expectations are verified.

## Migration Phases

### Phase 0 — Inventory and Baseline

- Freeze a complete route/workflow inventory from the current Nuxt app.
- Capture auth/public/admin access expectations, SEO metadata usage, and locale/RTL coverage.
- Establish parity baselines for top workflows, screenshots, and route response behavior.

### Phase 1 — React Runtime Foundation

- Stand up the replacement SPA foundation: routing, auth/session adapter, API client wrapper, i18n, theming, validation, shell/layout, and test harness.
- Prove compatibility with nginx static serving, `SUB_PATH`, and existing backend endpoints.
- Keep legacy frontend fully active.

### Phase 2 — Core User Workflow Migration

- Migrate auth entry points, navigation shell, recipe browse/detail/create/import flows, meal plan workflows, and shopping lists.
- Validate deep-link parity and session continuity between migrated and legacy surfaces.

### Phase 3 — Profile, Household, and Organizer/Group Data

- Migrate user profile, favorites, API tokens, household settings, members, webhooks/notifiers, group data pages, reports, and import/migration helper surfaces.
- Resolve non-admin authorization and organizer/manage flows.

### Phase 4 — Admin and Cutover Readiness

- Migrate admin setup, site settings, backups, AI/debug/manage surfaces, and any remaining route gaps.
- Close or formally approve every parity gap.
- Switch default routing to React while keeping rollback to legacy available.

### Phase 5 — Final Cutover and Legacy Retirement

- Retire legacy frontend only after all required routes have a replacement route, redirect, or approved retirement decision.
- Simplify nginx/frontend packaging back to a single SPA once rollback windows close.

## Route & Workflow Inventory Strategy

- Use `frontend/app/pages/**/*.vue` as the starting route census.
- Supplement route files with:
  - `frontend/app/middleware/auth-redirect.global.ts` for public/protected route rules
  - `frontend/app/middleware/admin-only.ts` and page-level middleware for permission constraints
  - `frontend/app/pages/index.vue` and `login.vue` for redirect/default-activity/OIDC behavior
  - `frontend/app/pages/**` `useSeoMeta` usage for title/share-preview requirements
  - key composables and client wrappers (`use-auth-backend.ts`, `use-mealie-auth.ts`, `composables/api/api-client.ts`) for workflow/API expectations
- Record the inventory in `contracts/route-parity.yaml` as the migration source of truth.
- Slice future implementation tasks by **surface + workflow**, not by component file alone.

## Coexistence & Cutover Strategy

- Keep the current nginx/API proxy contract intact.
- During coexistence, serve React for approved route subtrees while sending all other routes to the legacy SPA.
- Use the shared auth cookie and locale preference behavior so users can move between legacy and migrated surfaces without a forced re-login.
- Preserve public recipe/share routes and direct deep links from cold start.
- Gate promotion of a route subtree on parity evidence and documented rollback.
- Keep rollback operationally simple: revert routing to legacy without changing backend data, user accounts, or API behavior.

## Testing & Verification Strategy

- **Route coverage**: every current route gets a replacement mapping, redirect rule, or approved retirement decision.
- **Auth/session**: validate sign-in, logout, session refresh, deep-link redirect-after-login, password reset, self-registration when enabled, and OIDC callback behavior.
- **Workflow parity**: validate representative P1/P2 flows for recipes, shopping lists, meal planning, profile, and admin.
- **Localization**: smoke test every supported locale for sign-in, navigation shell, recipe view, and settings; explicitly validate RTL locales.
- **SEO/public pages**: verify titles/share metadata for public/shared recipe routes and other directly shared pages.
- **Performance**: compare top workflow timings and route stability against the current baseline.
- **Rollback drills**: prove operators can return traffic to the legacy frontend quickly using the documented deployment path.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Temporary dual-frontend architecture | Enables safe coexistence, baseline comparison, phased release, and rollback | In-place rewrite of `frontend/` would remove the working reference app and make partial rollout/rollback much riskier |
