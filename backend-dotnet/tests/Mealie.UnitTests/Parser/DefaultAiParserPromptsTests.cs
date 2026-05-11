using Mealie.Application.Services.Parser;

namespace Mealie.UnitTests.Parser;

public class DefaultAiParserPromptsTests
{
    [Fact]
    public void CategoryPrompt_IncludesStandardizedGlutenFreeCategory()
    {
        Assert.Contains("Gluten-Free", DefaultAiParserPrompts.Category);
        Assert.Contains("Add Gluten-Free", DefaultAiParserPrompts.Category);
    }
}
