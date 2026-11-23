using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fdw.Data.Abstractions;
using Fdw.Schema;

namespace Fdw.Data.Serialization.Converters;

/// <summary>
/// JSON converter for IField.
/// </summary>
public sealed class FieldConverter : JsonConverter<IField>
{
    /// <inheritdoc/>
    public override IField Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject token");

        var wire = JsonSerializer.Deserialize<FieldWire>(ref reader, options)
            ?? throw new JsonException("Field must have Name and FieldType properties");

        if (string.IsNullOrEmpty(wire.Name) || wire.FieldType == null)
            throw new JsonException("Field must have Name and FieldType properties");

        return new Field
        {
            Name = wire.Name,
            FieldType = wire.FieldType,
            // Why NotYetTyped: a field that names no datum type has not been typed; that is its state, not a guess.
            // An unknown name resolves to NotFound, which the reader of the field can see.
            Role = wire.Role is { } datumTypeName ? DatumTypes.ByName(datumTypeName) : DatumTypes.NotYetTyped,
            IsNullable = wire.IsNullable,
            IsIdentity = wire.IsIdentity,
            IsComputed = wire.IsComputed,
            IsSystemProvided = wire.IsSystemProvided,
            HasDefault = wire.HasDefault,
            Description = wire.Description,
            TypeSystemId = wire.TypeSystemId,
            ConverterTypeId = wire.ConverterTypeId
        };
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IField value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Name", value.Name);

        writer.WritePropertyName("FieldType");
        JsonSerializer.Serialize(writer, value.FieldType, options);

        // Write role name directly
        writer.WriteString("Role", value.Role.Name);
        writer.WriteBoolean("IsNullable", value.IsNullable);
        writer.WriteBoolean("IsIdentity", value.IsIdentity);
        writer.WriteBoolean("IsComputed", value.IsComputed);
        writer.WriteBoolean("IsSystemProvided", value.IsSystemProvided);
        writer.WriteBoolean("HasDefault", value.HasDefault);

        if (value.Description != null)
            writer.WriteString("Description", value.Description);

        if (value.TypeSystemId != null)
            writer.WriteString("TypeSystemId", value.TypeSystemId);

        if (value.ConverterTypeId.HasValue)
            writer.WriteNumber("ConverterTypeId", value.ConverterTypeId.Value);

        writer.WriteEndObject();
    }
}
