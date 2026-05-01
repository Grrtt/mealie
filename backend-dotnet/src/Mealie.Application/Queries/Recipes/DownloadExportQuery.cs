namespace Mealie.Application.Queries.Recipes;

public record DownloadExportQuery(string FileName) : IQuery<(Stream Stream, string FileName)?>
{
    public Task<(Stream Stream, string FileName)?> ExecuteAsync(IQueryServices services, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(FileName) || FileName.Contains('/') || FileName.Contains('\\') ||
            FileName.Contains(".."))
        {
            return Task.FromResult<(Stream, string)?>(null);
        }

        var filePath = Path.Combine(services.Settings.Value.DataDir, "exports", FileName);
        if (!File.Exists(filePath))
        {
            return Task.FromResult<(Stream, string)?>(null);
        }

        Stream stream = File.OpenRead(filePath);
        return Task.FromResult<(Stream, string)?>((stream, FileName));
    }
}
