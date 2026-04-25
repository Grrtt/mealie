using Mealie.Migration;
using Serilog;
using System.CommandLine;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("migration.log", rollingInterval: RollingInterval.Infinite)
    .CreateLogger();

var sourceOption = new Option<string>("--source") { Required = true, Description = "Source connection string (SQLite path or PostgreSQL DSN)" };
var targetOption = new Option<string>("--target") { Required = true, Description = "Target connection string (PostgreSQL DSN)" };
var sourceEngineOption = new Option<string>("--source-engine") { Description = "Source database engine: sqlite or postgres" };
sourceEngineOption.DefaultValueFactory = _ => "sqlite";
var targetEngineOption = new Option<string>("--target-engine") { Description = "Target database engine: postgres" };
targetEngineOption.DefaultValueFactory = _ => "postgres";
var dryRunOption = new Option<bool>("--dry-run") { Description = "Validate source data without writing to target" };
dryRunOption.DefaultValueFactory = _ => false;

var rootCommand = new RootCommand("Mealie data migration tool — transfer data from Python backend to C# backend");
rootCommand.Add(sourceOption);
rootCommand.Add(targetOption);
rootCommand.Add(sourceEngineOption);
rootCommand.Add(targetEngineOption);
rootCommand.Add(dryRunOption);

rootCommand.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
{
    var source = parseResult.GetValue(sourceOption)!;
    var target = parseResult.GetValue(targetOption)!;
    var sourceEngine = parseResult.GetValue(sourceEngineOption) ?? "sqlite";
    var targetEngine = parseResult.GetValue(targetEngineOption) ?? "postgres";
    var dryRun = parseResult.GetValue(dryRunOption);

    Log.Information("Mealie Migration starting. Source={Source}, Target={Target}, DryRun={DryRun}", source, target, dryRun);

    try
    {
        var runner = new MigrationRunner(source, target, sourceEngine, targetEngine, dryRun);
        return await runner.RunAsync();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Migration failed with unhandled exception");
        return 1;
    }
    finally
    {
        await Log.CloseAndFlushAsync();
    }
});

var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args);
