using System.Data.Common;
using Dapper;
using Mealie.Migration.SourceReaders;
using Mealie.Migration.TargetWriters;
using Microsoft.Data.Sqlite;
using Npgsql;
using Serilog;

namespace Mealie.Migration;

public class MigrationRunner(
    string sourceConnectionString,
    string targetConnectionString,
    string sourceEngine,
    string targetEngine,
    bool dryRun)
{
    private readonly ILogger _log = Log.ForContext<MigrationRunner>();

    public async Task<int> RunAsync()
    {
        using var sourceConn = CreateConnection(sourceEngine, sourceConnectionString);
        using var targetConn = CreateConnection(targetEngine, targetConnectionString);

        await sourceConn.OpenAsync();
        await targetConn.OpenAsync();

        // Idempotency check (T102)
        await EnsureMigrationLogTableAsync(targetConn);
        var alreadyMigrated = await IsAlreadyMigratedAsync(targetConn);
        if (alreadyMigrated)
        {
            _log.Warning("Database has already been migrated. Aborting to prevent data duplication.");
            Console.WriteLine("Database has already been migrated. Aborting to prevent data duplication.");
            return 1;
        }

        var report = new MigrationReport();

        // Execute tiers in dependency order (T111)
        await ExecuteTierAsync("Tier1 (Core)", () => new Tier1Reader(sourceConn).ReadAllAsync(), targetConn, report);
        await ExecuteTierAsync("Tier2 (Organizers)", () => new Tier2Reader(sourceConn).ReadAllAsync(), targetConn,
            report);
        await ExecuteTierAsync("Tier3 (Recipes)", () => new Tier3Reader(sourceConn).ReadAllAsync(), targetConn, report);
        await ExecuteTierAsync("Tier4 (Junction)", () => new Tier4Reader(sourceConn).ReadAllAsync(), targetConn,
            report);

        if (!dryRun)
        {
            await MarkMigrationCompleteAsync(targetConn);
        }

        ReportGenerator.PrintReport(report);
        return 0;
    }

    private async Task ExecuteTierAsync(string tierName, Func<Task<MigrationData>> readFn, DbConnection targetConn,
        MigrationReport report)
    {
        _log.Information("Starting {Tier}", tierName);
        var data = await readFn();

        if (!dryRun)
        {
            var writer = new TargetWriter(targetConn, _log);
            await writer.WriteAsync(data, report);
        }
        else
        {
            _log.Information("[DRY RUN] Would write {Count} records from {Tier}", data.TotalCount, tierName);
        }

        _log.Information("Completed {Tier}", tierName);
    }

    private static DbConnection CreateConnection(string engine, string connectionString)
    {
        return engine.ToLower() switch
        {
            "sqlite" => new SqliteConnection(connectionString),
            "postgres" or "postgresql" => new NpgsqlConnection(connectionString),
            _ => throw new ArgumentException($"Unsupported engine: {engine}")
        };
    }

    private static async Task EnsureMigrationLogTableAsync(DbConnection conn)
    {
        await conn.ExecuteAsync("""
                                CREATE TABLE IF NOT EXISTS __mealie_migration_log (
                                    id SERIAL PRIMARY KEY,
                                    migrated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                                    source_engine TEXT,
                                    notes TEXT
                                )
                                """);
    }

    private static async Task<bool> IsAlreadyMigratedAsync(DbConnection conn)
    {
        var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM __mealie_migration_log");
        return count > 0;
    }

    private static async Task MarkMigrationCompleteAsync(DbConnection conn)
    {
        await conn.ExecuteAsync(
            "INSERT INTO __mealie_migration_log (migrated_at, notes) VALUES (CURRENT_TIMESTAMP, 'Completed by Mealie.Migration CLI')");
    }
}
