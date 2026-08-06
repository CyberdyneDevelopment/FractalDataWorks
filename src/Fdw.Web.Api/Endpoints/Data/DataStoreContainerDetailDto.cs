using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Detailed DTO for a data container, including field information.
/// </summary>
public class DataStoreContainerDetailDto
{
    /// <summary>Gets or sets the container identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the container name (table/collection name).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container type (e.g., Table, View).</summary>
    public string ContainerType { get; set; } = string.Empty;

    /// <summary>Gets or sets the container description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the fields within this container.</summary>
    public IList<DataStoreFieldResponse> Fields { get; set; } = [];
}
