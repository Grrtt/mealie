# Gap Analysis: Webhooks & Event System

## Python (FastAPI) — Full Feature Set

**Service:** `mealie/services/event_bus_service.py`

### Event Types Published
| Event | Trigger |
|---|---|
| `recipe_created` | Recipe POST |
| `recipe_updated` | Recipe PUT/PATCH |
| `recipe_deleted` | Recipe DELETE |
| `shopping_list_created` | List POST |
| `shopping_list_updated` | List PUT, item add/remove |
| `shopping_list_deleted` | List DELETE |
| `mealplan_entry_created` | Meal plan POST |
| `mealplan_entry_updated` | Meal plan PUT |
| `mealplan_entry_deleted` | Meal plan DELETE |
| `user_signup` | User registration |
| `test_message` | Webhook test |

### Event Data Payloads
Each event carries typed data:
- `EventRecipeData` — `recipeSlug`, `recipeId`
- `EventShoppingListData` — `shoppingListId`
- `EventMealPlanData` — `mealplanId`, `date`
- `EventUserSignupData` — `username`, `email`

### Delivery Mechanism
1. Event fired in service layer → `EventBusService.dispatch()`
2. `EventBusService` queries household webhooks filtered by event type
3. HTTP POST to each webhook URL with JSON payload (signed with HMAC if configured)
4. Also fires to Apprise notifiers (push notifications)

### Webhook Management
- `GET /households/webhooks`
- `POST /households/webhooks`
- `GET /households/webhooks/{id}`
- `PUT /households/webhooks/{id}`
- `DELETE /households/webhooks/{id}`
- `POST /households/webhooks/{id}/test` — fire a `test_message` event
- `POST /households/webhooks/rerun` — rerun all webhooks for a given date (useful for meal plan automations)

### Notifiers (Apprise)
- Apprise URL-based push notification to 50+ services (Slack, Discord, Telegram, etc.)
- `POST /households/notifiers/{id}/test`

---

## C# (.NET) — Current State

**Service:** `Mealie.Application/Services/EventNotifierService.cs`
**Controller:** `Mealie.Api/Controllers/Households/WebhooksController.cs`, `EventNotifiersController.cs`

### Implemented
- Webhook CRUD
- Notifier CRUD
- Test webhook endpoint
- `EventNotifierService` class exists

### Missing
- **Event publishing not wired** — services (Recipe, ShoppingList, MealPlan) do not call `EventNotifierService`; events are never actually fired
- **Typed event payloads** — no `EventRecipeData`, `EventShoppingListData` etc. DTOs
- **Webhook rerun** — `POST /webhooks/rerun` not implemented
- **HMAC signing** — webhook payloads not signed
- **Apprise delivery** — unclear if Apprise HTTP calls are implemented
- **Event type filtering** — webhooks store an event type filter but it's unclear if the service applies it when dispatching

---

## Enhancement Opportunities (C#-Specific)

- `IDomainEventDispatcher` — a simple mediator that services call after mutations; implementations: `WebhookDispatcher`, `NotifierDispatcher`, `SignalRDispatcher`
- Typed domain event records: `RecipeCreatedEvent(RecipeId, Slug, GroupId)`, etc. — clean and testable
- HMAC-SHA256 signing: add `X-Mealie-Signature` header to outgoing webhook POSTs; consumers can verify authenticity
- Retry policy on webhook delivery: use `Polly` with exponential backoff (3 retries); log failures to a `webhook_delivery_log` table
- `IHostedService` for async webhook delivery — fire-and-forget dispatch from service layer; queue delivery to a `Channel<T>` consumed by the hosted service
- Webhook rerun: look up meal plan entries for the given date, reconstruct `MealPlanEntryCreated` events, dispatch normally
