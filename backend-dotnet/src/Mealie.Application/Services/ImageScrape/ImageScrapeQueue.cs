using System.Threading.Channels;

namespace Mealie.Application.Services.ImageScrape;

/// <summary>
/// Singleton channel used to queue image scrape jobs.
/// The background service is the single consumer; the migration service is a producer.
/// </summary>
public class ImageScrapeQueue
{
    private readonly Channel<ImageScrapeJob> _channel =
        Channel.CreateUnbounded<ImageScrapeJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    public ChannelWriter<ImageScrapeJob> Writer => _channel.Writer;
    public ChannelReader<ImageScrapeJob> Reader => _channel.Reader;
}
