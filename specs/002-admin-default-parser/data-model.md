# Data Model: Admin-Configurable Default Ingredient Parser

**Feature**: `002-admin-default-parser`  
**Phase**: 1 — Design

---

## Entities

### 1. `AiConfiguration` (new)

Represents a single admin-configured external AI service that can be used for ingredient
parsing and other AI-assisted features (image import, transcription).

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `id` | GUID | PK, auto-generated | |
| `name` | `VARCHAR` | NOT NULL | Human-readable label, e.g. "Company OpenAI" |
| `provider_type` | `VARCHAR` | NOT NULL | One of: `openAi`, `azureOpenAi`, `anthropic`, `ollama`, `custom` |
| `encrypted_api_key` | `VARCHAR` | NULLABLE | Fernet-encrypted; `NULL` for providers that don't use one (e.g. local Ollama) |
| `base_url` | `VARCHAR` | NULLABLE | Required for `azureOpenAi`, `ollama`, `custom`; optional for others |
| `default_model` | `VARCHAR` | NULLABLE | Model ID string, e.g. `gpt-4o`; falls back to provider SDK default if NULL |
| `is_active` | `BOOLEAN` | NOT NULL, DEFAULT `false` | Denormalized from `site_settings.default_parser`; at most one row is `true` |
| `enable_image_services` | `BOOLEAN` | NOT NULL, DEFAULT `true` | Gates image-import feature for this provider |
| `enable_transcription_services` | `BOOLEAN` | NOT NULL, DEFAULT `true` | Gates audio-transcription feature for this provider |
| `created_at` | `TIMESTAMP` | | Standard `BaseMixins` field |
| `updated_at` | `TIMESTAMP` | | Standard `BaseMixins` field |

**Table name**: `ai_configurations`  
**SQLAlchemy model location**: `mealie/db/models/server/ai_configuration.py`

**Indexes**:
- `ix_ai_configurations_created_at` on `created_at` (inherited from `SqlAlchemyBase`)
- `ix_ai_configurations_is_active` on `is_active` (for single-row lookups of the active config)

**Business rules**:
- At most one row may have `is_active = true` at a time. Enforced by the service layer.
- `encrypted_api_key` is encrypted with Fernet using a key derived from `settings.SECRET`.
- Deletion of the active configuration triggers `SiteSettings.default_parser` reset to `"nlp"`.

---

### 2. `SiteSettings` (new)

Singleton server-global settings table. Exactly one row exists (seeded by migration; created
lazily by `get_or_create` if missing). New site-wide settings can be added as columns here
without additional tables.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `id` | GUID | PK, auto-generated | |
| `default_parser` | `VARCHAR` | NOT NULL, DEFAULT `'nlp'` | `"nlp"` \| `"brute"` \| `<ai_configuration.id>` |
| `created_at` | `TIMESTAMP` | | Standard |
| `updated_at` | `TIMESTAMP` | | Standard |

**Table name**: `site_settings`  
**SQLAlchemy model location**: `mealie/db/models/server/site_settings.py`

**Business rules**:
- `default_parser` is `"nlp"` when not set or when the referenced AI provider is deleted.
- If `default_parser` contains a UUID, it **must** reference an existing `ai_configurations.id`.
  The service validates this on write; on read, a missing reference falls back to `"nlp"` with
  a logged warning and an admin-visible flag.

---

## Removed / Modified Entities

### `AppInfo` schema (`mealie/schema/admin/about.py` + `mealie/routes/app/app_about.py`)

Fields that were previously computed from env vars now computed from DB state:

| Old field | Change |
|---|---|
| `enable_openai: bool` | Now `True` iff an active `AiConfiguration` exists in DB |
| `enable_openai_image_services: bool` | Now `True` iff active provider has `enable_image_services=True` |
| `enable_openai_transcription_services: bool` | Now `True` iff active provider has `enable_transcription_services=True` |
| `activeAiProviderType: str \| None` | Provider type string of the active AI config; `None` if default is built-in |

*(Field names kept for frontend backward compatibility.)*

---

### `RegisteredParser` enum (`mealie/schema/recipe/recipe_ingredient.py`)

| Change | Detail |
|---|---|
| Remove `openai` member | AI providers are now addressed by UUID, not enum value |
| Keep `nlp` and `brute` | Built-in parsers remain enum-stable |

### `IngredientRequest` / `IngredientsRequest`

| Field | Before | After |
|---|---|---|
| `parser` | `RegisteredParser = RegisteredParser.nlp` | `str \| None = None` — accepts `"nlp"`, `"brute"`, or a UUID string; `None` means "use server default" |

The field is renamed conceptually to a **parser key** rather than an enum; validation logic
moves to `get_parser()` in the service layer.

---

## Pydantic Schemas (new)

### `AiConfigurationOut` (response)

```python
class AiConfigurationOut(MealieModel):
    id: UUID4
    name: str
    provider_type: str  # AiProviderType literal
    has_api_key: bool   # True if encrypted_api_key is non-null
    masked_api_key: str | None  # e.g. "sk-...••••" — never the real key
    base_url: str | None
    default_model: str | None
    is_active: bool
    enable_image_services: bool
    enable_transcription_services: bool
    created_at: datetime | None
```

### `AiConfigurationCreate`

```python
class AiConfigurationCreate(MealieModel):
    name: str
    provider_type: str
    api_key: str | None = None     # plaintext; encrypted before DB write
    base_url: str | None = None
    default_model: str | None = None
    enable_image_services: bool = True
    enable_transcription_services: bool = True
```

### `AiConfigurationUpdate`

```python
class AiConfigurationUpdate(MealieModel):
    name: str | None = None
    api_key: str | None = None   # None = keep existing; "" = clear key
    base_url: str | None = None
    default_model: str | None = None
    enable_image_services: bool | None = None
    enable_transcription_services: bool | None = None
```

### `SiteSettingsOut` / `SiteSettingsUpdate`

```python
class SiteSettingsOut(MealieModel):
    default_parser: str          # "nlp" | "brute" | <uuid>
    default_parser_unavailable: bool  # True if default points to a deleted AI config
    legacy_env_vars_detected: bool    # True if OPENAI_API_KEY env var still set at startup

class SiteSettingsUpdate(MealieModel):
    default_parser: str          # "nlp" | "brute" | <uuid>
```

---

## State Transitions

### Default Parser State Machine

```
        ┌──────────┐
        │   nlp    │◄──── app startup default / fallback
        └────┬─────┘
             │ admin sets default_parser = "brute"
             ▼
        ┌──────────┐
        │  brute   │
        └────┬─────┘
             │ admin activates AI config / sets default_parser = <uuid>
             ▼
        ┌────────────────────────────────────┐
        │  AI Provider (AiConfiguration.id)  │◄─── activate(id) endpoint
        └───────────────────┬────────────────┘
                            │ delete(id) or set to nlp/brute
                            ▼
                       (fallback to nlp)
```

---

## Alembic Migrations

### Migration 1: `add_ai_configurations_table`

Creates `ai_configurations` table with all columns listed above.

### Migration 2: `add_site_settings_table`

Creates `site_settings` table. Seeds one row:
```sql
INSERT INTO site_settings (id, default_parser, created_at, updated_at)
VALUES (gen_random_uuid(), 'nlp', NOW(), NOW());
```
