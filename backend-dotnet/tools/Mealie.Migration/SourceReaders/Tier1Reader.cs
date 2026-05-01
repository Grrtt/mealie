using System.Data.Common;
using Dapper;

namespace Mealie.Migration.SourceReaders;

public class Tier1Reader(DbConnection conn)
{
    public async Task<MigrationData> ReadAllAsync()
    {
        var data = new MigrationData();
        data.Groups = (await conn.QueryAsync("SELECT * FROM groups")).ToList();
        data.Households = (await conn.QueryAsync("SELECT * FROM households")).ToList();
        data.Users = (await conn.QueryAsync("SELECT * FROM users")).ToList();
        data.ApiKeys = (await conn.QueryAsync("SELECT * FROM long_live_tokens")).ToList();
        data.MultiPurposeLabels = (await conn.QueryAsync("SELECT * FROM multi_purpose_labels")).ToList();
        try
        {
            data.GroupPreferences = (await conn.QueryAsync("SELECT * FROM group_preferences")).ToList();
        }
        catch
        {
        }

        try
        {
            data.HouseholdPreferences = (await conn.QueryAsync("SELECT * FROM household_preferences")).ToList();
        }
        catch
        {
        }

        return data;
    }
}
