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

        List<IField>? fields = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "Fields":
                        fields = JsonSerializer.Deserialize<List<IField>>(ref reader, options);
                        break;
                }
            }
        }

        return new ContainerSchema
        {
            Fields = fields ?? new List<IField>()
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
