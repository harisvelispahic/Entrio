using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IoT.API.Configuration;

/// <summary>
/// Forces every serialized <see cref="DateTime"/> to carry a UTC "Z" suffix.
///
/// WHY THIS IS NEEDED
/// ------------------
/// Every timestamp in this system is produced by <c>DateTime.UtcNow</c>, but SQL Server's
/// <c>datetime2</c> stores no offset, so EF reads values back with
/// <see cref="DateTimeKind.Unspecified"/>. System.Text.Json then omits the "Z", emitting
/// "2026-09-23T21:57:07" — and <c>new Date("2026-09-23T21:57:07")</c> in the browser
/// interprets that as LOCAL time. In UTC+2 every event and "last updated" time displayed
/// two hours off.
///
/// Treating Unspecified as UTC is correct here precisely because the whole codebase only
/// ever writes UtcNow; there is no local-time value to misinterpret.
///
/// THE CONVERTER KNOWS NO TIME ZONE
/// -------------------------------
/// Nothing here converts between zones. Unspecified is RELABELLED as UTC, not shifted, so
/// the output does not depend on where the API runs. The client owns all local-time
/// conversion: the browser turns the user's wall-clock choice into a UTC instant (which
/// is also what makes it DST-correct, since the browser knows the offset actually in
/// force on that date) and renders incoming instants back into local time.
/// </summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ToUtc(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(ToUtc(value).ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture));

    /// <summary>
    /// Read and write share this so a value survives a round trip unchanged.
    ///
    /// Unspecified is relabelled, never converted. It previously went through
    /// <c>ToUniversalTime()</c> on the read path, which interprets Unspecified as
    /// SERVER-LOCAL time: the same request then meant a different instant in the
    /// container (UTC) than on a developer machine (UTC+2). Timestamps on the wire are
    /// UTC by contract, so relabelling is both correct and environment-independent.
    /// </summary>
    internal static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

/// <summary>Same treatment for nullable timestamps (AcknowledgedAtUtc, OpenedAtUtc, ...).</summary>
public sealed class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
{
    private static readonly UtcDateTimeConverter Inner = new();

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null ? null : UtcDateTimeConverter.ToUtc(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            Inner.Write(writer, value.Value, options);
    }
}
