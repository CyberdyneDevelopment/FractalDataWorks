using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>
/// Reads a JSON <c>null</c> for a whole-number property as the property not being given, so <c>"skip": null</c> means
/// what leaving <c>skip</c> out means.
/// </summary>
/// <remarks>
/// A request body's number property that the caller may leave out reads as its unset value when it is absent, but a
/// <c>null</c> for it failed the whole request with "Cannot get the value of a token type 'Null' as a number" -- and an
/// API reference's generated example writes <c>null</c> for every member it has no value for (FDW-1125). The property is
/// left as an absent one leaves it. Anything other than <c>null</c> is read as a number is read everywhere else, so a
/// value that is not a whole number is still refused.
/// </remarks>
public sealed class NullAsOmittedInt32Converter : JsonConverter<int>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        return JsonSerializer.Deserialize<int>(ref reader, options);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteNumberValue(value);
    }
}
