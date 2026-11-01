using Fdw.Configuration;
using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.Discovery;

/// <summary>
/// Connection-type-agnostic shape of a container as returned by an
/// <see cref="ISchemaDiscoverer"/>. Translates to a
/// <c>IGenericConfiguration</c> on the calling side.
/// </summary>
public interface IDiscoveredContainer
{
    /// <summary>The container's path (e.g. SQL schema, HTTP base path).</summary>
    string PathName { get; }

    /// <summary>The container's name (e.g. SQL table/view name, HTTP endpoint).</summary>
    string Name { get; }

    /// <summary>One of <c>Table</c>, <c>View</c>, or implementation-specific values.</summary>
    string ContainerType { get; }

    /// <summary>The columns/fields the container exposes, if discovered.</summary>
    IReadOnlyList<IDiscoveredField> Fields { get; }
}
