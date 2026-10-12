using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for operations on one container of a data store.
/// </summary>
public class DataStoreContainerRequest
{
    /// <summary>Gets or sets the data store name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container's durable id.</summary>
    public Guid ContainerId { get; set; }
}
