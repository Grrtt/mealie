# Gap Analysis: Media & Images

## Python (FastAPI) — Full Feature Set

**Routes:** `mealie/routes/media.py`

### Endpoints
- `GET /media/recipes/{recipe_slug}/images/{file_name}` — serve recipe image
- `GET /media/recipes/{recipe_slug}/assets/{file_name}` — serve recipe asset
- `GET /media/users/{user_id}/avatar` — serve user profile picture
- `GET /media/households/{household_id}/favicon` — household favicon

### Image Processing
- On upload: image is resized and converted using `Pillow`
- Three sizes stored per recipe image: `original.webp`, `min-original.webp` (720px), `tiny-original.webp` (100px thumbnail)
- Conversion always targets WebP format
- Profile pictures similarly converted to WebP thumbnail

### Storage Structure
```
data/
  recipes/{recipe_slug}/
    images/
      original.webp
      min-original.webp
      tiny-original.webp
    assets/
      {file_name}
  users/
    {user_id}/
      profile.webp
```

### Token-Based Serving
- Media URLs can be signed with a short-lived token for private households
- Token validated before serving

---

## C# (.NET) — Current State

**Controllers:** `Mealie.Api/Controllers/` (media endpoints may be in Recipes or a dedicated controller)

### Implemented
- Recipe image upload, replace, delete
- Asset upload, delete
- Static file serving likely via `StaticFiles` middleware

### Missing / Uncertain
- **Image resizing / WebP conversion** — no `ImageSharp` or equivalent processing pipeline visible
- **Multiple image sizes** — likely only one size stored
- **User profile pictures** — upload/serve endpoints not visible
- **Household favicons** — not present
- **Token-signed media URLs** — not implemented
- **Consistent storage paths** — unclear if C# follows the same directory structure as Python

---

## Enhancement Opportunities (C#-Specific)

- `SixLabors.ImageSharp` — excellent .NET library for image resizing and WebP conversion; drop-in for Pillow equivalent
- `IImageProcessingService` — on upload: validate MIME type, resize to three sizes (original, 720px, 100px), save as WebP; store to `AppDirectories.RecipeImageDir/{slug}/`
- `IMediaTokenService` — sign media URLs with `HMAC-SHA256` + expiry for private households; validate in a middleware or minimal API endpoint filter
- Serve media via a dedicated minimal API endpoint rather than `StaticFiles` middleware so authorization and token validation can be applied per-request
- Background image optimization: `IHostedService` that processes a queue of newly uploaded images asynchronously so upload response is instant
