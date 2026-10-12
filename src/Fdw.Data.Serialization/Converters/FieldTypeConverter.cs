using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Serialization.Converters;

/// <summary>
/// JSON converter for IFieldType.
/// Serializes as { "TypeName": "String", "ClrType": "System.String" }
/// </summary>
public sealed class FieldTypeConverter : JsonConverter<IFieldType>
{
    /// <inheritdoc/>
    public override IFieldType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        string? typeName = null;
        string? clrTypeName = null;

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
                    case "TypeName":
                        typeName = reader.GetString();
                        break;
                    case "ClrType":
                        clrTypeName = reader.GetString();
                        break;
                }
            }
        }

        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(clrTypeName))
            throw new JsonException("FieldType must have TypeName and ClrType properties");

        var clrType = Type.GetType(clrTypeName) ?? typeof(object);

        return new SimpleFieldType
        {
            TypeName = typeName,
            ClrType = clrType
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IFieldType value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("TypeName", value.TypeName);
        writer.WriteString("ClrType", value.ClrType.AssemblyQualifiedName ?? value.ClrType.FullName ?? "System.Object");
        writer.WriteEndObject();
    }
}
