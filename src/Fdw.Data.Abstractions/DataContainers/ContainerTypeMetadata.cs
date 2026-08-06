using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Data.DataStores.Abstractions;
using Fdw.Results;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Metadata about a data container type's capabilities and characteristics.
/// </summary>
/// <remarks>
/// ContainerTypeMetadata provides comprehensive information about what a container
/// type can and cannot do, which is useful for automatic container selection,
/// validation, and performance optimization.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ContainerTypeMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerTypeMetadata"/> class.
    /// </summary>
    /// <param name="containerType">The container type name.</param>
    /// <param name="supportsRead">Whether reading is supported.</param>
    /// <param name="supportsWrite">Whether writing is supported.</param>
    /// <param name="supportsStreaming">Whether streaming is supported.</param>
    /// <param name="supportsSchemaInference">Whether schema inference is supported.</param>
    /// <param name="fileExtension">The associated file extension.</param>
    /// <param name="mimeType">The associated MIME type.</param>
    /// <param name="compatibleConnections">Compatible connection types.</param>
    /// <param name="limitations">Known limitations.</param>
    /// <param name="additionalMetadata">Additional metadata.</param>
    public ContainerTypeMetadata(
        string containerType,
        bool supportsRead,
        bool supportsWrite,
        bool supportsStreaming,
        bool supportsSchemaInference,
        string? fileExtension = null,
        string? mimeType = null,
        IEnumerable<string>? compatibleConnections = null,
        IEnumerable<string>? limitations = null,
        IDictionary<string, object>? additionalMetadata = null)
    {
        ContainerType = containerType ?? throw new ArgumentNullException(nameof(containerType));
        SupportsRead = supportsRead;
        SupportsWrite = supportsWrite;
        SupportsStreaming = supportsStreaming;
        SupportsSchemaInference = supportsSchemaInference;
        FileExtension = fileExtension;
        MimeType = mimeType;
        CompatibleConnections = compatibleConnections?.ToList() ?? new List<string>();
        Limitations = limitations?.ToList() ?? new List<string>();
        AdditionalMetadata = additionalMetadata != null
            ? new Dictionary<string, object>(additionalMetadata, StringComparer.Ordinal)
            : new Dictionary<string, object>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the container type name.
    /// </summary>
    /// <value>The name of the container type.</value>
    public string ContainerType { get; }

    /// <summary>
    /// Gets a value indicating whether this container type supports reading.
    /// </summary>
    /// <value><c>true</c> if reading is supported; otherwise, <c>false</c>.</value>
    public bool SupportsRead { get; }

    /// <summary>
    /// Gets a value indicating whether this container type supports writing.
    /// </summary>
    /// <value><c>true</c> if writing is supported; otherwise, <c>false</c>.</value>
    public bool SupportsWrite { get; }

    /// <summary>
    /// Gets a value indicating whether this container type supports streaming.
    /// </summary>
    /// <value><c>true</c> if streaming is supported; otherwise, <c>false</c>.</value>
    public bool SupportsStreaming { get; }

    /// <summary>
    /// Gets a value indicating whether this container type supports schema inference.
    /// </summary>
    /// <value><c>true</c> if schema inference is supported; otherwise, <c>false</c>.</value>
    public bool SupportsSchemaInference { get; }

    /// <summary>
    /// Gets the file extension associated with this container type.
    /// </summary>
    /// <value>The file extension, or null if not applicable.</value>
    public string? FileExtension { get; }

    /// <summary>
    /// Gets the MIME type associated with this container type.
    /// </summary>
    /// <value>The MIME type, or null if not applicable.</value>
    public string? MimeType { get; }

    /// <summary>
    /// Gets the connection types compatible with this container.
    /// </summary>
    /// <value>A list of compatible connection type names.</value>
    public IReadOnlyList<string> CompatibleConnections { get; }

    /// <summary>
    /// Gets known limitations of this container type.
    /// </summary>
    /// <value>A list of limitation descriptions.</value>
    public IReadOnlyList<string> Limitations { get; }

    /// <summary>
    /// Gets additional metadata about this container type.
    /// </summary>
    /// <value>Additional properties and information.</value>
    public IReadOnlyDictionary<string, object> AdditionalMetadata { get; }
}