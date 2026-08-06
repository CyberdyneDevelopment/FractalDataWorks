using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Summary DTO for a data container within a path.
/// </summary>
public class DataStoreContainerResponse
{
    /// <summary>Gets or sets the container identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the container name (table/collection name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type (e.g., Table, View).</summary>
    public string ContainerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of fields in the container.</summary>
    public int FieldCount { get; set; }

    /// <summary>Gets or sets the source-discovered description.</summary>
    public string? SourceDescription { get; set; }

    /// <summary>Gets or sets the auto-generated key field names.</summary>
    public IList<string> SurrogateKeyFields { get; set; } = [];

    /// <summary>Gets or sets the business key field names.</summary>
    public IList<string> NaturalKeyFields { get; set; } = [];

    /// <summary>Gets or sets the fields within the container.</summary>
    public IList<DataStoreFieldResponse> Fields { get; set; } = [];
}
