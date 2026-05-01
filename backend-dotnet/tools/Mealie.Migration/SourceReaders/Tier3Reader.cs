using System.Data.Common;
using Dapper;

namespace Mealie.Migration.SourceReaders;

public class Tier3Reader(DbConnection conn)
{
    public async Task<MigrationData> ReadAllAsync()
    {
        var data = new MigrationData();
        data.Recipes = (await conn.QueryAsync("SELECT * FROM recipes")).ToList();
        data.Ingredients = (await conn.QueryAsync("SELECT * FROM recipes_ingredients")).ToList();
        data.Instructions = (await conn.QueryAsync("SELECT * FROM recipe_instructions")).ToList();
        try
        {
            data.Notes = (await conn.QueryAsync("SELECT * FROM notes")).ToList();
        }
        catch
        {
        }

        try
        {
            data.Assets = (await conn.QueryAsync("SELECT * FROM recipe_assets")).ToList();
        }
        catch
        {
        }

        data.Comments = (await conn.QueryAsync("SELECT * FROM recipe_comments")).ToList();
        try
        {
            data.TimelineEvents = (await conn.QueryAsync("SELECT * FROM recipe_timeline_events")).ToList();
        }
        catch
        {
        }

        try
        {
            data.ShareTokens = (await conn.QueryAsync("SELECT * FROM recipe_share_tokens")).ToList();
        }
        catch
        {
        }

        return data;
    }
}
