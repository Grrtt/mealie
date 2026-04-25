namespace Mealie.Migration;

public class MigrationReport
{
    private readonly List<(string Entity, int Migrated, int Skipped, int Errors)> _rows = [];

    public void Add(string entity, int migrated, int skipped, int errors)
        => _rows.Add((entity, migrated, skipped, errors));

    public IReadOnlyList<(string Entity, int Migrated, int Skipped, int Errors)> Rows => _rows;
}

public static class ReportGenerator
{
    public static void PrintReport(MigrationReport report)
    {
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              MIGRATION SUMMARY REPORT                  ║");
        Console.WriteLine("╠══════════════════════╦══════════╦══════════╦══════════╣");
        Console.WriteLine("║ Entity               ║ Migrated ║  Skipped ║   Errors ║");
        Console.WriteLine("╠══════════════════════╬══════════╬══════════╬══════════╣");

        int totalMigrated = 0, totalSkipped = 0, totalErrors = 0;
        foreach (var (entity, migrated, skipped, errors) in report.Rows)
        {
            Console.WriteLine($"║ {entity,-20} ║ {migrated,8} ║ {skipped,8} ║ {errors,8} ║");
            totalMigrated += migrated;
            totalSkipped += skipped;
            totalErrors += errors;
        }

        Console.WriteLine("╠══════════════════════╬══════════╬══════════╬══════════╣");
        Console.WriteLine($"║ {"TOTAL",-20} ║ {totalMigrated,8} ║ {totalSkipped,8} ║ {totalErrors,8} ║");
        Console.WriteLine("╚══════════════════════╩══════════╩══════════╩══════════╝");

        if (totalErrors > 0)
            Console.WriteLine($"\n⚠️  {totalErrors} records had errors — check migration.log for details");
        else
            Console.WriteLine("\n✅ Migration completed successfully!");
    }
}
