# Quickstart: React Frontend Migration Parity

## Goal

Use the shipped React frontend as the parity baseline for continued validation of the final single-SPA deployment shape.

## 1. Baseline the current application

From the repository root, install dependencies first:

```bash
task setup
```

For the normal split-process developer baseline, run the backend and React frontend in separate terminals:

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

## 2. Use the planning artifacts as the source of truth

- `plan.md` — architecture, phases, coexistence/cutover, validation strategy
- `research.md` — decisions and tradeoffs already resolved
- `data-model.md` — planning entities for route parity, phases, gaps, and checks
- `contracts/route-parity.yaml` — route/workflow inventory contract
- `contracts/release-gates.yaml` — required evidence before promoting a phase
- `contracts/runtime-invariants.md` — non-negotiable runtime behaviors to preserve

## 3. Baseline checks to perform before validation begins

1. Confirm the current frontend route inventory from `frontend-react/src/routes/**/*.tsx`.
2. Confirm public/protected/admin route rules from:
   - `frontend-react/src/features/auth/routeGuards.ts`
3. Confirm auth/session behavior from:
   - `frontend-react/src/features/auth/session.ts`
   - `frontend-react/src/features/auth/useCurrentUser.ts`
   - `frontend-react/src/lib/api/client.ts`
4. Confirm localization/runtime expectations from:
   - `frontend-react/src/lib/i18n/i18n.ts`
   - `frontend-react/src/lib/i18n/messages/*`
   - `frontend-react/src/lib/i18n/dateTimeFormats/*`
5. Confirm static deployment expectations from:
   - `frontend-react/Dockerfile`
   - `frontend-react/nginx.conf`
   - `docker-compose.yml`
   - `Taskfile.yml`

## 4. Future implementation order

When task generation begins, create tasks in this order:

1. Route/workflow inventory and baseline evidence
2. React runtime foundation (routing, auth, i18n, API, theming, validation, shell)
3. Core recipe + planning + shopping workflows
4. Profile/household/group-data workflows
5. Admin workflows
6. Ongoing React-only deployment validation

## 5. Verification checkpoints for every migration slice

- Route in scope resolves without dead ends
- Direct navigation/deep links work
- Session continuity is preserved
- Locale smoke tests pass, including RTL where applicable
- Public/shared routes keep working without auth
- No new backend or deployment dependency is introduced unless explicitly approved
- React remains the only shipped frontend

## 6. React-only frontend workflow

The migration is complete: the active app ships a single React SPA from `frontend-react/`.

### Local validation

```bash
task ui
curl -I http://localhost:4173/login
curl -I http://localhost:4173/admin
```

Expected outcome:

- both routes are served by the same React app
- direct navigation and deep links stay functional
- no route is redirected to a legacy frontend

### Docker validation

```bash
docker compose up --build --detach
curl -sf http://localhost:9000/healthz
curl -sf http://localhost/login
```

Expected outcome:

- the backend responds on port `9000`
- the frontend responds on port `80`
- the stack runs without any release-variant or route-slice configuration
