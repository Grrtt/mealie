# Research: C# Backend Consolidation Refactors

**Phase**: Phase 0 — Outline & Research  
**Feature**: `001-csharp-backend-migration`  
**Generated**: 2026-05-03

---

## 1. Organizer CRUD consolidation

**Decision**: Consolidate tags/categories/tools behind shared organizer CRUD internals plus a single slug uniqueness policy used by both create and update flows.

**Rationale**: The controllers and command/query handlers are near-clones, and the current create/update slug behavior is inconsistent. A shared internal organizer path removes duplication without changing route prefixes or DTO types and directly closes the slug-safety gap.

**Alternatives considered**:
- Keep separate handlers and only patch slug updates — rejected because it leaves the main duplication source intact.
- Replace all three controllers with one highly generic public controller — rejected because it increases API-shape risk and makes small route differences harder to preserve.

---

## 2. Meal plan helper consolidation

**Decision**: Split duplicated meal plan helpers into two responsibilities: response mapping/loading and recipe-selection logic.

**Rationale**: The current `MealPlanHelpers` copies bundle data shaping with business selection logic, which makes reuse brittle and obscures behavior that must remain stable in create/update/fill/query flows. Separate helpers make it easier to validate mapping parity independently from random/fill behavior.

**Alternatives considered**:
- Create one larger shared meal-plan utility class — rejected because it preserves the current mixed-responsibility design.
- Leave helpers duplicated and only delete obvious copy/paste blocks — rejected because it does not improve maintainability enough for the migration codebase.

---

## 3. Shopping list helper consolidation

**Decision**: Split shopping list duplication into mapping helpers, item mutation/merge helpers, and recipe-link helpers.

**Rationale**: `ShoppingListMappings` currently mixes read-model shaping with create/update/merge behavior, making the shared intent unclear and increasing regression risk. Separating these concerns supports incremental refactors and targeted validation of merge semantics.

**Alternatives considered**:
- Keep one large shared shopping-list helper — rejected because it would still mix mapping and mutation concerns.
- Refactor queries only — rejected because the main duplication spans both command and query paths.

---

## 4. Food/unit CRUD consolidation

**Decision**: Reuse a shared ingredient-entity CRUD pattern for foods and units, with entity-specific hooks where behavior still differs.

**Rationale**: `FoodService`/`UnitService` and `FoodsController`/`UnitsController` are strongly parallel around pagination, alias mutation, merge behavior, and response mapping. A shared internal CRUD core can remove repetition while keeping food-specific label/event behavior and unit-specific fields explicit.

**Alternatives considered**:
- Merge foods and units into one public API/service type — rejected because the domains still have meaningful differences.
- Defer entirely until after all higher-priority work — rejected because the workstream is medium priority and naturally reuses the organizer consolidation patterns.

---

## 5. Parser provider consolidation

**Decision**: Keep `OpenAiCompatibleParserStrategy` as the core behavior and introduce a config-driven client builder/factory for provider-specific `HttpClient` setup.

**Rationale**: The provider subclasses mainly differ in base URL and auth/header construction. A dedicated builder/factory reduces duplicate wiring, keeps provider differences explicit, and may eliminate current constructor-capture warnings by moving provider setup into a narrower abstraction.

**Alternatives considered**:
- Collapse all providers into one switch-heavy strategy — rejected because provider differences still deserve explicit seams and tests.
- Leave current subclasses as-is — rejected because the duplication is small but repeated and easy to make inconsistent over time.

---

## 6. Tenant self-resource controller consolidation

**Decision**: Introduce a shared self-resource controller pattern for group/household self-get/update/preferences/member/invitation flows while leaving domain-specific endpoints separate.

**Rationale**: `GroupsController` and `HouseholdsController` repeat the same self-resource HTTP flow and not-found handling patterns. A shared base/helper can reduce duplication without forcing unrelated endpoints like migration reports or invitation emails into an artificial abstraction.

**Alternatives considered**:
- Create one monolithic tenant controller — rejected because group and household responsibilities are still distinct.
- Skip controller consolidation and only share application-layer code — rejected because a visible duplication hotspot remains in the controllers themselves.

---

## 7. Sequencing strategy

**Decision**: Use characterization-first, independently shippable workstreams in this order: organizer CRUD, meal plan helpers, shopping list helpers, food/unit CRUD, parser providers, tenant self-resource controllers.

**Rationale**: Organizer CRUD is the cleanest place to prove the consolidation style before applying it to more stateful helper flows. Parser provider work is largely independent and can run in parallel with food/unit consolidation once the shared client-builder shape is agreed.

**Alternatives considered**:
- One large consolidation PR — rejected because the behavior-safety risk is too high.
- Purely priority-based execution without dependency awareness — rejected because later controller/service consolidations can reuse patterns proven earlier.
