using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Serialization.Converters;

/// <summary>
/// JSON converter for IStorageContainer.
/// Serializes containers with their key properties and schema.
/// Deserializes to SerializedContainer which can be used to reconstruct actual container instances.
/// </summary>
public sealed class ContainerConverter : JsonConverter<IStorageContainer>
{
    private static readonly IReadOnlyDictionary<string, object> EmptyMetadata =
        new Dictionary<string, object>(StringComparer.Ordinal);

    /// <inheritdoc/>
    public override IStorageContainer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var name = root.GetProperty("Name").GetString()!;
        var containerTypeName = root.GetProperty("ContainerType").GetString()!;
        var formatTypeName = root.GetProperty("Format").GetString()!;
        var pathTypeName = root.GetProperty("PathType").GetString()!;
        var pathValue = root.GetProperty("PathValue").GetString()!;

        var schema = ReadSchema(root, options);
        var supportedOperations = ReadSupportedOperations(root, options);
        var metadata = ReadMetadata(root, options);

        var baseContainer = new SerializedContainer
        {
            Name = name,
            ContainerTypeName = containerTypeName,
            FormatTypeName = formatTypeName,
            PathTypeName = pathTypeName,
            PathValue = pathValue,
            Schema = schema,
            SupportedOperations = supportedOperations,
            Metadata = metadata
        };

        return ReadContainerProperties(root, containerTypeName, baseContainer, options);
    }

    private static IContainerSchema ReadSchema(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("Schema", out var schemaElement))
        {
            return JsonSerializer.Deserialize<IContainerSchema>(schemaElement.GetRawText(), options)!;
        }

        return new ContainerSchema { Fields = [] };
    }

    private static string[] ReadSupportedOperations(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("SupportedOperations", out var opsElement))
        {
            return JsonSerializer.Deserialize<string[]>(opsElement.GetRawText(), options) ?? [];
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyDictionary<string, object> ReadMetadata(JsonElement root, JsonSerializerOptions options)
    {
        if (root.TryGetProperty("Metadata", out var metadataElement))
        {
            var deserializedMetadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataElement.GetRawText(), options);
            if (deserializedMetadata != null)
            {
                return deserializedMetadata;
            }
        }

        return EmptyMetadata;
    }

    private static IStorageContainer ReadContainerProperties(
        JsonElement root,
        string containerTypeName,
        SerializedContainer baseContainer,
        JsonSerializerOptions options)
    {
        if (root.TryGetProperty("Properties", out var propsElement))
        {
            if (string.Equals(containerTypeName, "Endpoint", StringComparison.Ordinal))
            {
                var endpointProps = JsonSerializer.Deserialize<EndpointContainerProperties>(propsElement.GetRawText(), options);
                if (endpointProps != null)
                {
                    return baseContainer.WithProperties(endpointProps);
                }
            }

            // Add other container type property mappings here as needed
        }

        return baseContainer;
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IStorageContainer value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString("Name", value.Name);
        writer.WriteString("ContainerType", value.ContainerType.Name);
        writer.WriteString("Format", value.Format.Name);
        writer.WriteString("PathType", value.Path.GetType().Name);
        writer.WriteString("PathValue", value.Path.PathValue);

        writer.WritePropertyName("Schema");
        JsonSerializer.Serialize(writer, value.Schema, options);

        writer.WritePropertyName("SupportedOperations");
        JsonSerializer.Serialize(writer, value.SupportedOperations, options);

        writer.WritePropertyName("Metadata");
        JsonSerializer.Serialize(writer, value.Metadata, options);

        // Serialize container-specific properties based on type
        WriteContainerProperties(writer, value, options);

        writer.WriteEndObject();
    }

    private static void WriteContainerProperties(Utf8JsonWriter writer, IStorageContainer value, JsonSerializerOptions options)
    {
        var containerTypeName = value.ContainerType.Name;

        // Handle EndpointContainer
        if (string.Equals(containerTypeName, "Endpoint", StringComparison.Ordinal))
        {
            var httpMethodsProp = value.GetType().GetProperty("HttpMethods");
            if (httpMethodsProp != null)
            {
                var httpMethods = httpMethodsProp.GetValue(value) as string[];
                if (httpMethods != null)
                {
                    writer.WritePropertyName("Properties");
                    JsonSerializer.Serialize(writer, new EndpointContainerProperties { HttpMethods = httpMethods }, options);
                }
            }
        }

        // Handle SerializedContainer<TProperties> - it already has typed properties
        if (value is SerializedContainer<EndpointContainerProperties> typedEndpoint && typedEndpoint.Properties != null)
        {
            writer.WritePropertyName("Properties");
            JsonSerializer.Serialize(writer, typedEndpoint.Properties, options);
        }

        // Add other container type property serialization here as needed
    }
}
