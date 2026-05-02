# Research: Admin-Configurable Default Ingredient Parser

**Feature**: `002-admin-default-parser`  
**Phase**: 0 — Pre-design research

---

## 1. API Key Storage — Encryption at Rest

**Question**: How should AI provider API keys be stored securely so they are never returned in
plaintext (FR-012) but can still be used by the service layer at runtime?

**Decision**: Use **Fernet symmetric encryption** (from Python's `cryptography` library, already
a transitive dependency via `openai`) with a key derived from the application's existing `SECRET`
environment variable. Encrypted bytes are stored as a URL-safe base64 string in the DB. On read
for display, return only a masked preview (e.g., `sk-...xxxx****`). On read for use, decrypt
in-memory inside the service; never serialize the plaintext key in any Pydantic response schema.

**Rationale**:
- `SECRET` already exists in every Mealie deployment (see `mealie/core/settings/settings.py`).
  Deriving the Fernet key from it (via `hashlib.sha256(secret.encode()).digest()[:32]`) means no
  new secret-management infrastructure is required.
- Fernet is authenticated encryption (AES-128-CBC + HMAC-SHA256), preventing both decryption
  and tampering without the key.
- Masked preview follows the same pattern as `MaskedNoneString` already used for `OIDC_CLIENT_SECRET`.

**Alternatives considered**:
- *Store as plaintext*: Rejected — violates FR-012 and security best practice.
- *Store a hash only*: Rejected — the service needs to use the key at runtime; a hash is
  not reversible.
- *External secrets manager (Vault, AWS Secrets Manager)*: Rejected — too heavyweight for a
  self-hosted application; out of scope.

---

## 2. Dynamic Parser Identifiers

**Question**: How should the `parser` field in `IngredientRequest` / `IngredientsRequest` be
extended to support DB-configured AI providers without breaking the existing `RegisteredParser`
StrEnum API contract?

**Decision**: Change `parser` from a strict `RegisteredParser` enum to **`str | None`** in both
request schemas. Keep `RegisteredParser` as a StrEnum for the two built-in values
(`nlp`, `brute`) only — remove `openai`. A UUID string (v4) in the `parser` field refers to a
specific `AiConfiguration` record. `None` means "use the server-side default".

Resolution order in `get_parser()`:
1. Non-admin user → always resolve to site default, ignore any provided value.
2. Admin + `None` → resolve to site default.
3. Admin + `"nlp"` or `"brute"` → instantiate the matching built-in parser.
4. Admin + UUID string → look up `AiConfiguration` by ID; instantiate `OpenAIService` with
   that provider's credentials; fall back to NLP on lookup failure.

**Rationale**: Supports unlimited AI providers without enum changes per provider, preserves
backward compatibility for callers using `"nlp"` or `"brute"`, and is consistent with how
UUIDs already flow through Mealie's APIs.

**Alternatives considered**:
- *`ai:<uuid>` namespaced prefix*: Rejected — more complex parsing with no added benefit.
- *Separate `ai_provider_id` field alongside `parser`*: Rejected — the `parser` field
  already captures intent; a second field causes ambiguity.
- *Extend StrEnum with one `ai` value*: Rejected — loses the ability to distinguish between
  multiple configured providers in the admin per-session use case.

---

## 3. Site-Wide Default Parser Storage

**Question**: Where should the single, server-global "default parser" setting be persisted?
There is no existing site-level settings table in Mealie.

**Decision**: Create a **new singleton `SiteSettingsModel` table** (`site_settings`) with typed
columns. The table is seeded with one row (via Alembic migration data seeding) and a
`get_or_create` repository helper guarantees exactly one row always exists. Initial value:
`default_parser = "nlp"`.

Schema:
```
site_settings
  id            GUID PK
  default_parser  VARCHAR  DEFAULT 'nlp'
                  -- "nlp" | "brute" | <ai_configuration.id>
  created_at    TIMESTAMP
  updated_at    TIMESTAMP
```

**Rationale**: A dedicated typed model is safer than a generic key-value table (compile-time
schema, no stringly-typed lookups). A singleton pattern (enforced by `get_or_create` at the
repository layer) avoids a separate migration every time a new setting is added — columns can
be added to the table.

**Alternatives considered**:
- *Key-value `app_config` table*: Rejected — no type safety, harder to query in application code.
- *Store in the existing `GroupPreferencesModel`*: Rejected — group preferences are
  per-group; the default parser must be global across all groups (per spec Assumption 3).
- *Environment variable*: Rejected — contradicts FR-005 and FR-013.

---

## 4. `is_active` Flag on `AiConfiguration`

**Question**: Should `AiConfiguration` carry its own `is_active` flag, or should activeness be
derived purely from `SiteSettings.default_parser`?

