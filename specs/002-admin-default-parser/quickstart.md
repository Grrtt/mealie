# Quickstart: Admin-Configurable Default Ingredient Parser

**Feature**: `002-admin-default-parser`  
**Branch**: `002-admin-default-parser`

---

## What This Feature Does

- Admins can configure one or more AI providers (OpenAI, Azure OpenAI, Anthropic, Ollama,
  custom) via **Admin > AI Configuration** — credentials stored encrypted in the DB.
- Admins select a **site-wide default ingredient parser** from all available options
  (`nlp`, `brute`, or any configured AI provider) via **Admin > Site Settings**.
- **Non-admin users** no longer see a parser selector — all parsing silently uses the
  admin-configured default.
- **Admin users** retain the parser selector in the ingredient parse dialog and can override
  the default per-session without changing the site-wide setting.
- If the configured default parser becomes unavailable (e.g., the provider was deleted),
  the system falls back to NLP automatically and shows an admin warning.

---

## Running the Development Stack

```bash
# Python backend (from repo root)
uv run python -m mealie

# OR via Docker Compose (recommended for full stack)
docker compose -f docker-compose.dotnet.yml up --build
```

---

## Running Tests

```bash
# Backend unit + integration tests
uv run pytest tests/ -x

# Frontend type-check
cd frontend && npm run typecheck
```

---

## Step-by-Step: Configure an AI Provider

1. Log in as an admin.
2. Navigate to **Admin → AI Configuration**.
3. Click **Add Provider**, fill in:
   - **Name**: any descriptive label
   - **Provider Type**: select your AI service
   - **API Key**: your key (stored encrypted; never returned in plaintext)
   - **Base URL**: required for Azure OpenAI, Ollama, or Custom providers
   - **Default Model**: optional model override (e.g. `gpt-4o`)
4. Click **Create**.

The new provider now appears in the **Admin → Site Settings → Ingredient Parsing** selector.

---

## Step-by-Step: Set the Site-Wide Default Parser

1. Navigate to **Admin → Site Settings**.
2. Scroll to the **Ingredient Parsing** section.
3. Select your preferred parser from the dropdown:
   - **NLP** (built-in, always available)
   - **Brute Force** (built-in, always available)
   - Any configured AI provider by name
4. Click **Save**.

All non-admin ingredient parsing operations now use this parser.

---

## Upgrade Notice: Removed `OPENAI_*` Environment Variables

**If you previously configured AI parsing via environment variables**, those variables are
no longer effective. The following env vars have been removed:

- `OPENAI_API_KEY`
- `OPENAI_MODEL`
- `OPENAI_BASE_URL`
- `OPENAI_ENABLE_IMAGE_SERVICES`
- `OPENAI_ENABLE_TRANSCRIPTION_SERVICES`

**Action required**: Re-enter your AI provider credentials via **Admin → AI Configuration**
after upgrading. A warning banner will appear on the AI Configuration admin page if legacy
env vars are still set but no DB provider has been configured.

The following env vars are **still supported** for server-side tuning:
- `OPENAI_WORKERS`, `OPENAI_REQUEST_TIMEOUT`, `OPENAI_SEND_DATABASE_DATA`,
  `OPENAI_AUDIO_MODEL`, `OPENAI_CUSTOM_PROMPT_DIR`, `OPENAI_CUSTOM_HEADERS`,
  `OPENAI_CUSTOM_PARAMS`

---

## Key Files

| File | Role |
|---|---|
| `mealie/db/models/server/ai_configuration.py` | SQLAlchemy model for AI provider records |
| `mealie/db/models/server/site_settings.py` | SQLAlchemy model for site-wide settings singleton |
| `mealie/schema/admin/ai_configuration.py` | Pydantic schemas (Create / Update / Out) |
| `mealie/routes/admin/admin_ai_configurations.py` | Admin CRUD routes for AI providers |
| `mealie/routes/admin/admin_site_settings.py` | Admin site settings GET/PUT |
| `mealie/services/openai/openai.py` | OpenAI service — updated to accept DB provider config |
| `mealie/services/parser_services/ingredient_parser.py` | Parser dispatcher — updated for dynamic providers |
| `mealie/routes/parser/ingredient_parser.py` | Parser routes — adds role enforcement |
| `frontend/app/pages/admin/ai-configuration.vue` | Admin AI provider management UI (already built) |
| `frontend/app/pages/admin/site-settings.vue` | Admin site settings — add default parser section |
| `frontend/app/components/.../RecipePageParseDialog.vue` | Parse dialog — admin-only selector |
