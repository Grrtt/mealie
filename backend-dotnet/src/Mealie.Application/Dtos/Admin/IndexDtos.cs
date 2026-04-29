namespace Mealie.Application.Dtos.Admin;

public record IndexInfoResponse(
    string Name,
    int DocumentCount,
    string DirectoryPath,
    long DirectorySizeBytes
);

public record IndexSearchRequest(string? Query, int MaxResults = 50);

public record IndexSearchResponse(
    string IndexName,
    int TotalHits,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Documents
);
