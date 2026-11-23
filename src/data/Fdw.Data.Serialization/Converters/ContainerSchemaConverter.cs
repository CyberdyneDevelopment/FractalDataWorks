using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Serialization.Converters;

/// <summary>
/// JSON converter for IContainerSchema.
/// </summary>
public sealed class ContainerSchemaConverter : JsonConverter<IContainerSchema>
{
    /// <inheritdoc/>
    public override IContainerSchema Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        return new ContainerSchema
        {
            Fields = JsonSerializer.Deserialize<ContainerSchemaWire>(ref reader, options)?.Fields ?? []
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IContainerSchema value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("Fields");
        JsonSerializer.Serialize(writer, value.Fields, options);

        writer.WriteEndObject();
    }
}
