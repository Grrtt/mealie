# Tasks: Admin-Configurable Default Ingredient Parser

**Feature**: `002-admin-default-parser`  
**Input**: Design documents from `specs/002-admin-default-parser/`  
**Prerequisites**: plan.md ✅ · spec.md ✅ · research.md ✅ · data-model.md ✅ · contracts/api-contracts.md ✅ · quickstart.md ✅

**Tech stack**: Python 3.12 · FastAPI · SQLAlchemy 2 · Alembic · Pydantic v2 · `cryptography` (Fernet) · Vue 3 / Nuxt 4 / TypeScript / Vuetify 3

**Tests**: Not included — not requested in the feature specification.

---

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel with other [P]-marked tasks in the same phase (operates on different files or is independently complete)
- **[Story]**: Maps to a user story from spec.md (US1–US4)
- Exact file paths included in every task description

---

## Phase 1: Setup (DB Models & Migrations)

**Purpose**: Create the two new tables and their SQLAlchemy models. These are the lowest-level dependency — nothing else can be built until the schema exists. No user story label — these are structural prerequisites for all stories.

- [X] T001 Create `AiConfigurationModel` SQLAlchemy ORM model with all columns (`id`, `name`, `provider_type`, `encrypted_api_key`, `base_url`, `default_model`, `is_active`, `enable_image_services`, `enable_transcription_services`, `created_at`, `updated_at`), `ix_ai_configurations_is_active` index, and `BaseMixins` inheritance in `mealie/db/models/server/ai_configuration.py`
- [X] T002 Create `SiteSettingsModel` singleton SQLAlchemy ORM model with columns (`id`, `default_parser` VARCHAR DEFAULT `'nlp'`, `created_at`, `updated_at`) and `BaseMixins` inheritance in `mealie/db/models/server/site_settings.py`
- [X] T003 [P] Export `AiConfigurationModel` and `SiteSettingsModel` from `mealie/db/models/server/__init__.py` so Alembic autogenerate picks them up
- [X] T004 Create Alembic migration `add_ai_configurations_table` (creates `ai_configurations` table with all columns and the `is_active` index) in `mealie/alembic/versions/` following existing date-prefixed naming (`YYYY-MM-DD-HH.MM.SS_<hash>_add_ai_configurations_table.py`)
- [X] T005 Create Alembic migration `add_site_settings_table` (creates `site_settings` table and seeds exactly one row with `default_parser = 'nlp'` via `op.execute(INSERT ...)`) in `mealie/alembic/versions/` following existing date-prefixed naming

**Checkpoint**: Both tables exist in the DB schema and are importable by the application layer.

---

## Phase 2: Foundational (Schema · Encryption · Repositories)

**Purpose**: All application-layer building blocks that every user story route and service requires. No user story label — these are shared infrastructure that blocks all story phases.

**⚠️ CRITICAL**: No user story phase can begin until this phase is complete.

- [X] T006 [P] Create Pydantic v2 schemas `AiConfigurationCreate`, `AiConfigurationUpdate`, `AiConfigurationOut`, `SiteSettingsOut`, and `SiteSettingsUpdate` (exact shapes per `data-model.md` — `AiConfigurationOut` exposes `has_api_key: bool` and `masked_api_key: str | None`, never plaintext; `SiteSettingsOut` includes `default_parser_unavailable: bool` and `legacy_env_vars_detected: bool`) in `mealie/schema/admin/ai_configuration.py`
- [X] T007 [P] Implement Fernet API key utilities (encrypt, decrypt, mask) inside `mealie/services/openai/openai.py`: derive 32-byte Fernet key from `hashlib.sha256(settings.SECRET.encode()).digest()[:32]`; `mask_api_key()` returns `"sk-...••••"` preview; never expose plaintext in any serialised schema
- [X] T008 [P] Create `RepositoryAiConfiguration` with `get_all()`, `get_by_id()`, `create()` (encrypts key), `update()` (handles `None`=keep / `""`=clear / string=replace key), `delete()` (resets site default to `"nlp"` if active), and `activate(id)` (single transaction: all `is_active=False`, this one `True`, update `site_settings.default_parser`) in `mealie/repos/repository_ai_configuration.py`
- [X] T009 [P] Create `RepositorySiteSettings` with `get_or_create()` (guarantees exactly one row, seeded `default_parser="nlp"`), `get()`, and `update(default_parser)` (validates value is `"nlp"`, `"brute"`, or a UUID matching an existing `ai_configurations.id`; clears all `is_active` when built-in is chosen) in `mealie/repos/repository_site_settings.py`
- [X] T010 Register `RepositoryAiConfiguration` and `RepositorySiteSettings` in `mealie/repos/repository_factory.py` following the existing repo registration pattern
- [X] T011 Update `RegisteredParser` StrEnum (remove `openai` member — keep only `nlp` and `brute`) and change `parser` field type from `RegisteredParser` to `str | None = None` in both `IngredientRequest` and `IngredientsRequest` in `mealie/schema/recipe/recipe_ingredient.py`

