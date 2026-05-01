using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Mealie.Infrastructure.Data;

/// <summary>
///     Stores all Guid values as uppercase strings in the database so that
///     case-sensitive engines (e.g. SQLite TEXT) never produce mismatches
///     between values written by different clients.
/// </summary>
public class UppercaseGuidConverter() : ValueConverter<Guid, string>(
    v => v.ToString("D").ToUpperInvariant(),
    v => Guid.Parse(v));
