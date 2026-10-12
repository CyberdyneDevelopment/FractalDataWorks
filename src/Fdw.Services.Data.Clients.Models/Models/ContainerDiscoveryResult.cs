using System;
using System.Collections.Generic;
using Fdw.Web.Clients.Abstractions.Contracts;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Result of container discovery.
/// </summary>
public sealed class ContainerDiscoveryResult : IContainerDiscovery
{
    /// <summary>Gets or sets the name of the container discovered.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the type of container discovered.</summary>
    public string ContainerType { get; set; } = string.Empty;
    /// <summary>Gets or sets the list of fields discovered in the container.</summary>
    public IReadOnlyList<FieldDiscoveryResult> Fields { get; set; } = Array.Empty<FieldDiscoveryResult>();

    /// <inheritdoc />
    IReadOnlyList<IFieldDiscovery> IContainerDiscovery.Fields => Fields;
}