**Decision**: Store **`is_active: bool`** directly on `AiConfigurationModel` as a DB column
for query efficiency (avoids a join to `site_settings` on every list read), but keep it
**strictly in sync** with `SiteSettings.default_parser` via a service-layer transaction:

- `activate(config_id)` → in one transaction: set all `is_active=False`, set this
  `is_active=True`, update `site_settings.default_parser = config_id`.
- `update_site_settings(default_parser="nlp"|"brute")` → in one transaction: set all
  `is_active=False`, update `site_settings.default_parser`.
- `delete(config_id)` where `is_active=True` → in one transaction: set
  `site_settings.default_parser="nlp"`, log a warning.

**Rationale**: Single-source-of-truth semantics maintained transactionally; `is_active` on the
model is a denormalized cache for fast list responses, which the existing frontend already
expects.

---

## 5. Impact on Non-Parser OpenAI Features (Image Import, Transcription)

**Question**: `OPENAI_ENABLED` gates image import and audio transcription in addition to
parsing. When `OPENAI_API_KEY` env var is removed (FR-013), how do these features remain
functional?

**Decision**: `OPENAI_ENABLED` is replaced with a **DB query**: "is there at least one active
`AiConfiguration` in the database?". Callers of `OpenAIService` outside the parser (recipe
image import, audio transcription, scraper strategies) are updated to use the **active**
`AiConfiguration` from the DB instead of env var settings.

The `enable_openai_image_services` and `enable_openai_transcription_services` flags currently
derived from `OPENAI_ENABLE_IMAGE_SERVICES` / `OPENAI_ENABLE_TRANSCRIPTION_SERVICES` are
**moved to per-provider DB columns** (`enable_image_services: bool`, `enable_transcription_services: bool`)
so they remain configurable per-provider without env vars.

The public `AppInfo.enable_openai`, `enable_openai_image_services`, and
`enable_openai_transcription_services` fields remain but are now computed from the active DB
provider. `activeAiProviderType` (already in the TypeScript types) is populated from
the active provider's `provider_type`.

**Rationale**: Consistent with FR-013 (no env var AI config) while preserving existing feature
behaviour. All OpenAI-dependent features now share the same DB-based provider infrastructure.

**Alternatives considered**:
- *Leave image/transcription on env vars while only moving parsing to DB*: Rejected — directly
  violates FR-013 which removes ALL AI provider env vars, not only parser-related ones.

---

## 6. Frontend: Parser Selector Visibility

**Question**: How should the frontend enforce admin-only parser visibility cleanly?

**Decision**: Use the existing `useAuthUser()` composable / `$auth.user.admin` flag already
available in Nuxt app context. In `RecipePageParseDialog.vue`:

- **Non-admin**: the parser selector block is not rendered (`v-if="isAdmin"`); the parse API
  call omits the `parser` field entirely (sends `null`); the server resolves to its default.
- **Admin**: the selector renders, initially populated from a `GET /api/admin/site-settings`
  call (gives the current default); the admin can change it per-session; on change the API call
  sends the selected parser value.

All configured AI providers are loaded in the admin path via a single
`GET /api/admin/ai-configurations` call on dialog open; the resulting list is mapped into
the `availableParsers` array alongside `nlp` and `brute`.

**Rationale**: Role check is already available client-side; the pattern of using admin API
endpoints inside admin UI flows is established throughout the frontend. The single `GET` on
dialog open avoids stale data without overcomplicating reactivity.

---

## 7. Alembic Migration Strategy

**Question**: What is the correct Alembic migration approach for the two new tables?

**Decision**: Two sequential Alembic migrations:
1. `add_ai_configurations_table` — creates `ai_configurations` table.
2. `add_site_settings_table` — creates `site_settings` table, seeds one row with
   `default_parser = 'nlp'` using `op.execute(INSERT ...)`.

The `SiteSettings` repository's `get_or_create` is a safety net for environments that skip
migration steps or run tests with in-memory DBs.

---

## 8. Upgrade / Migration Notice

**Question**: How should existing users be notified that `OPENAI_*` env vars are no longer
effective?

**Decision**: At application startup, detect the presence of legacy env vars (`OPENAI_API_KEY`,
`OPENAI_BASE_URL`, `OPENAI_MODEL`) and emit a **`WARNING`-level log message** once with
instructions to migrate credentials via the admin UI. Also display a **banner / alert on the
Admin > AI Configuration page** if no providers are configured yet and a log warning was
detected (surfaced via a new `GET /api/admin/ai-configurations/migration-status` endpoint or
included in the site-settings response as a `legacy_env_vars_detected: bool` flag).

**Alternatives considered**:
- *Block startup on detection of legacy vars*: Rejected — too disruptive for users upgrading
  automatically; warning + banner is friendlier.
- *Silent removal with docs-only notice*: Rejected — too likely to cause confusion.