**Checkpoint**: All schemas, encryption helpers, and repositories are available. Parser schema is updated. User story phases may now begin.

---

## Phase 3: User Story 1 — Admin Configures AI Providers (Priority: P1) 🎯 MVP

**Goal**: An admin can add, edit, remove, and activate AI provider configurations through the admin API, each with encrypted credentials, making them available as parser options.

**Independent Test**: Log in as admin → add a provider via `POST /api/admin/ai-configurations` → confirm it appears in `GET /api/admin/ai-configurations` → call `PUT /api/admin/ai-configurations/{id}/activate` → confirm `isActive: true` → delete it → confirm it no longer appears and `GET /api/admin/site-settings` shows `default_parser: "nlp"` and `defaultParserUnavailable: false`.

- [X] T012 [US1] Implement admin AI configurations router with all six endpoints (`GET` list, `POST` create, `GET` by ID, `PUT` update, `DELETE`, `PUT /{id}/activate`) enforcing admin JWT auth and delegating to `RepositoryAiConfiguration`; `POST` and `PUT` accept plaintext `api_key` and store only the encrypted value; no plaintext key ever in any response in `mealie/routes/admin/admin_ai_configurations.py`
- [X] T013 [US1] Register the `admin_ai_configurations` router in `mealie/routes/admin/__init__.py` under the `/api/admin` prefix, following the existing admin router registration pattern
- [X] T014 [P] [US1] Add legacy-env warning banner to `frontend/app/pages/admin/ai-configuration.vue`: fetch `GET /api/admin/site-settings` on page load; if `legacyEnvVarsDetected === true` and no providers are configured, display a Vuetify `v-alert` (warning level) instructing the admin to re-enter credentials via the UI

**Checkpoint**: US1 is fully functional and independently testable. AI providers can be created, viewed, updated, activated, and deleted via the admin API.

---

## Phase 4: User Story 2 — Admin Sets the Site-Wide Default Parser (Priority: P1)

**Goal**: An admin can view and change the site-wide default ingredient parser from the site settings page, choosing from built-in parsers and any configured AI providers.

**Independent Test**: Log in as admin → navigate to Admin → Site Settings → confirm "Ingredient Parsing" section shows current default (NLP) → change to "Brute Force" → save → reload page → confirm "Brute Force" is still selected → `GET /api/admin/site-settings` returns `"defaultParser": "brute"`.

- [X] T015 [US2] Implement admin site settings router with `GET /api/admin/site-settings` (returns `SiteSettingsOut` including `default_parser_unavailable` and `legacy_env_vars_detected` flags) and `PUT /api/admin/site-settings` (validates value, delegates to `RepositorySiteSettings.update()`, rejects unknown UUIDs with 422) in `mealie/routes/admin/admin_site_settings.py`
- [X] T016 [US2] Register the `admin_site_settings` router in `mealie/routes/admin/__init__.py` under the `/api/admin` prefix alongside the existing admin routers
- [X] T017 [P] [US2] Create TypeScript API client class for admin site settings with `getSettings()` → `GET /api/admin/site-settings` and `updateSettings(payload)` → `PUT /api/admin/site-settings` in `frontend/app/lib/api/admin/admin-site-settings.ts`, following the pattern of `frontend/app/lib/api/admin/admin-about.ts`
- [X] T018 [US2] Add an "Ingredient Parsing" section to `frontend/app/pages/admin/site-settings.vue`: fetch current default via `adminSiteSettings.getSettings()` on mount; render a `v-select` populated with `[{ value: "nlp", title: "NLP (default)" }, { value: "brute", title: "Brute Force" }, ...configuredAiProviders]`; show a `v-alert` warning if `defaultParserUnavailable` is true; save via `adminSiteSettings.updateSettings()`

**Checkpoint**: US2 is fully functional. An admin can view and change the site-wide default parser in under 2 minutes with 5 or fewer interactions (SC-001).

---

## Phase 5: User Story 3 — Non-Admin User Cannot Choose a Parser (Priority: P2)

**Goal**: Non-admin users parse ingredients using the admin-configured default — silently, with no parser selector visible. Server enforces this regardless of what the client sends.

**Independent Test**: Log in as a non-admin → open the ingredient parse dialog → confirm no parser selector is visible → trigger parsing → inspect the network request and confirm `parser: null` is sent → confirm the response uses the admin-configured default algorithm.

