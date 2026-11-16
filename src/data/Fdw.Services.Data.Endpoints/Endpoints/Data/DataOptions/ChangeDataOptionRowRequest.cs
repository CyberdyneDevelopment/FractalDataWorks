using System;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The request of <c>PATCH /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/options/{Id}</c> (FDW-5664).</summary>
public sealed class ChangeDataOptionRowRequest
{
    /// <summary>Gets or sets the data store name (route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path (route).</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container name (route).</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the row's id (route).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the new option; left out, it stays.</summary>
    public string? Option { get; set; }

    /// <summary>Gets or sets whether the row is switched on; left out, it stays.</summary>
    public bool? Active { get; set; }

    /// <summary>Gets or sets the new value; left out, it stays.</summary>
    public string? Value { get; set; }
}
