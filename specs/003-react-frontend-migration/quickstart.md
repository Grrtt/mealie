# Quickstart: React Frontend Migration Parity

## Goal

Use the current Nuxt frontend as the parity baseline while preparing later implementation tasks for a phased React replacement that preserves backend and deployment expectations.

## 1. Baseline the current application

From the repository root, install dependencies first:

```bash
task setup
```

For the normal split-process developer baseline, run the backend and current frontend in separate terminals:

```bash
task dotnet
task ui
```

For the production-like container baseline, use the existing compose entry point:

```bash
docker compose -f docker-compose.dotnet.yml up --build
```

Key expectations from the current codebase:

- Frontend dev server runs on `http://localhost:3000`
- Backend API runs on `http://localhost:9000`
- Production-like compose entry point is `docker compose -f docker-compose.dotnet.yml up --build`
- Static production build command is `task ui:generate`

## 2. Use the planning artifacts as the migration source of truth

- `plan.md` — architecture, phases, coexistence/cutover, validation strategy
- `research.md` — decisions and tradeoffs already resolved
- `data-model.md` — planning entities for route parity, phases, gaps, and checks
- `contracts/route-parity.yaml` — route/workflow inventory contract
- `contracts/release-gates.yaml` — required evidence before promoting a phase
- `contracts/runtime-invariants.md` — non-negotiable runtime behaviors to preserve

## 3. Baseline checks to perform before implementation begins

1. Confirm the current frontend route inventory from `frontend/app/pages/**/*.vue`.
2. Confirm public/protected/admin route rules from:
   - `frontend/app/middleware/auth-redirect.global.ts`
   - `frontend/app/middleware/admin-only.ts`
3. Confirm auth/session behavior from:
   - `frontend/app/composables/use-auth-backend.ts`
   - `frontend/app/composables/use-mealie-auth.ts`
   - `frontend/app/plugins/axios.ts`
   - `frontend/app/plugins/init-auth.client.ts`
4. Confirm localization/runtime expectations from:
   - `frontend/nuxt.config.ts`
   - `frontend/app/i18n.config.ts`
   - `frontend/app/lang/locales/*`
   - `frontend/app/lang/dateTimeFormats/*`
5. Confirm static deployment expectations from:
   - `frontend/Dockerfile`
   - `frontend/nginx.conf`
   - `docker-compose.dotnet.yml`
   - `Taskfile.yml`

## 4. Future implementation order

When task generation begins, create tasks in this order:

1. Route/workflow inventory and baseline evidence
2. React runtime foundation (routing, auth, i18n, API, theming, validation, shell)
3. Core recipe + planning + shopping workflows
4. Profile/household/group-data workflows
5. Admin workflows and cutover controls
6. Final cutover + legacy retirement

## 5. Verification checkpoints for every migration slice

- Route in scope resolves without dead ends
- Direct navigation/deep links work
- Session continuity is preserved
- Locale smoke tests pass, including RTL where applicable
- Public/shared routes keep working without auth
- No new backend or deployment dependency is introduced unless explicitly approved
- Rollback path remains documented and testable

## 6. Release-variant coexistence workflow

The migration gateway now exposes rollout state through `MEALIE_FRONTEND_RELEASE_VARIANT` plus the `MEALIE_ROUTE_SLICE_*` flags documented in `contracts/route-parity.yaml`.

### Legacy-only baseline

```bash
task ui:release:rollback
curl -sf http://localhost/__release-variant
```

Expected outcome:

- `/__release-variant` reports `legacy-only`
- unsupported or not-yet-approved paths continue to resolve through the Nuxt frontend

### Hybrid cutover for approved React slices

```bash
task ui:release:hybrid
curl -sf http://localhost/__release-variant
curl -I http://localhost/login
curl -I http://localhost/admin
```

Expected outcome:

- `/__release-variant` reports `hybrid`
- `/login` returns `X-Mealie-Frontend-Instance: react`
- `/admin` remains `X-Mealie-Frontend-Instance: legacy` until the final cutover tasks retire that fallback

### React sidecar validation

The React sidecar continues to run on `http://localhost:8080` during coexistence. It exposes the same rollout metadata for direct smoke checks:

```bash
curl -sf http://localhost:8080/__release-variant
```

### Rollback drill

1. Run `task ui:release:hybrid`.
2. Verify an approved route such as `/login` is served from React.
3. Run `task ui:release:rollback`.
4. Re-run the same route checks and confirm the gateway returns to `legacy-only` without touching backend data, users, or auth cookies.