- [X] T019 [US3] Refactor `OpenAIService.__init__()` in `mealie/services/openai/openai.py` to accept an `AiConfigurationOut` provider object (or equivalent decrypted config dataclass) instead of reading `OPENAI_API_KEY`, `OPENAI_MODEL`, and `OPENAI_BASE_URL` from env vars; retain all existing public method signatures
- [X] T020 [US3] Update the OpenAI ingredient parser in `mealie/services/parser_services/openai/parser.py` to receive its provider config at construction from the caller (passed down from `get_parser()`) rather than reading env vars directly
- [X] T021 [US3] Update `get_parser()` in `mealie/services/parser_services/ingredient_parser.py` to implement the four-step resolution order: (1) non-admin → always use site default ignoring provided value; (2) admin + `None` → use site default; (3) admin + `"nlp"` or `"brute"` → instantiate built-in parser; (4) admin + UUID → look up `AiConfiguration` by ID, decrypt key, instantiate `OpenAIService`, fall back to NLP on lookup failure with a logged warning
- [X] T022 [US3] Enforce role-based parser override in `mealie/routes/parser/ingredient_parser.py`: inspect the requesting user's role on `POST /api/parser/ingredient` and `POST /api/parser/ingredients`; for non-admin callers, replace any provided `parser` value with `None` before passing to the service layer (server-side enforcement regardless of client input)
- [X] T023 [US3] In `frontend/app/components/Domain/Recipe/RecipePage/RecipePageParts/RecipePageParseDialog.vue`, wrap the entire parser selector block in `v-if="isAdmin"` using the existing `useAuthUser()` composable; ensure the parse API call omits the `parser` field entirely (sends `null`) when the user is not an admin

**Checkpoint**: US3 is fully functional. 100% of non-admin parsing uses the admin-configured default (SC-002). No parser selector is visible to non-admin users (FR-007).

---

## Phase 6: User Story 4 — Admin Can Override Parser Per-Session (Priority: P3)

**Goal**: When an admin opens the ingredient parse dialog, they see a parser selector pre-populated with the current site-wide default. They can switch parsers for that session without altering the site-wide setting.

**Independent Test**: Log in as admin → open parse dialog → confirm parser selector is visible and pre-populated with the site default → change to a different parser → parse ingredients → confirm the selected parser was used → navigate to Admin → Site Settings → confirm the site-wide default is unchanged.

- [X] T024 [US4] In `frontend/app/components/Domain/Recipe/RecipePage/RecipePageParts/RecipePageParseDialog.vue`, on dialog open (within the `v-if="isAdmin"` block), fetch `GET /api/admin/site-settings` to get `defaultParser` and `GET /api/admin/ai-configurations` to get the configured provider list; populate a reactive `availableParsers` array as `[{ value: "nlp", title: "NLP" }, { value: "brute", title: "Brute Force" }, ...providers.map(p => ({ value: p.id, title: p.name }))]`; set the selected parser to `defaultParser`
- [X] T025 [US4] Wire the admin parser `v-select` selection to the parse API call in `frontend/app/components/Domain/Recipe/RecipePage/RecipePageParts/RecipePageParseDialog.vue`: pass the selected `parser` value (UUID or built-in key) in the request body; do not call `PUT /api/admin/site-settings` — the per-session choice must never mutate the site-wide default (FR-009 / SC-003)

**Checkpoint**: US4 is fully functional. Admin per-session parser override works without affecting the site-wide setting.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Remove legacy env var support, update AppInfo API to use DB-derived values, and update all remaining OpenAI feature callers to use the DB provider. These touch multiple stories and are final clean-up.

- [ ] T026 Remove the five credential env var fields (`OPENAI_API_KEY`, `OPENAI_MODEL`, `OPENAI_BASE_URL`, `OPENAI_ENABLE_IMAGE_SERVICES`, `OPENAI_ENABLE_TRANSCRIPTION_SERVICES`) from `mealie/core/settings/settings.py`; add startup detection logic that emits a `WARNING`-level log message if any of the removed vars are still present in the environment, instructing the admin to migrate credentials via the admin UI (FR-013 / SC-007 / Assumption 8)
- [ ] T027 [P] Update `AppInfo` Pydantic schema in `mealie/schema/admin/about.py`: change `enable_openai`, `enable_openai_image_services`, `enable_openai_transcription_services`, and `activeAiProviderType` to be computed from the active `AiConfiguration` DB record (no longer sourced from env var settings); keep field names unchanged for frontend backward compatibility
- [ ] T028 [P] Update `/api/app/about` route in `mealie/routes/app/app_about.py` to query `RepositoryAiConfiguration` for the active provider record and compute the four `AppInfo` OpenAI fields from it: `enable_openai = active_config is not None`; `enable_openai_image_services = active_config.enable_image_services`; `enable_openai_transcription_services = active_config.enable_transcription_services`; `activeAiProviderType = active_config.provider_type`
- [ ] T029 Update all non-parser callers of `OpenAIService` (recipe image import, audio transcription, scraper strategies) throughout `mealie/services/` to fetch the active `AiConfiguration` via `RepositoryAiConfiguration.get_active()` and pass the decrypted provider config to `OpenAIService.__init__()` instead of relying on removed env var fields; these callers gate on `active_config is not None` in place of the removed `OPENAI_ENABLED` flag (research.md §5)

