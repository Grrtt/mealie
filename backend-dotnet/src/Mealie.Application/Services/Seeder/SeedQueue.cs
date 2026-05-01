using System.Threading.Channels;

namespace Mealie.Application.Services.Seeder;

public enum SeedType
{
    Foods,
    Labels,
    Units
}

public record SeedJobRequest(Guid GroupId, string Locale, SeedType Type);

/// <summary>
///     Singleton channel used to queue seed jobs (foods, labels, units).
///     The background service is the single consumer; controllers are producers.
/// </summary>
public class SeedQueue
{
    private readonly Channel<SeedJobRequest> _channel =
        Channel.CreateUnbounded<SeedJobRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelWriter<SeedJobRequest> Writer => _channel.Writer;
    public ChannelReader<SeedJobRequest> Reader => _channel.Reader;
}
