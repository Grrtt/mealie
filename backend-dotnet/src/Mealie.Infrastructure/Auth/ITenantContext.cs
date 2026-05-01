namespace Mealie.Infrastructure.Auth;

public interface ITenantContext
{
    Guid GroupId { get; }
    Guid HouseholdId { get; }
    Guid UserId { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
    void SetContext(Guid groupId, Guid householdId, Guid userId, bool isAdmin);
}

public class TenantContextAccessor : ITenantContext
{
    public Guid GroupId { get; private set; }

    public Guid HouseholdId { get; private set; }

    public Guid UserId { get; private set; }

    public bool IsAdmin { get; private set; }

    public bool IsAuthenticated => UserId != Guid.Empty;

    public void SetContext(Guid groupId, Guid householdId, Guid userId, bool isAdmin)
    {
        GroupId = groupId;
        HouseholdId = householdId;
        UserId = userId;
        IsAdmin = isAdmin;
    }
}
