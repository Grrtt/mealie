namespace Mealie.Application.Dtos.Organizers;

public class CookbookReorderRequest
{
    public Guid Id { get; set; }
    public int Position { get; set; }
}