**Final Checkpoint**: All quickstart.md scenarios pass. `uv run pytest tests/ -x` succeeds. `cd frontend && npm run typecheck` passes. No `OPENAI_*` credential env var is read anywhere in the codebase (SC-007).

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup          ──────────────────────────────────┐
Phase 2: Foundational   ── depends on Phase 1 ────────────┤ BLOCKS all user stories
Phase 3: US1 (P1)       ── depends on Phase 2 ────────────┤
Phase 4: US2 (P1)       ── depends on Phase 2 ────────────┤ (can run in parallel with Phase 3)
Phase 5: US3 (P2)       ── depends on Phases 2, 3, 4 ────┤
Phase 6: US4 (P3)       ── depends on Phases 3, 4, 5 ────┤
Phase 7: Polish         ── depends on all story phases ───┘
```

### User Story Dependencies

| Story | Depends On | Reason |
|---|---|---|
| US1 (P1) | Phase 2 only | First independent story; no story deps |
| US2 (P1) | Phase 2 only | Can start in parallel with US1; site settings API is independent |
| US3 (P2) | US1 + US2 | Parser enforcement requires both the AI provider repo (US1) and site settings repo (US2) to resolve the default |
| US4 (P3) | US1 + US2 + US3 | Admin dialog needs provider list (US1), site default (US2), and the v-if admin guard (US3) already in place |

### Within Each Phase

- Tasks marked `[P]` within a phase can run in parallel (they touch different files)
- Non-`[P]` tasks within a phase must run sequentially as listed
- Always complete foundational tasks (T006–T011) before beginning any story work

---

## Parallel Execution Examples

### Phase 2 (Foundational) — 4 tasks can run in parallel

```
T006 (schema)    ──────────────────────────────────────────────────────┐
T007 (encryption)─────────────────────────────────────────────────────┤
T008 (repo AI)   ─────────────────────────────────────────────────────┤──▶ T010 (factory) ──▶ T011 (enum)
T009 (repo site) ─────────────────────────────────────────────────────┘
```

### Phase 3 + Phase 4 — US1 and US2 backend routes in parallel

```
T012 (AI config routes)   ──▶ T013 (register)
                                              ──▶ (US3 can start)
T015 (site settings routes)──▶ T016 (register)

T014 (frontend warning)   [P] — independent of T015/T016
T017 (TS client)          [P] — independent of T012/T013
```

---

## Implementation Strategy

### Suggested MVP Scope (Phase 1 + Phase 2 + Phase 3 + Phase 4)

Completing Phases 1–4 delivers:
- AI providers manageable through the admin API ✅
- Site-wide default parser configurable ✅
- Admin warning for legacy env vars ✅

Non-admin enforcement (US3) and admin per-session override (US4) are self-contained follow-on increments that extend without breaking the MVP.

### Delivery Order

1. **Foundation first** (Phases 1–2): enables all parallel work
2. **US1 + US2 in parallel** (Phases 3–4): backend + frontend for both P1 stories
3. **US3** (Phase 5): server enforcement + frontend hiding
4. **US4** (Phase 6): admin dialog enhancements
5. **Polish** (Phase 7): env var removal, AppInfo update, non-parser callers

---

## Task Summary

| Phase | Tasks | Parallelizable | Story |
|---|---|---|---|
| Phase 1: Setup | T001–T005 | T003 | — |
| Phase 2: Foundational | T006–T011 | T006, T007, T008, T009 | — |
| Phase 3: US1 | T012–T014 | T014 | US1 |
| Phase 4: US2 | T015–T018 | T017 | US2 |
| Phase 5: US3 | T019–T023 | — | US3 |
| Phase 6: US4 | T024–T025 | — | US4 |
| Phase 7: Polish | T026–T029 | T027, T028 | — |
| **Total** | **29 tasks** | **9 parallelizable** | **4 stories** |
