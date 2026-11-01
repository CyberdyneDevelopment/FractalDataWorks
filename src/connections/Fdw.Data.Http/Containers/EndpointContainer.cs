using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.Http.Paths;

namespace Fdw.Data.Http.Containers;

/// <summary>
/// Represents a REST API endpoint container.
/// ContainerType: Endpoint, Format: Json/Xml/Protobuf (from OpenAPI or Accept header)
/// </summary>
/// <remarks>
/// Why (Stage 3): the shared <c>ContainerBase</c> was deleted by the foundational redesign (the
/// uniform <see cref="DataContainer"/> base replaces it for runtime nodes). <c>EndpointContainer</c>
/// is constructed only by the legacy REST schema importers (<c>RestOpenApiSchemaImporter</c> /
/// <c>ODataSchemaImporter</c>) from an already-materialised <see cref="IContainerSchema"/>, not from
/// <see cref="IDataField"/> child nodes, so it stays a plain <see cref="IStorageContainer"/> rather
/// than being forced into the field-children <see cref="IDataContainer"/> model. The few
/// <c>ContainerBase</c> members it relied on are inlined here.
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires HTTP connections
public sealed class EndpointContainer : IStorageContainer
{
    /// <summary>
    /// Metadata key: JSONPath-style selector to the array of row objects.
    /// Example: "features" or "$.features". Absent = rows at JSON root.
    /// </summary>
    public const string RecordSelectorKey = "RecordSelector";

    /// <summary>
    /// Metadata key: whether to flatten nested JSON objects into dot-notation fields.
    /// Value type: bool.
    /// </summary>
    public const string FlattenNestedObjectsKey = "FlattenNestedObjects";

    /// <summary>
    /// Metadata key: separator character for flattened field names (default ".").
    /// Value type: string.
    /// </summary>
    public const string FlattenSeparatorKey = "FlattenSeparator";

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointContainer"/> class.
    /// </summary>
    /// <param name="name">The endpoint name.</param>
    /// <param name="path">The HTTP path to the endpoint.</param>
    /// <param name="schema">The container schema with field definitions.</param>
    /// <param name="format">The data format (required - Json, Xml, etc.).</param>
    /// <param name="httpMethods">The HTTP methods supported by this endpoint (e.g., ["GET", "POST"]).</param>
    /// <param name="recordSelector">
    /// Optional JSONPath-style path to the array of row objects (e.g. "features" or "$.features").
    /// Null means rows are at the JSON root.
    /// </param>
    /// <param name="flattenNestedObjects">
    /// When true, nested JSON objects are flattened into dot-notation fields.
    /// Corresponds to <see cref="FlattenNestedObjectsKey"/> metadata entry.
    /// </param>
    /// <param name="flattenSeparator">
    /// Separator character for flattened field names. Default is ".".
    /// Corresponds to <see cref="FlattenSeparatorKey"/> metadata entry.
    /// </param>
    public EndpointContainer(
        string name,
        HttpPath path,
        IContainerSchema schema,
        IFormatType format,
        string[] httpMethods,
        string? recordSelector = null,
        bool flattenNestedObjects = false,
        string flattenSeparator = ".")
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        Format = format ?? throw new ArgumentNullException(nameof(format));
        HttpMethods = httpMethods ?? throw new ArgumentNullException(nameof(httpMethods));
        ContainerType = EndpointContainerType.Instance;
        SupportedOperations = MapHttpMethodsToOperations(httpMethods);

        var meta = new Dictionary<string, object>(StringComparer.Ordinal);
        if (recordSelector is not null)
            meta[RecordSelectorKey] = recordSelector;
        meta[FlattenNestedObjectsKey] = flattenNestedObjects;
        meta[FlattenSeparatorKey] = flattenSeparator;
        Metadata = meta;
    }

    /// <summary>
    /// Gets the HTTP methods supported by this endpoint.
    /// </summary>
    public string[] HttpMethods { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public IContainerType ContainerType { get; }

    /// <inheritdoc/>
    public IFormatType Format { get; }

    /// <inheritdoc/>
    public IPath Path { get; }

    /// <inheritdoc/>
    public IContainerSchema Schema { get; }

    /// <inheritdoc/>
    public string[] SupportedOperations { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    private static string[] MapHttpMethodsToOperations(string[] methods)
    {
        var operations = new List<string>();
        foreach (var method in methods.Select(m => m.ToUpperInvariant()))
        {
            switch (method)
            {
                case "GET":
                    operations.Add("Query");
                    break;
                case "POST":
                    operations.Add("Insert");
                    break;
                case "PUT":
                case "PATCH":
                    operations.Add("Update");
                    break;
                case "DELETE":
                    operations.Add("Delete");
                    break;
            }
        }
        return operations.Distinct(StringComparer.Ordinal).ToArray();
    }
}
