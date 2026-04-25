using System.Threading.Channels;

namespace Mealie.Application.Services.Migrations;

public record MigrationJobRequest(
    Guid ReportId,
    Guid GroupId,
    Guid HouseholdId,
    Guid UserId,
    string TempFilePath);

/// <summary>
/// Singleton channel used to queue migration import jobs.
/// The background service is the single consumer; the controller is a producer.
/// </summary>
public class MigrationQueue
{
    private readonly Channel<MigrationJobRequest> _channel =
        Channel.CreateUnbounded<MigrationJobRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    public ChannelWriter<MigrationJobRequest> Writer => _channel.Writer;
    public ChannelReader<MigrationJobRequest> Reader => _channel.Reader;

    public async ValueTask EnqueueAsync(MigrationJobRequest job, CancellationToken ct = default)
        => await _channel.Writer.WriteAsync(job, ct);
}
