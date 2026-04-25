using Mealie.Application.Dtos.Recipes;
using Mealie.Application.Services.Recipes;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Recipes;

[ApiController]
[Route("api/recipes")]
[Authorize]
public class RecipeCommentsController(
    IRecipeCommentService commentService,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("{slug}/comments")]
    public async Task<ActionResult<IList<CommentResponse>>> GetComments(string slug, CancellationToken ct)
    {
        var comments = await commentService.GetCommentsAsync(slug, ct);
        return Ok(comments);
    }

    [HttpPost("{slug}/comments")]
    public async Task<ActionResult<CommentResponse>> AddComment(
        string slug, [FromBody] CreateCommentRequest request, CancellationToken ct)
    {
        var comment = await commentService.AddCommentAsync(slug, tenantContext.UserId, request, ct);
        if (comment is null) return NotFound(new { detail = "Recipe not found" });
        return Ok(comment);
    }

    [HttpPut("{slug}/comments/{commentId:guid}")]
    public async Task<ActionResult<CommentResponse>> UpdateComment(
        string slug, Guid commentId, [FromBody] UpdateCommentRequest request, CancellationToken ct)
    {
        var comment = await commentService.UpdateCommentAsync(commentId, tenantContext.UserId, request, ct);
        if (comment is null) return NotFound(new { detail = "Comment not found or not owned by user" });
        return Ok(comment);
    }

    [HttpDelete("{slug}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(string slug, Guid commentId, CancellationToken ct)
    {
        var deleted = await commentService.DeleteCommentAsync(commentId, tenantContext.UserId, ct);
        if (!deleted) return NotFound(new { detail = "Comment not found or not owned by user" });
        return NoContent();
    }
}
