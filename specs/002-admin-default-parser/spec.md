# Feature Specification: Admin-Configurable Default Ingredient Parser

**Feature Branch**: `002-admin-default-parser`  
**Created**: 2026-05-01  
**Status**: Draft  
**Input**: User description: "i need the admin to be able to select the parsing algorithm that is the default when importing a recipe. anyone who is not an admin does not get the ability to choose. the admin needs the ability to configure the parse for the application and can even still choose the parser to use when importing a recipe, but everyone who is a regular user simply chooses to import a recipe. it should be able to work with any of the supported AI models"

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Admin Configures AI Providers (Priority: P1)

An admin wants to use an AI model to parse ingredients. They navigate to the admin settings, find an "AI Providers" section, and add a provider by supplying the necessary credentials (e.g., API key, model name, and base URL for compatible services). Multiple providers can be configured. Each configured provider then becomes available as a parser option across the application. The admin can also remove or disable a provider.

**Why this priority**: This is the foundation that makes AI-based parsing available to choose as a default. Without it, the parser selection (P2) cannot include AI options.

**Independent Test**: Can be fully tested by adding an AI provider in admin settings, confirming it appears as an available parser choice, then removing it and confirming it disappears from the parser options.

**Acceptance Scenarios**:

1. **Given** the admin is on the AI providers settings page, **When** they add a new provider with valid credentials, **Then** that provider appears as an available parser option throughout the application.
2. **Given** the admin has configured a provider, **When** they remove or disable it, **Then** it no longer appears as a selectable parser option.
3. **Given** the admin supplies a base URL in addition to an API key, **When** the provider is saved, **Then** the application uses that base URL for all requests to that provider, enabling compatibility with OpenAI-compatible third-party services.
4. **Given** the admin submits provider credentials, **When** the form is saved, **Then** the API key is stored securely and not returned in plaintext in any subsequent API response.

---

### User Story 2 — Admin Sets the Site-Wide Default Parser (Priority: P1)

An admin wants to control which ingredient parsing algorithm the entire site uses by default when users import recipes. They navigate to the admin settings, find a "Default Ingredient Parser" option, choose from the available algorithms (built-in parsers and any configured AI providers), and save. From that point forward, all ingredient parsing operations use that algorithm unless overridden by the admin during their own session.

**Why this priority**: This is the core administrative capability being requested. Without it the feature has no value.

**Independent Test**: Can be fully tested by an admin visiting site settings, changing the default parser, saving, then importing a recipe and confirming the correct algorithm is used — delivers complete value independently.

**Acceptance Scenarios**:

1. **Given** the admin is on the site settings page, **When** they view the "Ingredient Parsing" section, **Then** they see a selector showing the currently active default parser.
2. **Given** the admin selects a different parser and saves, **When** any user next triggers ingredient parsing, **Then** the selected algorithm is applied.
3. **Given** no default has been set yet, **When** the setting is displayed, **Then** the NLP parser is shown as the pre-selected default.
4. **Given** no AI providers have been configured, **When** the admin views the parser options, **Then** only the built-in parsers (NLP and brute force) are available for selection.
5. **Given** one or more AI providers have been configured, **When** the admin views the parser options, **Then** each configured AI provider appears as an additional selectable option alongside the built-in parsers.

---

### User Story 3 — Non-Admin User Cannot Choose a Parser (Priority: P2)

A non-admin user imports a recipe and triggers ingredient parsing. Unlike the admin, they do not see a parser selector — the ingredient parse dialog runs using the admin-configured default silently, without any parser selection control being visible.

**Why this priority**: This is the access-control half of the feature. It prevents non-admin users from bypassing the admin's choice, which is the explicit requirement.

**Independent Test**: Can be fully tested by logging in as a non-admin user, opening the ingredient parse dialog, and confirming the parser selector is absent and the admin's configured default is applied.

**Acceptance Scenarios**:

1. **Given** a non-admin user opens the ingredient parse dialog, **When** the dialog renders, **Then** no parser selection control is visible.
2. **Given** a non-admin user triggers ingredient parsing, **When** the parsing request is made, **Then** the server applies the admin-configured default parser.
3. **Given** a non-admin user previously stored a local parser preference, **When** they trigger parsing after this feature is deployed, **Then** their stored preference is ignored and the admin default is used.
4. **Given** no admin default has been set, **When** a non-admin user parses ingredients, **Then** the NLP parser (system default) is used.

---

### User Story 4 — Admin Can Still Override Parser Per-Session (Priority: P3)

An admin is editing a recipe and wants to test a different parser for a specific ingredient set. Because they are an admin, the parser selector remains visible in the ingredient parse dialog, allowing them to change it on-the-fly without altering the site-wide default.

**Why this priority**: The admin workflow should not be degraded. Per-session selection is a quality-of-life feature for admins that also supports testing and troubleshooting.

**Independent Test**: Can be tested independently by logging in as an admin, opening the parse dialog, confirming the parser selector is visible, selecting a different parser, and confirming it is used for that parsing operation only — without altering the site-wide setting.

**Acceptance Scenarios**:

1. **Given** an admin opens the ingredient parse dialog, **When** the dialog renders, **Then** the parser selector is visible and pre-populated with the current site-wide default.
2. **Given** an admin changes the parser in the dialog and parses, **When** the parsing completes, **Then** the algorithm used matches what the admin selected in the dialog.
3. **Given** an admin changes the parser in the dialog, **When** the site-wide default setting is later viewed, **Then** the site-wide default is unchanged.

