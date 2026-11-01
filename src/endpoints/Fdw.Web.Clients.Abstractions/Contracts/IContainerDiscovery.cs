namespace Fdw.Web.Clients.Abstractions.Contracts;

using System.Collections.Generic;

/// <summary>
/// Abstraction for container discovery results used across Schema and Data domains.
/// </summary>
public interface IContainerDiscovery
{
    /// <summary>Gets the name of the discovered container.</summary>
    string Name { get; }
    /// <summary>Gets the type of container discovered.</summary>
    string ContainerType { get; }
    /// <summary>Gets the list of fields discovered in the container.</summary>
    IReadOnlyList<IFieldDiscovery> Fields { get; }
}
