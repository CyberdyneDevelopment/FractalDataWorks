using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request of <c>DELETE /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/options?id=…&amp;id=…</c> (FDW-5664).</summary>
public sealed class RemoveDataOptionRowsRequest
{
    /// <summary>Gets or sets the data store name (route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path (route).</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container name (route).</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the ids of the rows to remove (query <c>id</c>, repeated), all or none.</summary>
    public List<Guid> Id { get; set; } = [];
}
