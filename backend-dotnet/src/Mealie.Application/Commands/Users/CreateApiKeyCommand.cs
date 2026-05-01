using Mealie.Application.Dtos.Users;
using Mealie.Application.Queries;
using Mealie.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace Mealie.Application.Commands.Users;

public record CreateApiKeyCommand(Guid UserId, string Name) : IQuery<ApiKeyResponse>
{
    public async Task<ApiKeyResponse> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var rawToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var hash = BCrypt.Net.BCrypt.HashPassword(rawToken);
        var apiKey = new ApiKey
        {
            Name = Name, Token = hash, UserId = UserId,
            CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow
        };
        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync(ct);
        return new ApiKeyResponse { Id = apiKey.Id, Name = Name, Token = rawToken };
    }
}