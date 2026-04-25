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
    private Guid _groupId;
    private Guid _householdId;
    private Guid _userId;
    private bool _isAdmin;

    public Guid GroupId => _groupId;
    public Guid HouseholdId => _householdId;
    public Guid UserId => _userId;
    public bool IsAdmin => _isAdmin;
    public bool IsAuthenticated => _userId != Guid.Empty;

    public void SetContext(Guid groupId, Guid householdId, Guid userId, bool isAdmin)
    {
        _groupId = groupId;
        _householdId = householdId;
        _userId = userId;
        _isAdmin = isAdmin;
    }
}
