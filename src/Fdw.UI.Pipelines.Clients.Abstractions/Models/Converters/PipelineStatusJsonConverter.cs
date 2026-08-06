using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fdw.UI.Pipelines.Clients.Models.Converters;

/// <summary>
/// JSON converter for <see cref="IPipelineStatus"/>. Reads the status name (from either a bare
/// string token or the <c>name</c> property of an object shape such as
/// <c>{"id":0,"name":"Draft",..}</c>) and resolves it via <see cref="PipelineStatuses.ByName"/>;
/// writes the status as its name string.
/// </summary>
/// <remarks>
/// Why: <see cref="IPipelineStatus"/> is a TypeCollection interface that System.Text.Json cannot
/// (de)serialize by default ("Deserialization of interface or abstract types is not supported").
/// The server serializes the status as the full TypeOption object, while the client's stored JSON
/// uses a bare name string — this converter accepts both so every designer round-trip (Builder and
/// Calculated Designer save/load/publish/edit) works over the wire and on disk.
/// </remarks>
public sealed class PipelineStatusJsonConverter : JsonConverter<IPipelineStatus>
{
    /// <inheritdoc />
    public override IPipelineStatus? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        // Why: fail loud — the status arrived in a shape we do not understand, rather than
        // silently substituting a default status.
        throw new JsonException(
            $"Unexpected token '{reader.TokenType}' when reading {nameof(IPipelineStatus)}.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IPipelineStatus value, JsonSerializerOptions options)
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

    private static IPipelineStatus ResolveByName(string? name)
    {
        // Why: the name is required to resolve a status; a missing name is a hard error, not a
        // defaultable condition.
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new JsonException(
                $"Missing status name when reading {nameof(IPipelineStatus)}.");
        }

        return PipelineStatuses.ByName(name!);
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
