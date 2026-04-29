using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Recipes;
using Mealie.Infrastructure.Auth;
using Mealie.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Recipes;

[ApiController]
[Route("api/comments")]
[Authorize]
public class CommentsController(
    IRecipeCommentService commentService,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CommentResponse>>> GetComments(
        [FromQuery] PaginationParams pagination, CancellationToken ct)
    {
        var comments = await commentService.GetAllCommentsAsync(tenantContext.GroupId, ct);
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
        var comment = await commentService.GetCommentByIdAsync(id, ct);
        if (comment is null) return NotFound();
        return Ok(comment);
    }

    [HttpPost]
    public async Task<ActionResult<CommentResponse>> CreateComment([FromBody] CreateCommentRequestWithRecipe request, CancellationToken ct)
    {
        if (request.RecipeId == Guid.Empty)
        {
            return BadRequest(new { detail = "recipeId is required" });
        }

        var comment = await commentService.AddCommentByRecipeIdAsync(request.RecipeId, tenantContext.UserId, new CreateCommentRequest { Text = request.Text }, ct);
        if (comment is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(comment);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CommentResponse>> UpdateComment(Guid id, [FromBody] UpdateCommentRequest request, CancellationToken ct)
    {
        var comment = await commentService.UpdateCommentAsync(id, tenantContext.UserId, request, ct);
        if (comment is null) return NotFound(new { detail = "Comment not found or not owned by user" });
        return Ok(comment);
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CommentResponse>> PatchComment(Guid id, [FromBody] UpdateCommentRequest request, CancellationToken ct)
        => await UpdateComment(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteComment(Guid id, CancellationToken ct)
    {
        var deleted = await commentService.DeleteCommentAsync(id, tenantContext.UserId, ct);
        if (!deleted) return NotFound(new { detail = "Comment not found or not owned by user" });
        return NoContent();
    }
}

public class CreateCommentRequestWithRecipe
{
    public Guid RecipeId { get; set; }
    public string Text { get; set; } = string.Empty;
}
