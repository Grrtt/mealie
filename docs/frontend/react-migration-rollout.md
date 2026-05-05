# React Migration Rollout and Rollback

## Purpose

Ship React as the default SPA while preserving an explicit legacy-only rollback control until the Nuxt retirement window can be closed safely.

## Controls

- `MEALIE_FRONTEND_RELEASE_VARIANT`
  - `legacy-only` — gateway serves only the legacy Nuxt app for rollback drills
  - `hybrid` — gateway proxies approved route slices to the React sidecar and keeps legacy as the fallback
  - `react-default` — gateway serves the shipped React-default route map
- `MEALIE_ROUTE_SLICE_*` — per-slice enable flags for the approved React surfaces listed in `specs/003-react-frontend-migration/contracts/route-parity.yaml`
- `MEALIE_REACT_RELEASE_VARIANT` — build-time/runtime visibility for the direct React sidecar on port `8080`

## Recommended commands

```bash
# Start the shipped React-default baseline
task ui:release:react-default

# Exercise the hybrid coexistence gateway if you need to compare slices explicitly
task ui:release:hybrid

# Revert the gateway to the legacy-only rollback shape
task ui:release:rollback
```

## Runtime verification

The gateway and the React sidecar both expose `GET /__release-variant`.

```bash
curl -sf http://localhost/__release-variant
curl -sf http://localhost:8080/__release-variant
curl -I http://localhost/login
curl -I http://localhost/admin
```

Expected headers:

- The shipped React-default routes return `X-Mealie-Frontend-Instance: react`
- Rollback drills return `X-Mealie-Frontend-Instance: legacy`
- Every gateway response includes `X-Mealie-Release-Target` and `X-Mealie-Route-Slice`

## Rollback

Rollback is still a routing-only operation:

1. Set the gateway variant back to `legacy-only` by running `task ui:release:rollback`.
2. Wait for the frontend container to rebuild and restart.
3. Confirm `curl -sf http://localhost/__release-variant` reports `legacy-only`.
4. Smoke-test the last promoted route and at least one legacy fallback route.

No backend schema changes, user resets, or account migrations are required for rollback.

## Legacy retirement status

The Nuxt frontend is **not retired yet**. `react-default` is now the shipped runtime, but the `legacy-only` rollback variant still depends on the existing `frontend/` nginx + Nuxt asset shape. Full single-SPA consolidation stays deferred until the rollback window is explicitly closed and operators no longer need to route traffic back to the legacy bundle.
