# API Contracts: Admin-Configurable Default Ingredient Parser

**Feature**: `002-admin-default-parser`  
**Phase**: 1 — Design

---

## 1. Admin AI Configurations API

**Base path**: `/api/admin/ai-configurations`  
**Auth required**: Admin JWT  
**Existing frontend client**: `frontend/app/lib/api/admin/admin-ai-configurations.ts` ✅ (no client changes needed)  
**Backend**: `mealie/routes/admin/admin_ai_configurations.py` — NEW

---

### `GET /api/admin/ai-configurations`

List all configured AI providers.

**Response `200 OK`**:
```jsonc
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Company OpenAI",
    "providerType": "openAi",
    "hasApiKey": true,
    "maskedApiKey": "sk-...••••1234",
    "baseUrl": null,
    "defaultModel": "gpt-4o",
    "isActive": true,
    "enableImageServices": true,
    "enableTranscriptionServices": true,
    "createdAt": "2026-01-01T00:00:00Z"
  }
]
```

---

### `POST /api/admin/ai-configurations`

Create a new AI provider configuration.

**Request body**:
```jsonc
{
  "name": "Company OpenAI",
  "providerType": "openAi",      // "openAi" | "azureOpenAi" | "anthropic" | "ollama" | "custom"
  "apiKey": "sk-...",            // plaintext; encrypted before storage; nullable
  "baseUrl": null,               // required for azureOpenAi/ollama/custom
  "defaultModel": "gpt-4o",     // nullable
  "enableImageServices": true,   // optional, default true
  "enableTranscriptionServices": true  // optional, default true
}
```

**Response `201 Created`**: Same shape as list item above.

**Errors**:
- `422` — `providerType` is not a known value, or `baseUrl` missing when required for provider type.

---

### `GET /api/admin/ai-configurations/{id}`

Get a single configuration.

**Response `200 OK`**: Same shape as list item.  
**Response `404 Not Found`**: Config does not exist.

---

### `PUT /api/admin/ai-configurations/{id}`

Update a configuration. API key semantics:
- `apiKey: null` → keep the existing key (do not change it).
- `apiKey: ""` (empty string) → clear the key.
- `apiKey: "sk-..."` → replace with new encrypted value.

**Request body**:
```jsonc
{
  "name": "Updated Name",               // nullable — omit or null = no change
  "apiKey": null,                       // null = keep existing; "" = clear; string = replace
  "baseUrl": "https://my.proxy/v1",    // nullable
  "defaultModel": "gpt-4o-mini",       // nullable
  "enableImageServices": true,          // nullable
  "enableTranscriptionServices": false  // nullable
}
```

**Response `200 OK`**: Updated configuration.  
**Response `404 Not Found`**: Config does not exist.

---

### `DELETE /api/admin/ai-configurations/{id}`

Delete a configuration. If the deleted config is currently the site default parser:
- Atomically resets `site_settings.default_parser = "nlp"` and clears `is_active`.

**Response `204 No Content`**  
**Response `404 Not Found`**: Config does not exist.

---

### `PUT /api/admin/ai-configurations/{id}/activate`

Mark this configuration as the active/default AI provider.  
Atomically: sets all others `is_active = false`, sets this `is_active = true`,
updates `site_settings.default_parser = id`.

**Request body**: `{}` (empty)  
**Response `200 OK`**: Updated configuration with `"isActive": true`.  
**Response `404 Not Found`**: Config does not exist.

---

## 2. Admin Site Settings API

**Base path**: `/api/admin/site-settings`  
**Auth required**: Admin JWT  
**Backend**: `mealie/routes/admin/admin_site_settings.py` — NEW  
**Frontend client**: `frontend/app/lib/api/admin/admin-site-settings.ts` — NEW

---

### `GET /api/admin/site-settings`

Retrieve the current site-wide settings.

**Response `200 OK`**:
```jsonc
{
  "defaultParser": "nlp",                   // "nlp" | "brute" | "<ai_config_uuid>"
  "defaultParserUnavailable": false,         // true if default points to a deleted/missing AI config
  "legacyEnvVarsDetected": false             // true if OPENAI_API_KEY env var is still set
}
```

