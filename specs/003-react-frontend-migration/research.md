# Research: React Frontend Migration Parity

## Decision 1: Migrate with a parallel React SPA instead of rewriting `frontend/` in place

**Decision**: Keep the current Nuxt app as the reference implementation during migration and build the replacement SPA in a separate frontend workspace.

**Rationale**: The current codebase already has a complete static SPA deployment pipeline (`frontend/Dockerfile`, `frontend/nginx.conf`) and a large route surface. A parallel app preserves a runnable baseline for parity capture, allows coexistence under nginx, and makes rollback operationally simple.

**Alternatives considered**:
- Rewrite `frontend/` in place — rejected because it removes the live baseline and increases rollback risk.
- Microfrontend embed inside the current app — rejected because it adds host/container complexity without improving route parity or deployment safety.

## Decision 2: Keep the runtime/deployment contract unchanged through most of the migration

**Decision**: Preserve the existing static SPA served by nginx on port 80, with `/api`, `/docs`, `/healthz`, `/swagger`, and `/mcp` proxied to the backend, and preserve `SUB_PATH` support.

**Rationale**: The current production-like path is already encoded in `frontend/nginx.conf`, `frontend/Dockerfile`, `docker-compose.dotnet.yml`, and `Taskfile.yml`. Holding this contract steady isolates frontend migration risk from backend/deployment risk and directly satisfies the spec requirement to preserve existing deployment expectations.

**Alternatives considered**:
- Introduce SSR/Next.js-style serving — rejected because current deployment is a static SPA and SSR would change infrastructure and caching behavior.
- Move frontend serving into the backend container immediately — rejected because the current dotnet compose flow already expects a separate frontend container.

## Decision 3: Treat route inventory as a first-class contract artifact

**Decision**: Use `frontend/app/pages/**/*.vue` plus middleware/redirect logic as the source of truth and publish the inventory in `contracts/route-parity.yaml`.

**Rationale**: The spec requires 100% route coverage before final cutover. The existing Nuxt file-based pages, auth middleware exemptions, SEO metadata usage, and special redirects (root redirect, shared/public routes, OIDC callback, PWA shortcut behavior) define the real parity scope more accurately than a narrative-only checklist.

**Alternatives considered**:
- Only track high-level feature areas — rejected because it would miss deep links and admin/public route edge cases.
- Rely solely on later test discovery — rejected because tasks need an up-front route/workflow source of truth.

## Decision 4: Preserve the existing auth/session semantics exactly

**Decision**: Keep `mealie.access_token` cookie behavior, `/api/users/self` session hydration, 401-to-login handling, password/OIDC flows, and public-route exemptions as runtime invariants.

**Rationale**: The current auth flow is spread across `use-auth-backend.ts`, `use-mealie-auth.ts`, `plugins/axios.ts`, `init-auth.client.ts`, `auth-redirect.global.ts`, `admin-only.ts`, `index.vue`, and `login.vue`. Reusing these semantics allows legacy and React surfaces to coexist without forced reauthentication and preserves deep-link/login outcomes.

**Alternatives considered**:
- Switch to a new auth provider or token storage model — rejected because the spec defaults to backend/API compatibility and safe coexistence.
- Rework authorization boundaries during migration — rejected because it would couple parity work to product/security redesign.

## Decision 5: Reuse current localization assets and preserve locale/RTL behavior

**Decision**: Carry forward the current locale set, locale-detection behavior, date/time formatting assets, and RTL handling as-is, while mapping them into a React-compatible i18n layer.

**Rationale**: The current frontend already maintains 42 locale files plus matching date-time format files, uses `no_prefix` routing, and declares RTL locales in `nuxt.config.ts`. Reusing those assets avoids unnecessary translation churn and keeps locale smoke testing focused on behavior rather than content changes.

**Alternatives considered**:
- Reduce supported locales for the migration — rejected because the spec explicitly keeps current locale scope.
- Change URL strategy to locale prefixes — rejected because it would break current route-entry expectations.

## Decision 6: Keep validation semantics and error messaging parity during migration

**Decision**: Preserve the current validation rules and translated error messages as the initial source of truth, even if the React implementation later wraps them in a different form library.

**Rationale**: The existing frontend uses shared validators in `frontend/app/lib/validators/inputs.ts` and `password.ts`, and user-facing messages are localized. Keeping semantics stable avoids accidental behavior drift on sign-in, profile, recipe import, and admin forms.

**Alternatives considered**:
- Redesign validation rules up front — rejected because it introduces product behavior changes while parity is still incomplete.
- Allow feature teams to redefine validation per form — rejected because it makes parity review inconsistent.

## Decision 7: Slice rollout by route surface, not by technical layer alone

**Decision**: Define migration phases around user-visible surfaces and workflows (auth/navigation, recipe core, planning/shopping, profile/household/group data, admin/cutover) instead of only shared libraries or component categories.

**Rationale**: The spec requires independently testable release slices. Surface-based slicing aligns work with routes users navigate to, lets parity testing follow real workflows, and makes nginx coexistence/cutover simpler because route subtree rollout is easier to reason about than cross-cutting component-only progress.

**Alternatives considered**:
- Migrate all primitives/libs first, then all pages — rejected because it delays user-visible validation.
- Cut over individual components across the whole app — rejected because it complicates routing and rollback.

## Decision 8: Make parity validation an explicit release gate, not a best-effort check

**Decision**: Require route smoke tests, workflow checks, locale/RTL coverage, metadata checks, performance comparisons, and rollback drills before approving each migration phase.

**Rationale**: The feature specification includes measurable cutover criteria and rollback expectations. Capturing those as concrete release gates makes later task generation and acceptance review objective.

**Alternatives considered**:
- Manual exploratory validation only — rejected because it is too easy to miss route gaps.
- Delay parity checks until final cutover — rejected because it concentrates risk at the end of the migration.

## Decision 9: Preserve public/shared recipe access and SEO metadata as separate parity concerns

**Decision**: Treat public/shared recipe pages and route-level metadata (`useSeoMeta`) as dedicated contract items, not incidental page details.

**Rationale**: Shared/public routes must continue to work without auth, and the spec explicitly requires meaningful titles/share previews. The current app already uses route-level SEO metadata in many pages, especially login, recipe, shopping-list, profile, and admin surfaces.

**Alternatives considered**:
- Validate SEO only after full cutover — rejected because public/share flows are part of core acceptance.
- Treat public routes like any other protected route migration — rejected because auth and cold-start behavior differ materially.
