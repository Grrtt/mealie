using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Mealie.Infrastructure.Data;

public class TextDateTimeConverter() : ValueConverter<DateTime, string>(
    v => v.ToString("O", CultureInfo.InvariantCulture),
    v => TextTemporalConversion.ParseDateTime(v));

public class NullableTextDateTimeConverter() : ValueConverter<DateTime?, string?>(
    v => v.HasValue ? v.Value.ToString("O", CultureInfo.InvariantCulture) : null,
    v => v == null ? null : TextTemporalConversion.ParseNullableDateTime(v));

public class TextDateOnlyConverter() : ValueConverter<DateOnly, string>(
    v => v.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
    v => TextTemporalConversion.ParseDateOnly(v));

public class NullableTextDateOnlyConverter() : ValueConverter<DateOnly?, string?>(
    v => v.HasValue ? v.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null,
    v => v == null ? null : TextTemporalConversion.ParseNullableDateOnly(v));

static class TextTemporalConversion
{
    public static DateTime ParseDateTime(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public static DateTime? ParseNullableDateTime(string value) =>
        DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    public static DateOnly ParseDateOnly(string value) =>
        DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);

    public static DateOnly? ParseNullableDateOnly(string value) =>
        DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);
}
