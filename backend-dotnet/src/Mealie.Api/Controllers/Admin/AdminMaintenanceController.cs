using Mealie.Application.Dtos.Admin;
using Mealie.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Mealie.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/maintenance")]
[Authorize(Roles = "admin")]
public class AdminMaintenanceController(IOptions<AppSettings> settings) : ControllerBase
{
    private readonly AppSettings _settings = settings.Value;

    [HttpGet]
    public IActionResult GetMaintenance()
    {
        try
        {
            var dataDir = _settings.DataDir;
            var dataDirSize = GetDirectorySizeFormatted(dataDir);
            var cleanableImages = CountCleanableImages(dataDir);
            var cleanableDirs = CountCleanableDirectories(dataDir);

            return Ok(new MaintenanceSummary
            {
                DataDirSize = dataDirSize,
                CleanableImages = cleanableImages,
                CleanableDirs = cleanableDirs
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { detail = $"Error: {ex.Message}" });
        }
    }

    [HttpGet("storage")]
    public IActionResult GetStorage()
    {
        try
        {
            var dataDir = _settings.DataDir;
            var tempDir = Path.Combine(dataDir, "temp");
            var backupsDir = Path.Combine(dataDir, "backups");
            var groupsDir = Path.Combine(dataDir, "groups");
            var recipesDir = Path.Combine(dataDir, "recipes");
            var userDir = Path.Combine(dataDir, "users");

            return Ok(new StorageDetails
            {
                TempDirSize = GetDirectorySizeFormatted(tempDir),
                BackupsDirSize = GetDirectorySizeFormatted(backupsDir),
                GroupsDirSize = GetDirectorySizeFormatted(groupsDir),
                RecipesDirSize = GetDirectorySizeFormatted(recipesDir),
                UserDirSize = GetDirectorySizeFormatted(userDir)
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { detail = $"Error: {ex.Message}" });
        }
    }

    [HttpGet("logs")]
    public IActionResult GetLogs([FromQuery] int lines = 100)
    {
        try
        {
            var logDir = Path.Combine(_settings.DataDir, "logs");
            var logFiles = Directory.Exists(logDir)
                ? Directory.GetFiles(logDir, "*.log", SearchOption.TopDirectoryOnly)
                : [];

            var allLines = new List<string>();
            foreach (var logFile in logFiles.OrderByDescending(f => System.IO.File.GetLastWriteTime(f)))
            {
                try
                {
                    var fileLines = System.IO.File.ReadAllLines(logFile);
                    allLines.AddRange(fileLines);
                }
                catch
                {
                }
            }

            var result = allLines.TakeLast(lines).ToList();
            return Ok(new LogResponse { Lines = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { detail = $"Error: {ex.Message}" });
        }
    }

    [HttpPost("clean/temp")]
    public IActionResult CleanTemp()
    {
        try
        {
            var tempDir = Path.Combine(_settings.DataDir, "temp");
            var deletedCount = 0;

            if (Directory.Exists(tempDir))
            {
                var files = Directory.GetFiles(tempDir);
                foreach (var file in files)
                {
                    try
                    {
                        System.IO.File.Delete(file);
                        deletedCount++;
                    }
                    catch
                    {
                    }
                }
            }

            return Ok(new CleanResponse { Detail = $"{deletedCount} items cleaned" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { detail = $"Error: {ex.Message}" });
        }
    }

    [HttpPost("clean/images")]
    public IActionResult CleanImages()
    {
        try
        {
            var recipesDir = Path.Combine(_settings.DataDir, "recipes");
            var deletedCount = 0;

            if (Directory.Exists(recipesDir))
            {
                var imageFiles = Directory.GetFiles(recipesDir, "*.*", SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                    .Where(f => IsImageFile(f));

                foreach (var file in imageFiles)
                {
                    try
                    {
                        System.IO.File.Delete(file);
                        deletedCount++;
                    }
                    catch
                    {
                    }
                }
            }

            return Ok(new CleanResponse { Detail = $"{deletedCount} items cleaned" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { detail = $"Error: {ex.Message}" });
        }
    }

    [HttpPost("clean/recipe-folders")]
    public IActionResult CleanRecipeFolders()
    {
        try
        {
            var recipesDir = Path.Combine(_settings.DataDir, "recipes");
            var deletedCount = 0;

            if (Directory.Exists(recipesDir))
            {
                var folders = Directory.GetDirectories(recipesDir);
                foreach (var folder in folders)
                {
                    var folderName = Path.GetFileName(folder);
                    if (!Guid.TryParse(folderName, out _))
                    {
                        try
                        {
                            Directory.Delete(folder, true);
                            deletedCount++;
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return Ok(new CleanResponse { Detail = $"{deletedCount} items cleaned" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { detail = $"Error: {ex.Message}" });
        }
    }

    [HttpPost("clean/logs")]
    public IActionResult CleanLogs()
    {
        try
        {
            var logDir = Path.Combine(_settings.DataDir, "logs");
            var deletedCount = 0;

            if (Directory.Exists(logDir))
            {
                var logFiles = Directory.GetFiles(logDir, "*.log");
                foreach (var logFile in logFiles)
                {
                    try
                    {
                        System.IO.File.Delete(logFile);
                        deletedCount++;
                    }
                    catch
                    {
                    }
                }
            }

            return Ok(new CleanResponse { Detail = $"{deletedCount} items cleaned" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { detail = $"Error: {ex.Message}" });
        }
    }

    private static string GetDirectorySizeFormatted(string path)
    {
        if (!Directory.Exists(path))
        {
            return "0 B";
        }

        var size = GetDirectorySize(path);
        return FormatBytes(size);
    }

    private static long GetDirectorySize(string path)
    {
        try
        {
            var info = new DirectoryInfo(path);
            return info.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        }
        catch
        {
            return 0;
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private static int CountCleanableImages(string dataDir)
    {
        try
        {
            var recipesDir = Path.Combine(dataDir, "recipes");
            if (!Directory.Exists(recipesDir))
            {
                return 0;
            }

            return Directory.GetFiles(recipesDir, "*.*", SearchOption.AllDirectories)
                .Count(f => !f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) && IsImageFile(f));
        }
        catch
        {
            return 0;
        }
    }

    private static int CountCleanableDirectories(string dataDir)
    {
        try
        {
            var recipesDir = Path.Combine(dataDir, "recipes");
            if (!Directory.Exists(recipesDir))
            {
                return 0;
            }

            return Directory.GetDirectories(recipesDir)
                .Count(d => !Guid.TryParse(Path.GetFileName(d), out _));
        }
        catch
        {
            return 0;
        }
    }

    private static bool IsImageFile(string filePath)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return imageExtensions.Contains(ext);
    }
}
