# Implementation Plan: Admin-Configurable Default Ingredient Parser

**Branch**: `002-admin-default-parser` | **Date**: 2026-05-01 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/002-admin-default-parser/spec.md`

---

## Summary

Admins need to configure AI providers through a database-backed admin UI (not environment
variables) and select a site-wide default ingredient parser that all non-admin users are
automatically routed to. Non-admin users lose the parser selector entirely; admin users
retain per-session override capability.

**Technical approach**:
1. Two new DB tables: `ai_configurations` (encrypted credentials, multi-provider) and
   `site_settings` (singleton, holds `default_parser`).
2. New admin REST endpoints: `/api/admin/ai-configurations` (CRUD + activate) and
   `/api/admin/site-settings` (GET/PUT). The frontend admin UI for AI configurations
   already exists and requires no structural changes.
3. `OpenAIService` refactored to accept a DB-fetched provider config instead of env vars;
   `get_parser()` extended to resolve any UUID to an `AiConfiguration`.
4. Parser routes enforce role-based access: non-admins are silently routed to the server
   default.
5. Remove credential env vars (`OPENAI_API_KEY`, `OPENAI_MODEL`, `OPENAI_BASE_URL`,
   `OPENAI_ENABLE_IMAGE_SERVICES`, `OPENAI_ENABLE_TRANSCRIPTION_SERVICES`); emit startup
   warning if legacy vars are detected.
6. Frontend parse dialog updated to be admin-only for the selector; site settings page
   updated with a default-parser control.

---

## Technical Context

**Language/Version**: Python 3.12 (backend), TypeScript / Vue 3 / Nuxt 4 (frontend)  
**Primary Dependencies**: FastAPI, SQLAlchemy 2, Alembic, Pydantic v2, `openai` SDK,
`cryptography` (Fernet), Vuetify 3  
**Storage**: PostgreSQL or SQLite via SQLAlchemy (existing)  
**Testing**: pytest (backend), Vitest + Playwright (frontend)  
**Target Platform**: Linux server (Docker)  
**Project Type**: Full-stack web service  
**Performance Goals**: AI parsing response time unchanged (existing <30 s for AI calls,
<500 ms for NLP/brute); parser resolution overhead < 5 ms (single DB read)  
**Constraints**: API keys encrypted at rest; zero plaintext key in any API response;
single-row site settings fetched on every parse request (cache acceptable)  
**Scale/Scope**: Single-instance deployment; typically < 100 admin users; unlimited
recipes; at most a handful of AI providers configured simultaneously

---

## Constitution Check

*The project constitution is unpopulated (template placeholders only). No custom gates
are defined. Standard Mealie development conventions apply: follow existing SQLAlchemy /
Alembic / FastAPI / Pydantic patterns used elsewhere in the codebase.*

✅ No constitution gate violations.

---

## Project Structure

### Documentation (this feature)

```text
specs/002-admin-default-parser/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── api-contracts.md # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
mealie/
├── db/
│   └── models/
│       └── server/
│           ├── ai_configuration.py     # NEW — AiConfigurationModel
│           ├── site_settings.py        # NEW — SiteSettingsModel (singleton)
│           └── __init__.py             # UPDATED — export new models
├── alembic/
│   └── versions/
│       ├── YYYY-MM-DD_*_add_ai_configurations_table.py   # NEW
│       └── YYYY-MM-DD_*_add_site_settings_table.py       # NEW
├── schema/
│   └── admin/
│       └── ai_configuration.py         # NEW — AiConfigurationOut/Create/Update,
│                                       #        SiteSettingsOut/Update
├── repos/
│   ├── repository_ai_configuration.py  # NEW — RepositoryAiConfiguration
│   ├── repository_site_settings.py     # NEW — RepositorySiteSettings
│   └── repository_factory.py          # UPDATED — register new repos
├── routes/
│   ├── admin/
│   │   ├── admin_ai_configurations.py  # NEW — CRUD + activate endpoint
│   │   ├── admin_site_settings.py      # NEW — GET / PUT site settings
│   │   └── __init__.py                # UPDATED — register new routers
│   └── parser/
│       └── ingredient_parser.py        # UPDATED — role-based parser enforcement
├── services/
│   ├── openai/
│   │   └── openai.py                  # UPDATED — accept DB provider config
│   └── parser_services/
│       ├── ingredient_parser.py        # UPDATED — dynamic UUID→provider resolution
│       └── openai/
│           └── parser.py              # UPDATED — receive provider, not read env vars
└── core/
    └── settings/
        └── settings.py                # UPDATED — remove credential env vars,
                                       #            add startup legacy-var warning

frontend/
└── app/
    ├── pages/
    │   └── admin/
    │       ├── ai-configuration.vue   # MINOR UPDATE — legacy-env warning banner
    │       └── site-settings.vue      # UPDATED — add Default Parser section
    ├── components/
    │   └── Domain/Recipe/RecipePage/RecipePageParts/
    │       └── RecipePageParseDialog.vue  # UPDATED — admin-only selector,
    │                                      #  multi-provider list from admin API
    └── lib/
        └── api/
            └── admin/
                └── admin-site-settings.ts  # NEW — site settings API client

tests/
├── unit/
│   └── services/
│       ├── test_ai_configuration_service.py  # NEW
│       └── test_parser_role_enforcement.py   # NEW
└── integration/
    └── api/
        └── admin/
            ├── test_admin_ai_configurations.py  # NEW
            └── test_admin_site_settings.py      # NEW
```

**Structure Decision**: Web application (existing full-stack). All backend changes follow
the existing `mealie/` Python package layout. Frontend changes are confined to admin pages
and the shared parse dialog component. No new top-level packages.

---

## Complexity Tracking

> *No constitution violations.*  
> *No unjustified complexity introduced.*
