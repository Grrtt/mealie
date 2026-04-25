using Dapper;
using System.Data.Common;

namespace Mealie.Migration.SourceReaders;

public class Tier2Reader(DbConnection conn)
{
    public async Task<MigrationData> ReadAllAsync()
    {
        var data = new MigrationData();
        data.Units = (await conn.QueryAsync("SELECT * FROM ingredient_units")).ToList<dynamic>();
        data.Foods = (await conn.QueryAsync("SELECT * FROM ingredient_foods")).ToList<dynamic>();
        try { data.FoodAliases = (await conn.QueryAsync("SELECT * FROM ingredient_foods_aliases")).ToList<dynamic>(); } catch { }
        data.Tags = (await conn.QueryAsync("SELECT * FROM tags")).ToList<dynamic>();
        data.Categories = (await conn.QueryAsync("SELECT * FROM categories")).ToList<dynamic>();
        data.Tools = (await conn.QueryAsync("SELECT * FROM tools")).ToList<dynamic>();
        data.Cookbooks = (await conn.QueryAsync("SELECT * FROM cookbooks")).ToList<dynamic>();
        return data;
    }
}
