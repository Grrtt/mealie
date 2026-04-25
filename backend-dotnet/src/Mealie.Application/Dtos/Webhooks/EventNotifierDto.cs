namespace Mealie.Application.Dtos.Webhooks;

public class EventNotifierResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ApprisUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public class CreateEventNotifierRequest
{
    public string Name { get; set; } = string.Empty;
    public string ApprisUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}
