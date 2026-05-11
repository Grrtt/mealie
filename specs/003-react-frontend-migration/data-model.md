# Data Model: React Frontend Migration Parity

## Overview

This feature does not introduce new backend persistence. The data model below defines the planning/control entities needed to manage route parity, phased rollout, validation, and cutover while preserving existing Mealie runtime behavior.

## Entities

### 1. Frontend Surface

| Field | Type | Description | Validation |
|---|---|---|---|
| `id` | string | Stable identifier for a major product area (`auth`, `recipes`, `shopping-lists`, `household`, `profile`, `admin`, etc.) | Required, kebab-case |
| `name` | string | Human-readable surface name | Required |
| `routePrefixes` | string[] | Primary route roots owned by the surface | At least one route or explicit redirect source |
| `accessMode` | enum | `public`, `authenticated`, `mixed`, `admin-only` | Required |
| `legacySourcePaths` | string[] | Current Vue/Nuxt page or middleware paths that define behavior | Required |
| `phaseId` | string | Migration phase currently responsible for the surface | Required |
| `status` | enum | `legacy-only`, `foundation-ready`, `pilot-ready`, `cutover-ready`, `react-default`, `retired` | Required |

**Relationships**
- One `FrontendSurface` owns many `RouteEntryPoint`s.
- One `MigrationPhase` includes many `FrontendSurface`s.
- One `FrontendSurface` can have many `UserWorkflow`s and `ParityGap`s.

### 2. Route Entry Point

| Field | Type | Description | Validation |
|---|---|---|---|
| `pathPattern` | string | Canonical route pattern users or links can navigate to | Required; unique |
| `surfaceId` | string | Owning surface | Required |
| `accessRule` | enum | `public`, `protected`, `admin-only`, `redirect-only` | Required |
| `deepLinkCritical` | boolean | Whether direct navigation/bookmarks must be explicitly tested | Required |
| `seoCritical` | boolean | Whether title/share metadata parity is required | Required |
| `legacyPagePath` | string | Existing Nuxt page file or middleware source | Required |
| `reactOutcome` | enum | `native-route`, `legacy-fallback`, `redirect`, `retired` | Required |
| `notes` | string | Edge cases such as OIDC callback or shared recipe behavior | Optional |

**Relationships**
- Many `RouteEntryPoint`s belong to one `FrontendSurface`.
- A `RouteEntryPoint` is covered by one or more `ValidationCheck`s.

### 3. User Workflow

| Field | Type | Description | Validation |
|---|---|---|---|
| `id` | string | Stable workflow identifier | Required, kebab-case |
| `surfaceId` | string | Owning surface | Required |
| `name` | string | User-visible workflow name | Required |
| `entryRoutes` | string[] | Starting routes or redirect entry points | Required |
| `completionSignal` | string | Observable successful end state | Required |
| `requiredApis` | string[] | Backend endpoints/contracts relied upon | Required |
| `localesInScope` | string[] | Locales/RTL variants that require smoke coverage | Required |
| `priority` | enum | `P1`, `P2` | Required |
| `status` | enum | `not-started`, `in-progress`, `parity-proven`, `approved` | Required |

**Relationships**
- One `FrontendSurface` has many `UserWorkflow`s.
- One `UserWorkflow` can accumulate many `ParityGap`s and `ValidationCheck`s.

### 4. Migration Phase

| Field | Type | Description | Validation |
|---|---|---|---|
| `id` | string | Stable phase identifier | Required |
| `name` | string | Human-readable phase label | Required |
| `goal` | string | User-facing purpose of the phase | Required |
| `surfaceIds` | string[] | Surfaces in scope for the phase | Required |
| `deploymentMode` | enum | `react-only` | Required |
| `entryCriteria` | string[] | Conditions to start the phase | Required |
| `exitCriteria` | string[] | Conditions to declare the phase complete | Required |
| `rollbackMode` | string | How operators roll back containers/images without a second frontend runtime | Required |

### 5. Parity Gap

| Field | Type | Description | Validation |
|---|---|---|---|
| `id` | string | Stable parity gap identifier | Required |
| `surfaceId` | string | Affected surface | Required |
| `workflowId` | string | Affected workflow, if any | Optional |
| `routePatterns` | string[] | Affected routes | Required |
| `severity` | enum | `critical`, `high`, `medium`, `low` | Required |
| `type` | enum | `route`, `auth`, `workflow`, `locale`, `rtl`, `metadata`, `performance`, `accessibility`, `deployment` | Required |
| `description` | string | User-visible difference from legacy behavior | Required |
| `temporaryDisposition` | enum | `legacy-fallback`, `blocked`, `approved-exception`, `fixed` | Required |
| `mustResolveBeforeCutover` | boolean | Whether full cutover is blocked | Required |

### 6. Validation Check

| Field | Type | Description | Validation |
|---|---|---|---|
| `id` | string | Stable validation identifier | Required |
| `scopeType` | enum | `route`, `workflow`, `locale`, `phase`, `deployment` | Required |
| `scopeId` | string | Route/workflow/phase identifier | Required |
| `checkType` | enum | `smoke`, `integration`, `visual`, `performance`, `accessibility`, `rollback` | Required |
| `method` | string | How the check runs (Vitest, Playwright, manual drill, docker compose, etc.) | Required |
| `evidence` | string | Link or description of required output | Required |
| `status` | enum | `pending`, `passing`, `failing`, `waived` | Required |

### 7. Localization Set

| Field | Type | Description | Validation |
|---|---|---|---|
| `localeCode` | string | Locale identifier (for example `en-US`, `ar-SA`) | Required; must match current frontend locale files |
| `direction` | enum | `ltr`, `rtl` | Required |
| `messageAsset` | string | Current locale message source file | Required |
| `dateTimeAsset` | string | Current datetime format source file | Required |
| `smokeRoutes` | string[] | Minimum required route checks per locale | Required |
| `status` | enum | `legacy-verified`, `react-verified`, `blocked` | Required |

## State Transitions

### Frontend Surface Status

```text
legacy-only
  -> foundation-ready
  -> pilot-ready
  -> cutover-ready
  -> react-default
  -> retired
```

Rules:
- `foundation-ready` requires routing/auth/i18n/API shell support.
- `pilot-ready` requires workflow-specific parity checks for routes in scope.
- `cutover-ready` requires release-gate evidence and documented rollback.
- `react-default` requires the phase to route production traffic to React by default.

### Parity Gap Status via `temporaryDisposition`

```text
legacy-fallback -> fixed
blocked -> fixed
blocked -> approved-exception
legacy-fallback -> approved-exception
```

Rules:
- No `critical` parity gap may remain unresolved at full cutover.
- `approved-exception` requires explicit scope/deployment approval per the spec.

### Validation Check Status

```text
pending -> passing
pending -> failing
failing -> passing
pending -> waived
```

Rules:
- `waived` requires documented justification and may not be used for critical route/auth/rollback checks.

## Derived Views Needed for Task Generation

- **Surface-by-phase matrix**: which routes/workflows are in each migration slice.
- **Critical deep-link list**: routes requiring direct-navigation and post-login return testing.
- **Locale coverage matrix**: per-locale smoke status, with explicit RTL tracking.
- **Parity gap register**: open blocking differences before each release.
