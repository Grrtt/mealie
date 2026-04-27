using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Mealie.Application.Services.IngredientParser;

/// <summary>
/// Singleton service that keeps a Python ingredient-parser-nlp subprocess alive
/// and communicates with it via newline-delimited JSON on stdin/stdout.
/// </summary>
public sealed class IngredientParserService(ILogger<IngredientParserService> logger) : IDisposable
{
    private Process? _process;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _available = true;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed class BridgeResult
    {
        public string? Input { get; set; }
        public string? Food { get; set; }
        public double? Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Note { get; set; }
        public string? Error { get; set; }
    }

    public async Task<IReadOnlyList<ParsedIngredientResult>> ParseBatchAsync(
        IEnumerable<string> ingredients, CancellationToken ct = default)
    {
        var list = ingredients as IReadOnlyList<string> ?? ingredients.ToList();
        if (!_available || list.Count == 0)
            return list.Select(i => new ParsedIngredientResult(i, null, null, null, null)).ToList();

        await _lock.WaitAsync(ct);
        try
        {
            EnsureProcess();
            if (_process is null || _process.HasExited)
            {
                logger.LogWarning("Ingredient parser process not available");
                return list.Select(i => new ParsedIngredientResult(i, null, null, null, null)).ToList();
            }

            var json = JsonSerializer.Serialize(list, JsonOpts);
            await _process.StandardInput.WriteLineAsync(json);
            await _process.StandardInput.FlushAsync(ct);

            var response = await _process.StandardOutput.ReadLineAsync(ct);
            if (response is null)
            {
                logger.LogWarning("Ingredient parser returned null response");
                return list.Select(i => new ParsedIngredientResult(i, null, null, null, null)).ToList();
            }

            var results = JsonSerializer.Deserialize<List<BridgeResult>>(response, JsonOpts) ?? [];
            return results.Select(r => new ParsedIngredientResult(
                r.Input ?? "",
                r.Food,
                r.Quantity,
                r.Unit,
                r.Note
            )).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error communicating with ingredient parser process");
            _process?.Kill();
            _process?.Dispose();
            _process = null;
            return list.Select(i => new ParsedIngredientResult(i, null, null, null, null)).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    private void EnsureProcess()
    {
        if (_process is not null && !_process.HasExited) return;

        var scriptPath = Path.Combine(AppContext.BaseDirectory, "scripts", "ingredient_parser_bridge.py");
        if (!File.Exists(scriptPath))
        {
            logger.LogWarning("Ingredient parser bridge script not found at {Path}", scriptPath);
            _available = false;
            return;
        }

        try
        {
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = $"\"{scriptPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            _process.Start();

            // Wait for the bridge to signal it has finished pre-warming (model loaded).
            // This prevents any one-time init output from polluting our JSON protocol.
            var ready = _process.StandardOutput.ReadLine();
            if (ready != "READY")
            {
                logger.LogWarning("Ingredient parser gave unexpected startup signal: {Signal}", ready);
                _process.Kill();
                _process = null;
                _available = false;
                return;
            }

            logger.LogInformation("Ingredient parser process started and ready (PID {Pid})", _process.Id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to start ingredient parser process — ingredient parsing disabled");
            _available = false;
            _process = null;
        }
    }

    public void Dispose()
    {
        try { _process?.Kill(); } catch { /* ignore */ }
        _process?.Dispose();
        _lock.Dispose();
    }
}
