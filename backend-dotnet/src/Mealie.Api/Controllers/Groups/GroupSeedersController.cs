using Mealie.Application.Services.Seeder;
using Mealie.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Mealie.Api.Controllers.Groups;

[ApiController]
[Route("api/groups/seeders")]
public class GroupSeedersController(SeedQueue seedQueue, ITenantContext tenantContext)
    : MealieControllerBase(tenantContext)
{
    [HttpPost("foods")]
    public async Task<IActionResult> SeedFoods([FromBody] SeederConfig? config, CancellationToken ct)
    {
        await seedQueue.Writer.WriteAsync(new SeedJobRequest(CurrentGroupId, config?.Locale ?? "en-US", SeedType.Foods),
            ct);
        return Ok(new { detail = "Seeding Successful" });
    }

    [HttpPost("units")]
    public async Task<IActionResult> SeedUnits([FromBody] SeederConfig? config, CancellationToken ct)
    {
        await seedQueue.Writer.WriteAsync(new SeedJobRequest(CurrentGroupId, config?.Locale ?? "en-US", SeedType.Units),
            ct);
        return Ok(new { detail = "Seeding Successful" });
    }

    [HttpPost("labels")]
    public async Task<IActionResult> SeedLabels([FromBody] SeederConfig? config, CancellationToken ct)
    {
        await seedQueue.Writer.WriteAsync(
            new SeedJobRequest(CurrentGroupId, config?.Locale ?? "en-US", SeedType.Labels), ct);
        return Ok(new { detail = "Seeding Successful" });
    }

    public record SeederConfig(string Locale = "en-US");
}
