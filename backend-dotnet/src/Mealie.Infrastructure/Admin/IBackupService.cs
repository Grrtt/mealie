namespace Mealie.Infrastructure.Admin;

public interface IBackupService
{
    Task<string> CreateBackupAsync(CancellationToken ct = default);
    Task<IList<BackupInfo>> ListBackupsAsync(CancellationToken ct = default);
    Task RestoreBackupAsync(string fileName, CancellationToken ct = default);
    Task DeleteBackupAsync(string fileName, CancellationToken ct = default);
}

public class BackupInfo
{
    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
}
