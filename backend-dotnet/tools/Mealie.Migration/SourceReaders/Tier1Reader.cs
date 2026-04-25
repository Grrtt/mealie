using Dapper;
using System.Data.Common;

namespace Mealie.Migration.SourceReaders;

public class Tier1Reader(DbConnection conn)
{
    public async Task<MigrationData> ReadAllAsync()
    {
        var data = new MigrationData();
        data.Groups = (await conn.QueryAsync("SELECT * FROM groups")).ToList<dynamic>();
        data.Households = (await conn.QueryAsync("SELECT * FROM households")).ToList<dynamic>();
        data.Users = (await conn.QueryAsync("SELECT * FROM users")).ToList<dynamic>();
        data.ApiKeys = (await conn.QueryAsync("SELECT * FROM long_live_tokens")).ToList<dynamic>();
        data.MultiPurposeLabels = (await conn.QueryAsync("SELECT * FROM multi_purpose_labels")).ToList<dynamic>();
        try { data.GroupPreferences = (await conn.QueryAsync("SELECT * FROM group_preferences")).ToList<dynamic>(); } catch { }
        try { data.HouseholdPreferences = (await conn.QueryAsync("SELECT * FROM household_preferences")).ToList<dynamic>(); } catch { }
        return data;
    }
}
