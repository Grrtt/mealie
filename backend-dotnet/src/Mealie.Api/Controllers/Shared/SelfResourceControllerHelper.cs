using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Shared;

public static class SelfResourceControllerHelper
{
    public static async Task<ActionResult<TResponse>> OkOrNotFoundAsync<TResponse>(
        Func<CancellationToken, Task<TResponse?>> load,
        Func<ActionResult> notFound,
        Func<TResponse, ActionResult<TResponse>> success,
        CancellationToken ct = default)
        where TResponse : class
    {
        var response = await load(ct);
        return response is null ? new ActionResult<TResponse>(notFound()) : success(response);
    }

    public static async Task<IActionResult> SuccessOrNotFoundAsync(
        Func<CancellationToken, Task<bool>> execute,
        Func<IActionResult> notFound,
        Func<IActionResult> success,
        CancellationToken ct = default)
    {
        return await execute(ct) ? success() : notFound();
    }

    public static ActionResult Envelope<TItem>(ControllerBase controller, IList<TItem> items)
    {
        return controller.Ok(new { items, total = items.Count, page = 1, perPage = -1 });
    }
}
