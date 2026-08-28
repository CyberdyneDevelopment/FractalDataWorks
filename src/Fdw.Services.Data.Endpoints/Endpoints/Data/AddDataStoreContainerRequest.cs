using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for adding a container to an existing data store path.
/// </summary>
public class AddDataStoreContainerRequest
{
    /// <summary>Gets or sets the data store name (from route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path name within the data store to add the container to.</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name for the new container.</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type discriminator (e.g., "Table", "View").</summary>
    public string? ContainerType { get; set; }

    /// <summary>
    /// Gets or sets the serialization format of this container's payload (e.g., "Json", "Xml").
    /// Selects the row-source used to parse responses. Null means inherit the owning connection
    /// transport's declared default format (e.g. Http → Json).
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets a JSONPath-style selector to the array of row objects in the response payload.
    /// Example: "features" for <c>response.features[*]</c>. Null means rows are at the JSON root.
    /// Only meaningful for REST containers.
    /// </summary>
    public string? RecordSelector { get; set; }

    /// <summary>
    /// Gets or sets whether nested JSON objects should be flattened into dot-notation field names.
    /// Null means the container inherits the library default (false).
    /// </summary>
    public bool? FlattenNestedObjects { get; set; }

    /// <summary>
    /// Gets or sets the separator character for flattened field names (e.g., ".").
    /// Null means the container inherits the library default (".").
    /// </summary>
    public string? FlattenSeparator { get; set; }

    /// <summary>
    /// Gets or sets the field (column) definitions for the new container. Without these, the container
    /// persists with zero <c>data.DataContainerField</c> rows and bulk-insert later fails with
    /// "Container X has no insertable fields" (FDW-548).
    /// </summary>
    public IList<CreateDataStoreContainerFieldRequest> Fields { get; set; } = [];
}
