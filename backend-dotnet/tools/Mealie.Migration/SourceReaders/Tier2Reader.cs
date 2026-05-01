using System.Data.Common;
using Dapper;

namespace Mealie.Migration.SourceReaders;

public class Tier2Reader(DbConnection conn)
{
    public async Task<MigrationData> ReadAllAsync()
    {
        var data = new MigrationData();
        data.Units = (await conn.QueryAsync("SELECT * FROM ingredient_units")).ToList();
        data.Foods = (await conn.QueryAsync("SELECT * FROM ingredient_foods")).ToList();
        try
        {
            data.FoodAliases = (await conn.QueryAsync("SELECT * FROM ingredient_foods_aliases")).ToList();
        }
        catch
        {
        }

        data.Tags = (await conn.QueryAsync("SELECT * FROM tags")).ToList();
        data.Categories = (await conn.QueryAsync("SELECT * FROM categories")).ToList();
        data.Tools = (await conn.QueryAsync("SELECT * FROM tools")).ToList();
        data.Cookbooks = (await conn.QueryAsync("SELECT * FROM cookbooks")).ToList();
        return data;
    }
}
