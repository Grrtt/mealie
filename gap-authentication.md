# Gap Analysis: Authentication & Users

## Python (FastAPI) — Full Feature Set

**Routes:** `mealie/routes/auth/`, `mealie/routes/users/`

### Auth Endpoints
- `POST /auth/token` — username/password login
- `POST /auth/token/long-token` — long-lived API token
- `POST /auth/refresh` — refresh access token
- `GET /auth/logout` — invalidate session
- `GET /oauth/callback` — OAuth/OIDC redirect handler
- `GET /oauth/providers` — list configured OAuth providers

### User Endpoints
- `GET /users` — list users (admin)
- `POST /users` — create user (admin)
- `GET /users/self` — current user profile
- `PUT /users/self` — update profile
- `GET /users/{id}` — get user (admin)
- `PUT /users/{id}` — update user (admin)
- `DELETE /users/{id}` — delete user (admin)
- `PUT /users/password` — change own password
- `POST /users/forgot-password` — initiate password reset
- `POST /users/reset-password` — complete password reset (token)
- `GET /users/api-tokens` — list API tokens
- `POST /users/api-tokens` — create API token
- `DELETE /users/api-tokens/{id}` — revoke API token
- `GET /users/self/ratings` — recipe ratings
- `POST /users/self/ratings/{slug}` — rate recipe
- `GET /users/self/favorites` — favorited recipes
- `POST /users/self/favorites/{slug}` — add favorite
- `DELETE /users/self/favorites/{slug}` — remove favorite
- `GET /users/{id}/image` — profile picture
- `POST /users/{id}/image` — upload profile picture
- `POST /users/register` — self-registration (if enabled)

### Auth Providers
- Local username/password (bcrypt)
- OIDC via `python-jose` + `httpx`
- LDAP via `ldap3`
- Per-user `auth_method` stored in DB

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/Auth/`, `Mealie.Api/Controllers/Users/`

### Implemented
- `POST /auth/token` — login
- `POST /auth/refresh` — token refresh
- `GET /auth/logout`
- API tokens: list, create, revoke
- User self: get, update
- Change own password
- Recipe ratings
- Recipe favorites
- User CRUD (admin) — partially in Admin controller

### Missing
- **OAuth/OIDC callback** — endpoint returns hard-coded "OIDC not configured" error
- **LDAP** — no implementation
- **Forgot-password flow** — no endpoint
- **Password reset with token** — no endpoint
- **User profile pictures** — no upload/serve endpoints
- **User self-registration** — not implemented
- **`GET /oauth/providers`** — not present

---

## Enhancement Opportunities (C#-Specific)

- OIDC via `Microsoft.AspNetCore.Authentication.OpenIdConnect` — well-supported in ASP.NET Core; integrate with existing JWT pipeline
- LDAP via `Novell.Directory.Ldap.NETStandard` or `LdapForNet`
- `IPasswordResetService` — generate a short-lived signed token (HMAC or JWT), store hash in DB, email link; validate on reset
- `IUserImageService` — store profile images in `AppDirectories.UserDir/{userId}/profile.webp`, serve via `/media/users/{id}/image`
- Rate-limit `/auth/token` with `AspNetCoreRateLimit` or ASP.NET Core's built-in rate limiting middleware
- Use `IAuthProviderFactory` to select local/OIDC/LDAP strategy at login time — cleaner than if/else chains
