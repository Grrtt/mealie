using Mealie.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace Mealie.Infrastructure.Admin;

public class BackupService(AppSettings settings, ILogger<BackupService> logger) : IBackupService
{
    private string BackupDir => Path.Combine(settings.DataDir, "backups");

    public async Task<string> CreateBackupAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(BackupDir);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
        var fileName = $"mealie_backup_{timestamp}.zip";
        var filePath = Path.Combine(BackupDir, fileName);

        logger.LogInformation("Creating backup: {FileName}", fileName);

        await Task.Run(() =>
        {
            using var archive = ZipFile.Open(filePath, ZipArchiveMode.Create);

            var dataDir = settings.DataDir;
            if (!Directory.Exists(dataDir)) return;

            foreach (var file in Directory.EnumerateFiles(dataDir, "*", SearchOption.AllDirectories))
            {
                // Skip backups directory to avoid recursive backups
                if (file.StartsWith(BackupDir, StringComparison.OrdinalIgnoreCase)) continue;

                var relativePath = Path.GetRelativePath(dataDir, file);
                archive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
            }
        }, ct);

        logger.LogInformation("Backup created: {FileName}", fileName);
        return fileName;
    }

    public Task<IList<BackupInfo>> ListBackupsAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(BackupDir);

        var files = Directory.EnumerateFiles(BackupDir, "*.zip")
            .Select(f => new BackupInfo
            {
                FileName = Path.GetFileName(f),
                CreatedAt = File.GetCreationTimeUtc(f),
                SizeBytes = new FileInfo(f).Length
            })
            .OrderByDescending(b => b.CreatedAt)
            .ToList();

        return Task.FromResult<IList<BackupInfo>>(files);
    }

    public Task RestoreBackupAsync(string fileName, CancellationToken ct = default)
    {
        var filePath = Path.Combine(BackupDir, Path.GetFileName(fileName));
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Backup not found: {fileName}");

        ZipFile.ExtractToDirectory(filePath, settings.DataDir, overwriteFiles: true);
        logger.LogInformation("Backup restored: {FileName}", fileName);
        return Task.CompletedTask;
    }

    public Task DeleteBackupAsync(string fileName, CancellationToken ct = default)
    {
        var filePath = Path.Combine(BackupDir, Path.GetFileName(fileName));
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            logger.LogInformation("Backup deleted: {FileName}", fileName);
        }
        return Task.CompletedTask;
    }
}
