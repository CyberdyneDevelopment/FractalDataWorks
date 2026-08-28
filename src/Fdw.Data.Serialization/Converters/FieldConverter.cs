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
        IPropertyRole role = PropertyRoles.ByName("Attribute");
        bool isNullable = false;
        bool isIdentity = false;
        bool isComputed = false;
        bool isSystemProvided = false;
        string? visibilityId = null;
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
                    ref isSystemProvided,
                ref visibilityId,
                    ref description, ref typeSystemId, ref converterTypeId);
            }
        }

        if (string.IsNullOrEmpty(name) || fieldType == null)
            throw new JsonException("Field must have Name and FieldType properties");

        return CreateField(name!, fieldType, role, isNullable,
            isIdentity, isComputed, isSystemProvided, description, typeSystemId, converterTypeId, visibilityId);
    }

    private static void ReadProperty(
        string? propertyName,
        ref Utf8JsonReader reader,
        JsonSerializerOptions options,
        ref string? name,
        ref IFieldType? fieldType,
        ref IPropertyRole role,
        ref bool isNullable,
        ref bool isIdentity,
        ref bool isComputed,
        ref bool isSystemProvided,
        ref string? visibilityId,
        ref string? description,
        ref string? typeSystemId,
        ref int? converterTypeId)
    {
        switch (propertyName)
        {
            case "Name":
                name = reader.GetString();
                break;
            case "FieldType":
                fieldType = JsonSerializer.Deserialize<IFieldType>(ref reader, options);
                break;
            case "Role":
                // Parse role name and fall back to Attribute if unknown
                var roleName = reader.GetString() ?? "Attribute";
                role = PropertyRoles.ByName(roleName) ?? PropertyRoles.ByName("Attribute");
                break;
            case "IsNullable":
                isNullable = reader.GetBoolean();
                break;
            case "IsPrimaryKey":
                reader.GetBoolean();
                break;
            case "IsIdentity":
                isIdentity = reader.GetBoolean();
                break;
            case "IsComputed":
                isComputed = reader.GetBoolean();
                break;
            case "IsSystemProvided":
                isSystemProvided = reader.GetBoolean();
                break;
            case "VisibilityId":
                visibilityId = reader.GetString();
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
        IPropertyRole role,
        bool isNullable,
        bool isIdentity,
        bool isComputed,
        bool isSystemProvided,
        string? description,
        string? typeSystemId,
        int? converterTypeId,
        string? visibilityId)
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
            Description = description,
            TypeSystemId = typeSystemId,
            ConverterTypeId = converterTypeId,
            Visibility = visibilityId is null
                ? FieldVisibilities.ByName("Visible")
                : FieldVisibilities.ByName(visibilityId)
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

        if (value.Description != null)
            writer.WriteString("Description", value.Description);

        if (value.TypeSystemId != null)
            writer.WriteString("TypeSystemId", value.TypeSystemId);

        if (value.ConverterTypeId.HasValue)
            writer.WriteNumber("ConverterTypeId", value.ConverterTypeId.Value);

        writer.WriteEndObject();
    }
}
