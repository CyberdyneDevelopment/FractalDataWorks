using System.Collections.Generic;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request of <c>POST /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/options</c> (FDW-5664).</summary>
public sealed class AddDataOptionRowsRequest
{
    /// <summary>Gets or sets the data store name (route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path (route).</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container name (route).</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the rows to add, all or none.</summary>
    public List<NewDataOptionRowPayload> Rows { get; set; } = [];
}
