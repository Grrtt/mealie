using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Groups;

[ApiController]
[Route("api/groups/seeders")]
[Authorize]
public class GroupSeedersController : ControllerBase
{
    public record SeederConfig(string Locale = "en-US");

    [HttpPost("foods")]
    public IActionResult SeedFoods([FromBody] SeederConfig? config)
        => Ok(new { detail = "Seeding Successful" });

    [HttpPost("units")]
    public IActionResult SeedUnits([FromBody] SeederConfig? config)
        => Ok(new { detail = "Seeding Successful" });

    [HttpPost("labels")]
    public IActionResult SeedLabels([FromBody] SeederConfig? config)
        => Ok(new { detail = "Seeding Successful" });
}
