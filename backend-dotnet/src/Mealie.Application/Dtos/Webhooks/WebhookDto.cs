namespace Mealie.Application.Dtos.Webhooks;

public class WebhookResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? ScheduledTime { get; set; }
}

public class CreateWebhookRequest
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string? ScheduledTime { get; set; }
}

public class TestWebhookRequest
{
    public string Url { get; set; } = string.Empty;
}
