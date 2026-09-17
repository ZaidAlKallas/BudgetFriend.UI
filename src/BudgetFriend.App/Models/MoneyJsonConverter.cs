using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudgetFriend.App.Models;

/// <summary>
/// The BudgetFriend.API serializes decimal money values as JSON strings
/// (for example <c>"amount": "-997"</c>). This converter accepts both a JSON
/// string and a JSON number so the frontend is resilient either way.
/// </summary>
public sealed class MoneyJsonConverter : JsonConverter<decimal> {
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return reader.TokenType switch {
            JsonTokenType.String => decimal.Parse(reader.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture),
            JsonTokenType.Number => reader.GetDecimal(),
            JsonTokenType.Null => 0m,
            _ => throw new JsonException($"Unexpected token {reader.TokenType} for decimal value.")
        };
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) {
        writer.WriteNumberValue(value);
    }
}

/// <summary>
/// Serializes DateTimeOffset values always as an ISO 8601 UTC instant with the
/// trailing 'Z' (e.g. 2024-03-20T13:45:30Z). The API's PostgreSQL timestamptz
/// columns reject non-UTC DateTimes; a plain "+00:00" offset is deserialized
/// by the server as Kind=Local and fails, while 'Z' round-trips as Kind=Utc.
/// </summary>
public sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset> {
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTimeOffset();

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(
            value.ToUniversalTime().UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
}

/// <summary>
/// Base type for any type that carries a sentinel Zero (e.g. counts that the
/// API may or may not include). Intentionally minimal.
/// </summary>
public static class JsonDefaults {
    public static readonly JsonSerializerOptions Api = CreateApiOptions();

    private static JsonSerializerOptions CreateApiOptions() {
        var options = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new MoneyJsonConverter());
        options.Converters.Add(new UtcDateTimeOffsetConverter());
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}