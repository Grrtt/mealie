# Mealie Development Guide for AI Agents

## Project Overview

Mealie is managed in this repository as a C#/.NET backend and a React frontend:

- **Backend:** `backend-dotnet/` — ASP.NET Core 10, EF Core, SQL Server/PostgreSQL/SQLite support
- **Frontend:** `frontend-react/` — React 19, TypeScript, Vite, MUI, React Query
- **Production:** the React app is statically built and served by nginx, with API traffic proxied to the C# backend

Do not reintroduce the original Python application. Parser support may still include Python helper code, including
`parser/` and `backend-dotnet/scripts/ingredient_parser_bridge.py`; keep that unless a task explicitly removes or replaces parser support.
The Python-backed NLP parser remains the default built-in parser.

## Backend Architecture

- Controllers live under `backend-dotnet/src/Mealie.Api/Controllers/` and handle HTTP concerns.
- Application services, commands, queries, and DTOs live under `backend-dotnet/src/Mealie.Application/`.
- EF Core entities live under `backend-dotnet/src/Mealie.Domain/`.
- EF Core persistence, auth, search, email, and integrations live under `backend-dotnet/src/Mealie.Infrastructure/`.
- Shared pagination and cross-layer helpers live under `backend-dotnet/src/Mealie.Shared/`.
- The migration utility under `backend-dotnet/tools/Mealie.Migration/` is retained for legacy data imports.

Use existing query, command, service, and mapping patterns. Keep multi-tenant group/household scoping intact.

## Frontend Architecture

- Feature-specific UI lives under `frontend-react/src/components/` and `frontend-react/src/routes/`.
- Shared API helpers live under `frontend-react/src/features/**/api.ts` or `frontend-react/src/lib/api/`.
- Auth/session state lives under `frontend-react/src/features/auth/`.
- Prefer React Query and feature-local hooks/state over duplicated inline fetch logic.
- Only edit `frontend-react/src/lib/i18n/messages/en-US.json` for new translation strings; non-English locales are Crowdin-managed.

## Essential Commands

```bash
task setup          # Install frontend dependencies
task dotnet         # Run the C# backend
task dotnet:build   # Build the C# backend
task dotnet:test    # Run C# backend tests
task ui             # Run the React dev server on port 4173
task ui:build       # Build the React frontend
task ui:check       # Run frontend lint + tests
```

Direct targeted equivalents are also acceptable:

```bash
cd backend-dotnet && dotnet build Mealie.slnx -p:AllowMissingPrunePackageData=true
cd frontend-react && npm run build
```

## Development Practices

- Keep backend changes type-safe and compatible with nullable C#.
- Use EF Core migrations for intentional schema changes.
- Keep frontend TypeScript strict-mode clean.
- Do not manually edit generated API type files under `frontend-react/src/lib/api/types/`.
- Preserve existing Docker and nginx contracts unless the task explicitly changes deployment.
- Do not add or modify tests unless explicitly requested; running existing validation is fine.

## Key Files

- `Taskfile.yml` — common development commands
- `backend-dotnet/Mealie.slnx` — .NET solution file
- `backend-dotnet/src/Mealie.Api/Program.cs` — application wiring and dependency injection
- `backend-dotnet/src/Mealie.Infrastructure/Data/ApplicationDbContext.cs` — EF Core DbContext
- `frontend-react/src/lib/api/client.ts` — shared frontend API client
- `frontend-react/vite.config.ts` — React build/dev configuration

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan
at `specs/003-react-frontend-migration/plan.md`.
<!-- SPECKIT END -->
