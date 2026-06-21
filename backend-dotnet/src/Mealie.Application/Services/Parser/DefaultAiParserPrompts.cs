namespace Mealie.Application.Services.Parser;

public static class DefaultAiParserPrompts
{
    public const string Ingredient = """
                                     You are a recipe ingredient parser. Given a numbered list of ingredient strings,
                                     extract the structured data for each one and return a JSON object with a single key
                                     "ingredients" whose value is an array of objects — one per input line, in the same order.
                                     Each object must have exactly these fields:
                                     - quantity: number or null
                                     - unit: string or null (the unit of measure, e.g. "cup", "tablespoon")
                                     - food: string or null (the main ingredient, e.g. "flour", "butter")
                                     - note: string or null (preparation notes, e.g. "finely chopped", "room temperature")
                                     The array length must equal the number of input lines.
                                     """;

    public const string Category = """
                                     Classify this recipe into the appropriate standardized categories.
                                     Choose only from: Breakfast, Lunch, Dinner, Snack, Side, Gluten-Free.
                                     Consider the recipe name, description, and ingredients to determine what category or categories apply.
                                     You must choose at least one additional category when there is a clear fit beyond the website's existing categories.
                                     Add Gluten-Free when the recipe is explicitly gluten-free or the ingredient list clearly indicates it is gluten-free.
                                     The website's existing categories are shown for context — do not repeat them, only add clearly
                                     applicable standardized categories that are missing.
                                     Return only categories that clearly apply. If none apply, return an empty array.
                                     """;

    public const string Tag = """
                              Suggest cuisine and culture tags for this recipe.
                              Consider labels such as: Italian, Mexican, Chinese, Indian, Japanese, Thai, Mediterranean,
                              American, French, Greek, Korean, Vietnamese, Middle Eastern, Comfort, Quick, Healthy.
                              The website's existing tags are shown for context — do not repeat them, only suggest new
                              culture/cuisine tags that are not already listed.
                              Return only tags that clearly apply. If none apply, return an empty array.
                              """;
}
