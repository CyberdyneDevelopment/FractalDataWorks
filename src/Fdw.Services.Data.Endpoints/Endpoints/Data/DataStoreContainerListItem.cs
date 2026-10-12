using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// One container found in a data store, with the path it is in (FDW-1194).
/// </summary>
/// <remarks>
/// For the store's container search and for one path's containers. A container's fields and keys are not read to find
/// it, so the item carries no field count; they are read on their own route when the container is opened.
/// </remarks>
public class DataStoreContainerListItem
{
    /// <summary>Gets or sets the container identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the container name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the container's kind (table, view, file and so on), as the store records it.</summary>
    public string? ContainerType { get; set; }

    /// <summary>Gets or sets the <c>ClassificationId</c> of the container's data classification level.</summary>
    public int ClassificationId { get; set; }

    /// <summary>Gets or sets the identifier of the path the container is in.</summary>
    public Guid PathId { get; set; }

    /// <summary>Gets or sets the name of the path the container is in.</summary>
    public string PathName { get; set; } = string.Empty;
}
