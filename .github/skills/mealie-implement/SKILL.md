---
name: mealie-implement
description: Implementation completion checklist for Mealie's C# backend migration. Use when implementing features, fixing bugs, or making any code changes to verify the build compiles and Docker containers rebuild successfully before declaring work done.
allowed-tools: shell
---

# Mealie Implementation Completion Requirements

Before declaring any implementation task **done**, you MUST verify both the C# backend and the Nuxt frontend compile successfully, then rebuild both Docker containers to confirm the full stack works.

## Step 1 — Compile the C# Backend

Run from the repo root:

```bash
cd backend-dotnet && dotnet build Mealie.slnx -p:AllowMissingPrunePackageData=true
```

**How to interpret output:**
- ✅ Pass: output ends with `Build succeeded.` and `0 Error(s)`
- ❌ Fail: any line containing `error CS` (e.g., `error CS0246: The type or namespace name 'Foo' could not be found`) — these are real compiler errors that MUST be fixed before continuing
- ⚠️ Ignore: `MSB3492` and other MSBuild cache warnings — these are harmless

Do not proceed to Step 2 until `0 Error(s)` is confirmed.

## Step 2 — Compile the Nuxt Frontend

Run from the repo root:

```bash
cd frontend && yarn build
```

Or using the task runner:

```bash
task ui:build
```

The build must exit with code 0 and produce no TypeScript or Vite errors. Fix any type errors or missing module errors before continuing.

## Step 3 — Rebuild Both Docker Containers

Once both builds pass, rebuild the full stack Docker images:

```bash
docker compose -f docker-compose.dotnet.yml up --build --detach
```

Wait for both containers to report healthy:
- **mealie_api** — healthcheck hits `http://localhost:9000/healthz`
- **mealie_frontend** — nginx serving on port 80

You can verify with:

```bash
curl -sf http://localhost:9000/healthz && echo "BE OK"
curl -sf http://localhost/login && echo "FE OK"
```

## Summary Checklist

- [ ] `dotnet build` → `Build succeeded. 0 Error(s)`
- [ ] `yarn build` (frontend) → exits 0, no type errors
- [ ] `docker compose ... up --build` → both containers healthy

Only after all three checks pass is the implementation considered **complete**.
