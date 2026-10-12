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
/// Metadata about a specific data container instance.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ContainerMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerMetadata"/> class.
    /// </summary>
    /// <param name="name">The name of the container.</param>
    /// <param name="sizeBytes">The size of the container in bytes.</param>
    /// <param name="createdDate">The creation date of the container.</param>
    /// <param name="modifiedDate">The last modification date of the container.</param>
    /// <param name="additionalMetadata">Additional metadata properties.</param>
    public ContainerMetadata(
        string name,
        long? sizeBytes = null,
        DateTime? createdDate = null,
        DateTime? modifiedDate = null,
        IDictionary<string, object>? additionalMetadata = null)
    {
        Name = name;
        SizeBytes = sizeBytes;
        CreatedDate = createdDate;
        ModifiedDate = modifiedDate;
        AdditionalMetadata = (IReadOnlyDictionary<string, object>)(additionalMetadata ?? new Dictionary<string, object>(StringComparer.Ordinal));
    }

    /// <summary>
    /// Gets the name of the container.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the size of the container in bytes.
    /// </summary>
    public long? SizeBytes { get; }

    /// <summary>
    /// Gets the creation date of the container.
    /// </summary>
    public DateTime? CreatedDate { get; }

    /// <summary>
    /// Gets the last modification date of the container.
    /// </summary>
    public DateTime? ModifiedDate { get; }

    /// <summary>
    /// Gets additional metadata associated with the container.
    /// </summary>
    public IReadOnlyDictionary<string, object> AdditionalMetadata { get; }
}