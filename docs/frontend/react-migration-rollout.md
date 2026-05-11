# React Frontend Deployment

## Purpose

Mealie now ships a single React frontend from `frontend-react/`. There is no split gateway, route-slice rollout, or Nuxt fallback path left in the active deployment.

## Local runtime

```bash
task ui
curl -I http://localhost:4173/login
curl -I http://localhost:4173/admin
```

Expected outcome:

- both routes are served by the same React app
- direct navigation and deep links stay functional
- backend requests continue to flow through `/api`, `/docs`, `/healthz`, `/swagger`, and `/mcp`

## Production deployment

Azure Pipelines builds:

1. `backend-dotnet/` into the API image
2. `frontend-react/` into the frontend image
3. `docker-compose.yml` plus `docker-compose.azure.yml` for the production host

The production host runs a single frontend container and a single API container. There is no release-variant switching or legacy rollback mode to configure.
