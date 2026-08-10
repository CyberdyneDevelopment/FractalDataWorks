using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// DTO representing a data container with path information included.
/// </summary>
public class DataStoreContainerWithPathDto
{
    /// <summary>Gets or sets the container identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the data store name.</summary>
    public string DataStoreName { get; set; } = string.Empty;

    /// <summary>Gets or sets the path (schema) name.</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container name (table/collection name).</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type (e.g., Table, View).</summary>
    public string ContainerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of fields in the container.</summary>
    public int FieldCount { get; set; }
}
