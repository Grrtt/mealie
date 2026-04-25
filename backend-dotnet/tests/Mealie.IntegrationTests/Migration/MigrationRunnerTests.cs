using Xunit;

namespace Mealie.IntegrationTests.Migration;

public class MigrationRunnerTests
{
    [Fact(Skip = "Requires SQLite fixture database — run manually")]
    public async Task RunAsync_WithValidSqliteSource_MigratesAllEntities()
    {
        // Arrange
        var runner = new Mealie.Migration.MigrationRunner(
            sourceConnectionString: "Data Source=test_fixture.db",
            targetConnectionString: "Host=localhost;Database=mealie_test;Username=mealie;Password=mealie",
            sourceEngine: "sqlite",
            targetEngine: "postgres",
            dryRun: false);

        // Act
        var exitCode = await runner.RunAsync();

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact(Skip = "Requires database — run manually")]
    public async Task RunAsync_SecondRun_ReturnsExitCode1()
    {
        var runner = new Mealie.Migration.MigrationRunner(
            sourceConnectionString: "Data Source=test_fixture.db",
            targetConnectionString: "Host=localhost;Database=mealie_test;Username=mealie;Password=mealie",
            sourceEngine: "sqlite",
            targetEngine: "postgres",
            dryRun: false);

        var firstRun = await runner.RunAsync();
        Assert.Equal(0, firstRun);

        var secondRun = await runner.RunAsync();
        Assert.Equal(1, secondRun);
    }
}
