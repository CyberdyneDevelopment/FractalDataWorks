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

        string? name = null;
        IFieldType? fieldType = null;
        // Why NotYetTyped: a field that names no datum type has not been typed; that is its state, not a guess.
        IDatumType role = DatumTypes.NotYetTyped;
        bool isNullable = false;
        bool isIdentity = false;
        bool isComputed = false;
        bool isSystemProvided = false;
        bool hasDefault = false;
        string? description = null;
        string? typeSystemId = null;
        int? converterTypeId = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                ReadProperty(propertyName, ref reader, options,
                    ref name, ref fieldType, ref role,
                    ref isNullable, ref isIdentity, ref isComputed,
                    ref isSystemProvided, ref hasDefault,
                    ref description, ref typeSystemId, ref converterTypeId);
            }
        }

        if (string.IsNullOrEmpty(name) || fieldType == null)
            throw new JsonException("Field must have Name and FieldType properties");

        return CreateField(name!, fieldType, role, isNullable,
            isIdentity, isComputed, isSystemProvided, hasDefault, description, typeSystemId, converterTypeId);
    }

    private static void ReadProperty(
        string? propertyName,
        ref Utf8JsonReader reader,
        JsonSerializerOptions options,
        ref string? name,
        ref IFieldType? fieldType,
        ref IDatumType role,
        ref bool isNullable,
        ref bool isIdentity,
        ref bool isComputed,
        ref bool isSystemProvided,
        ref bool hasDefault,
        ref string? description,
        ref string? typeSystemId,
        ref int? converterTypeId)
    {
        if (ReadIdentityProperty(propertyName, ref reader, options, ref name, ref fieldType, ref role, ref isNullable))
            return;

        ReadFlagsAndMetadataProperty(
            propertyName, ref reader,
            ref isIdentity, ref isComputed, ref isSystemProvided, ref hasDefault,
            ref description, ref typeSystemId, ref converterTypeId);
    }

    // Why split from ReadProperty: Name/FieldType/Role/IsNullable/IsPrimaryKey identify what the
    // field IS; the rest (below) are flags and descriptive metadata about it. Splitting on that
    // line is also what keeps each switch's cyclomatic complexity under FDW007's threshold.
    private static bool ReadIdentityProperty(
        string? propertyName,
        ref Utf8JsonReader reader,
        JsonSerializerOptions options,
        ref string? name,
        ref IFieldType? fieldType,
        ref IDatumType role,
        ref bool isNullable)
    {
        switch (propertyName)
        {
            case "Name":
                name = reader.GetString();
                return true;
            case "FieldType":
                fieldType = JsonSerializer.Deserialize<IFieldType>(ref reader, options);
                return true;
            case "Role":
                // Why no substitute: an unknown name resolves to NotFound, which the reader of the field
                // can see; a JSON null means the field was never typed.
                role = reader.GetString() is { } datumTypeName ? DatumTypes.ByName(datumTypeName) : DatumTypes.NotYetTyped;
                return true;
            case "IsNullable":
                isNullable = reader.GetBoolean();
                return true;
            case "IsPrimaryKey":
                reader.GetBoolean();
                return true;
            default:
                return false;
        }
    }

    private static void ReadFlagsAndMetadataProperty(
        string? propertyName,
        ref Utf8JsonReader reader,
        ref bool isIdentity,
        ref bool isComputed,
        ref bool isSystemProvided,
        ref bool hasDefault,
        ref string? description,
        ref string? typeSystemId,
        ref int? converterTypeId)
    {
        switch (propertyName)
        {
            case "IsIdentity":
                isIdentity = reader.GetBoolean();
                break;
            case "IsComputed":
                isComputed = reader.GetBoolean();
                break;
            case "IsSystemProvided":
                isSystemProvided = reader.GetBoolean();
                break;
            case "HasDefault":
                hasDefault = reader.GetBoolean();
                break;
            case "Description":
                description = reader.GetString();
                break;
            case "TypeSystemId":
                typeSystemId = reader.GetString();
                break;
            case "ConverterTypeId":
                converterTypeId = reader.GetInt32();
                break;
        }
    }

    private static Field CreateField(
        string name,
        IFieldType fieldType,
        IDatumType role,
        bool isNullable,
        bool isIdentity,
        bool isComputed,
        bool isSystemProvided,
        bool hasDefault,
        string? description,
        string? typeSystemId,
        int? converterTypeId)
    {
        return new Field
        {
            Name = name,
            FieldType = fieldType,
            Role = role,
            IsNullable = isNullable,
            IsIdentity = isIdentity,
            IsComputed = isComputed,
            IsSystemProvided = isSystemProvided,
            HasDefault = hasDefault,
            Description = description,
            TypeSystemId = typeSystemId,
            ConverterTypeId = converterTypeId
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