---

### Edge Cases

- What happens when an admin removes an AI provider that was set as the site-wide default parser? The system falls back to NLP and surfaces a visible warning in admin settings indicating the configured default is no longer available.
- What happens if a non-admin's previously stored local parser preference conflicts with the admin default? The admin default takes precedence and the stored preference is ignored.
- What happens when the admin default setting is missing or corrupted? The system falls back to the NLP parser as the hardcoded safe default.
- What happens when an admin saves the same parser that is already the default? The operation succeeds silently with no side effects.
- What happens when an admin configures a provider with an invalid API key? The provider is saved but flagged as unverified; a validation/test action should be available to confirm connectivity before the provider is used in production parsing.

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The admin settings area MUST include a section for managing AI providers, where each provider can be added, edited, and removed.
- **FR-002**: Each AI provider configuration MUST accept at minimum: an API key, a model name, and an optional base URL to support OpenAI-compatible third-party services.
- **FR-003**: Configured AI providers MUST become available as selectable parser options throughout the application alongside the built-in parsers.
- **FR-004**: The admin settings area MUST include a control for selecting the site-wide default ingredient parsing algorithm from all currently available parsers (built-in and configured AI providers).
- **FR-005**: The selected default parser MUST be persisted as a server-side site setting, not as a per-user preference.
- **FR-006**: When a non-admin user triggers ingredient parsing, the system MUST use the admin-configured default parser.
- **FR-007**: The parser selection control MUST NOT be visible to non-admin users in any ingredient parsing interface.
- **FR-008**: When an admin user triggers ingredient parsing, the parser selection control MUST remain visible and functional, pre-populated with the current site-wide default.
- **FR-009**: Changes made by an admin to the parser within the parse dialog (per-session override) MUST NOT alter the site-wide default setting.
- **FR-010**: The system MUST fall back to the NLP parser if the site-wide default is not set or if the configured parser is no longer available (e.g., the AI provider was removed).
- **FR-011**: The admin settings page MUST display a warning when the currently configured default parser is unavailable.
- **FR-012**: API keys for AI providers MUST be stored securely and MUST NOT be returned in plaintext in any API response after initial submission.
- **FR-013**: AI provider configuration MUST be managed exclusively through the admin UI; environment variables MUST NOT be used to configure AI providers. Any previously supported environment variables for AI provider configuration (e.g., API keys, model names) MUST be removed.
- **FR-014**: Environment variables MUST remain the configuration mechanism for built-in, non-AI parsers only (e.g., any server-side tuning for the NLP or brute-force parsers).

### Key Entities

- **AI Provider**: An admin-configured external service used for AI-assisted ingredient parsing. Defined by credentials (API key), a model identifier, and an optional base URL for OpenAI-compatible endpoints. Multiple providers can be configured simultaneously.
- **Available Parser**: A parsing option that can be selected as the site-wide default or used per-session by an admin. Consists of the built-in parsers (NLP and brute force) plus any configured and active AI providers.
- **Site Setting — Default Parser**: A server-persisted setting identifying which available parser to use when no per-session override is active. Defaults to NLP when unset.
- **User Role**: Determines whether a user sees the parser selection control. Admin users see and can use the selector; non-admin users do not.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An admin can add an AI provider and set it as the default parser in under 2 minutes, with no more than 5 interactions required.
- **SC-002**: 100% of ingredient parsing operations performed by non-admin users use the admin-configured default parser, with zero exposure of a parser selection control.
- **SC-003**: Admin-initiated ingredient parsing continues to work identically to the current experience, with the parser selector still visible and functional.
- **SC-004**: When the configured default parser is unavailable, the system always falls back to NLP without any parsing failure or error surfaced to end users.
- **SC-005**: The site-wide default parser setting survives application restarts and is consistent across all users and sessions.
- **SC-007**: After this feature ships, no AI provider functionality is configurable via environment variables; all such configuration goes through the admin UI.

---

## Assumptions

- The NLP parser is always available and is the appropriate fallback when no admin default is configured or when the configured parser becomes unavailable.
- "Admin" refers to users with the existing admin role in Mealie; no new role or permission level is introduced.
- The site-wide default parser setting is a single global value shared across all groups and households; group-level or household-level defaults are out of scope.
- Multiple AI providers can be configured, but only one parser (built-in or AI provider) is designated as the site-wide default at any given time.
- The per-user local parser preference (currently stored in browser local storage) becomes irrelevant for non-admin users after this feature ships; the preference value may remain in storage but must be ignored in favour of the admin default.
- Admins editing their own per-session parser choice in the parse dialog do not affect the site-wide default; this is intentional.
- The ingredient parse dialog is the only surface where the parser selector currently exists; any future surfaces must also respect this access-control rule.
- Existing environment variables used to configure AI providers (e.g., `OPENAI_API_KEY`, `OPENAI_MODEL`, `OPENAI_BASE_URL`, and related settings) are removed as part of this feature. Any existing OpenAI configuration set via those variables will need to be re-entered through the admin UI. A migration notice or upgrade warning SHOULD be provided to alert administrators.
- There is existing implementation for AI provider configuration and the OpenAI service layer in the codebase that can be updated and extended to satisfy these requirements rather than built from scratch.
- Built-in parsers (NLP and brute force) do not require credentials and remain unconditionally available regardless of any admin UI configuration.
