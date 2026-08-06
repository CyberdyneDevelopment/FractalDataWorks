using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fdw.Services.Abstractions.Health.Converters;

/// <summary>
/// JSON converter for <see cref="IHealthState"/>. Reads the state name (from either a bare
/// string token or the <c>name</c> property of an object shape such as
/// <c>{"id":1,"name":"Healthy",..}</c>) and resolves it via <see cref="HealthStates.ByName"/>;
/// writes the state as its name string.
/// </summary>
/// <remarks>
/// Why: <see cref="IHealthState"/> is a TypeCollection interface that System.Text.Json cannot
/// (de)serialize by default ("Deserialization of interface or abstract types is not supported").
/// The server serializes the state as the full TypeOption object; this converter accepts both a
/// bare name string and the full object shape so every health dashboard round-trip works over
/// the wire and on disk.
/// </remarks>
public sealed class HealthStateJsonConverter : JsonConverter<IHealthState>
{
    /// <inheritdoc />
    public override IHealthState? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            return ResolveByName(reader.GetString());
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return ResolveByName(ReadNameFromObject(ref reader));
        }

        // Why: fail loud — the state arrived in a shape we do not understand, rather than
        // silently substituting a default state.
        throw new JsonException(
            $"Unexpected token '{reader.TokenType}' when reading {nameof(IHealthState)}.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IHealthState value, JsonSerializerOptions options)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        writer.WriteStringValue(value.Name);
    }

    private static IHealthState ResolveByName(string? name)
    {
        // Why: the name is required to resolve a health state; a missing name is a hard error,
        // not a defaultable condition.
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new JsonException(
                $"Missing state name when reading {nameof(IHealthState)}.");
        }

        var state = HealthStates.ByName(name!);
        if (state == HealthStates.NotFound)
        {
            // Why: fail loud — an unrecognized state name is a hard error, not a defaultable
            // condition. TypeCollection.ByName returns the NotFound sentinel (never null).
            throw new JsonException(
                $"Unrecognized health state name '{name}' when reading {nameof(IHealthState)}.");
        }

        return state;
    }

    private static string? ReadNameFromObject(ref Utf8JsonReader reader)
    {
        string? name = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return name;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var isNameProperty = string.Equals(reader.GetString(), "name", StringComparison.OrdinalIgnoreCase);
            reader.Read();
            if (isNameProperty && reader.TokenType == JsonTokenType.String)
            {
                name = reader.GetString();
            }
            else
            {
                reader.Skip();
            }
        }

        return name;
    }
}
