using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Queries;
using Mealie.Application.Queries.Recipes;
using Mealie.Application.Commands.Recipes;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Recipes;

[ApiController]
[Route("api/comments")]
[Authorize]
public class CommentsController(
    QueryExecutor executor,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CommentResponse>>> GetComments(
        [FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        var comments = await executor.ExecuteAsync(new GetAllCommentsQuery(tenantContext.GroupId), ct);
        var total = comments.Count;
        var items = comments
            .Skip(pagination.Skip)
            .Take(pagination.PerPage)
            .ToList();
        return Ok(new PaginatedResponse<CommentResponse>
        {
            Page = pagination.Page,
            PerPage = pagination.PerPage,
            Total = total,
            TotalPages = (int)Math.Ceiling((double)total / pagination.PerPage),
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CommentResponse>> GetComment(Guid id, CancellationToken ct)
    {
        var comment = await executor.ExecuteAsync(new GetCommentByIdQuery(id), ct);
        if (comment is null) return NotFound();
        return Ok(comment);
    }

    [HttpPost]
    public async Task<ActionResult<CommentResponse>> CreateComment([FromBody] CreateCommentRequestWithRecipe request,
        CancellationToken ct)
    {
        if (request.RecipeId == Guid.Empty)
            return BadRequest(new { detail = "recipeId is required" });

        var comment = await executor.ExecuteAsync(
            new AddCommentByRecipeIdCommand(request.RecipeId, tenantContext.UserId,
                new CreateCommentRequest { Text = request.Text }), ct);
        if (comment is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(comment);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CommentResponse>> UpdateComment(Guid id, [FromBody] UpdateCommentRequest request,
        CancellationToken ct)
    {
        var comment = await executor.ExecuteAsync(new UpdateCommentCommand(id, tenantContext.UserId, request), ct);
        if (comment is null) return NotFound(new { detail = "Comment not found or not owned by user" });
        return Ok(comment);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CommentResponse>> PatchComment(Guid id, [FromBody] UpdateCommentRequest request,
        CancellationToken ct) => await UpdateComment(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteComment(Guid id, CancellationToken ct)
    {
        var deleted = await executor.ExecuteAsync(new DeleteCommentCommand(id, tenantContext.UserId), ct);
        if (!deleted) return NotFound(new { detail = "Comment not found or not owned by user" });
        return NoContent();
    }
}

public class CreateCommentRequestWithRecipe
{
    public Guid RecipeId { get; set; }
    public string Text { get; set; } = string.Empty;
}
