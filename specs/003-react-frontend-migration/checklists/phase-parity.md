# Phase Parity Checklist

- [x] Release-variant and route-slice controls are documented in `contracts/route-parity.yaml`.
- [x] The legacy gateway exposes coexistence status via `/__release-variant` and response headers.
- [x] The React sidecar exposes its active release shape via `/__release-variant`.
- [x] Playwright coexistence and rollback smoke specs are present for rollout drills.
- [x] The parity report emits phase evidence to the Playwright output stream.
- [x] Operator rollout and rollback steps are documented in the quickstart and rollout runbook.
