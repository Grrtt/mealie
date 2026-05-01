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

public record DeleteApiKeyCommand(Guid UserId, int KeyId) : IQuery<bool>
{
    public async Task<bool> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        var db = services.Db;
        var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == KeyId && k.UserId == UserId, ct);
        if (key is null) return false;
        db.ApiKeys.Remove(key);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