---

### `PUT /api/admin/site-settings`

Update site-wide settings.

**Request body**:
```jsonc
{
  "defaultParser": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  // "nlp" | "brute" | "<ai_configuration_id>"
}
```

**Response `200 OK`**: Updated settings (same shape as GET).

**Errors**:
- `422` — `defaultParser` is a UUID that does not match any existing `ai_configurations` record.
- `422` — `defaultParser` is not `"nlp"`, `"brute"`, or a valid UUID.

**Side effects**:
- If `defaultParser` is a UUID: sets `ai_configurations.is_active = true` for that ID,
  `false` for all others.
- If `defaultParser` is `"nlp"` or `"brute"`: sets `is_active = false` for all AI configs.

---

## 3. Updated Parser API

**Base path**: `/api/parser`  
**Auth required**: User JWT (any role)  
**Backend**: `mealie/routes/parser/ingredient_parser.py` — UPDATED

---

### `POST /api/parser/ingredient`

Parse a single ingredient string.

**Request body** (change: `parser` is now `string | null`):
```jsonc
{
  "ingredient": "2 cups all-purpose flour",
  "parser": null  // null = use server default; "nlp" | "brute" | "<ai_config_uuid>"
                  // non-admin callers: parser value is always ignored; server default is used
}
```

**Response `200 OK`**: `ParsedIngredient` — unchanged.

**Behaviour change**:
- If the requesting user is **not an admin**, the `parser` field is silently overridden with the
  server-side default regardless of what was sent.
- If `parser` is `null` (for any user), the server default is used.

---

### `POST /api/parser/ingredients`

Parse multiple ingredient strings.

**Request body**:
```jsonc
{
  "ingredients": ["2 cups flour", "1 tsp salt"],
  "parser": null  // same semantics as above
}
```

**Response `200 OK`**: `list[ParsedIngredient]` — unchanged.

---

## 4. Updated App Info API

**Path**: `GET /api/app/about`  
**Auth required**: None (public)  
**Backend**: `mealie/routes/app/app_about.py` — UPDATED

Fields now computed from the active `AiConfiguration` DB record instead of env vars. Field
names are **unchanged** for frontend backward compatibility.

**Response additions / changes**:
```jsonc
{
  // ... existing fields unchanged ...
  "enableOpenai": true,            // true iff active AI config exists in DB
  "enableOpenaiImageServices": true,     // true iff active config has enable_image_services=true
  "enableOpenaiTranscriptionServices": false,  // true iff active config has enable_transcription_services=true
  "activeAiProviderType": "openAi" // provider_type of the active config, or null
}
```

---

## 5. Removed Env Var Configuration

The following `AppSettings` fields are **removed** from `mealie/core/settings/settings.py`:

| Removed env var | Replacement |
|---|---|
| `OPENAI_API_KEY` | `AiConfiguration.encrypted_api_key` (DB) |
| `OPENAI_MODEL` | `AiConfiguration.default_model` (DB) |
| `OPENAI_BASE_URL` | `AiConfiguration.base_url` (DB) |
| `OPENAI_ENABLE_IMAGE_SERVICES` | `AiConfiguration.enable_image_services` (DB) |
| `OPENAI_ENABLE_TRANSCRIPTION_SERVICES` | `AiConfiguration.enable_transcription_services` (DB) |

The following env vars are **retained** as runtime tuning only (non-credential, non-AI-provider config):

| Retained env var | Purpose |
|---|---|
| `OPENAI_AUDIO_MODEL` | Audio model identifier for transcription (not per-provider) |
| `OPENAI_WORKERS` | Concurrency for batch AI parsing |
| `OPENAI_REQUEST_TIMEOUT` | HTTP timeout for AI API calls |
| `OPENAI_SEND_DATABASE_DATA` | Whether to include DB data in AI prompts |
| `OPENAI_CUSTOM_PROMPT_DIR` | Path to custom prompt overrides |
| `OPENAI_CUSTOM_HEADERS` | Extra HTTP headers for AI requests |
| `OPENAI_CUSTOM_PARAMS` | Extra HTTP query params for AI requests |
