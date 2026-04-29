namespace Mealie.Application.Dtos.Admin;

public class MaintenanceSummary
{
    public string DataDirSize { get; set; } = string.Empty;
    public int CleanableImages { get; set; }
    public int CleanableDirs { get; set; }
}

public class StorageDetails
{
    public string TempDirSize { get; set; } = string.Empty;
    public string BackupsDirSize { get; set; } = string.Empty;
    public string GroupsDirSize { get; set; } = string.Empty;
    public string RecipesDirSize { get; set; } = string.Empty;
    public string UserDirSize { get; set; } = string.Empty;
}

public class CleanResponse
{
    public string Detail { get; set; } = string.Empty;
}

public class LogResponse
{
    public List<string> Lines { get; set; } = [];
}

public class AnalyticsResponse
{
    public int TotalRecipes { get; set; }
    public int TotalUsers { get; set; }
    public int TotalGroups { get; set; }
    public int TotalHouseholds { get; set; }
}

public class DebugOpenAiRequest
{
    public string? TestMessage { get; set; }
}

public class DebugOpenAiResponse
{
    public bool Success { get; set; }
    public string? Response { get; set; }
}

public class PasswordResetTokenRequest
{
    public Guid? UserId { get; set; }
    public string? Email { get; set; }
}

public class PasswordResetTokenResponse
{
    public string Token { get; set; } = string.Empty;
}

public class DockerValidateResponse
{
    public string Message { get; set; } = "ok";
}

public class HouseholdPublicResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int RecipeCount { get; set; }
}

public class EmailTestRequest
{
    public string Email { get; set; } = string.Empty;
}

public class EmailTestResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
