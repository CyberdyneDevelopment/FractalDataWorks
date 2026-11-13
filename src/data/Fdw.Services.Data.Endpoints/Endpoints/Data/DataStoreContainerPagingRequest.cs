using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Request to return a container of a data store to its connection's paging, <c>DELETE /datastores/{Name}/containers/{ContainerId}/paging</c> (FDW-5562).</summary>
public class DataStoreContainerPagingRequest
{
    /// <summary>Gets or sets the data store's name, from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container's identifier, from the route.</summary>
    public Guid ContainerId { get; set; }
}
