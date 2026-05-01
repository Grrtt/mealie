using Mealie.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Api.Controllers.Utility;

[ApiController]
[Route("api/validators")]
[AllowAnonymous]
public class ValidatorsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("user/name")]
    public IActionResult ValidateUsername([FromQuery] string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return Ok(new { valid = true });
        }

        var taken = db.Users.IgnoreQueryFilters()
            .Any(u => u.Username != null && u.Username.ToLower() == name.ToLower());
        return Ok(new { valid = !taken });
    }

    [HttpGet("user/email")]
    public IActionResult ValidateEmail([FromQuery] string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Ok(new { valid = true });
        }

        var taken = db.Users.IgnoreQueryFilters()
            .Any(u => u.Email != null && u.Email.ToLower() == email.ToLower());
        return Ok(new { valid = !taken });
    }

    [HttpGet("group")]
    public IActionResult ValidateGroup([FromQuery] string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return Ok(new { valid = true });
        }

        var taken = db.Groups.IgnoreQueryFilters()
            .Any(g => g.Name.ToLower() == name.ToLower());
        return Ok(new { valid = !taken });
    }

    [HttpGet("household")]
    public IActionResult ValidateHousehold([FromQuery] string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return Ok(new { valid = true });
        }

        var taken = db.Households.IgnoreQueryFilters()
            .Any(h => h.Name.ToLower() == name.ToLower());
        return Ok(new { valid = !taken });
    }

    [HttpGet("recipe")]
    public IActionResult ValidateRecipe([FromQuery] Guid groupId, [FromQuery] string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return Ok(new { valid = true });
        }

        var slug = name.ToLowerInvariant().Replace(" ", "-");
        var taken = db.Recipes.IgnoreQueryFilters()
            .Any(r => r.GroupId == groupId && r.Slug == slug);
        return Ok(new { valid = !taken });
    }
}
