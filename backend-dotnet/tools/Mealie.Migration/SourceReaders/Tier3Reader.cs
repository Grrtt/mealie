using Dapper;
using System.Data.Common;

namespace Mealie.Migration.SourceReaders;

public class Tier3Reader(DbConnection conn)
{
    public async Task<MigrationData> ReadAllAsync()
    {
        var data = new MigrationData();
        data.Recipes = (await conn.QueryAsync("SELECT * FROM recipes")).ToList<dynamic>();
        data.Ingredients = (await conn.QueryAsync("SELECT * FROM recipes_ingredients")).ToList<dynamic>();
        data.Instructions = (await conn.QueryAsync("SELECT * FROM recipe_instructions")).ToList<dynamic>();
        try { data.Notes = (await conn.QueryAsync("SELECT * FROM notes")).ToList<dynamic>(); } catch { }
        try { data.Assets = (await conn.QueryAsync("SELECT * FROM recipe_assets")).ToList<dynamic>(); } catch { }
        data.Comments = (await conn.QueryAsync("SELECT * FROM recipe_comments")).ToList<dynamic>();
        try { data.TimelineEvents = (await conn.QueryAsync("SELECT * FROM recipe_timeline_events")).ToList<dynamic>(); } catch { }
        try { data.ShareTokens = (await conn.QueryAsync("SELECT * FROM recipe_share_tokens")).ToList<dynamic>(); } catch { }
        return data;
    }
}
