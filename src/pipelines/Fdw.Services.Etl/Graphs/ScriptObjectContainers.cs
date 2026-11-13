using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Etl.Graphs;

/// <summary>Finds the containers of a data store an object a script names may be (FDW-5457), by the store's catalogue.</summary>
/// <remarks>
/// A path the script names narrows the search to it; with none named, every path that holds a container of the name is listed, so a caller can
/// tell none from one from several, as the lineage's own container rule does: one is the container, several is a name to make plain.
/// </remarks>
public static class ScriptObjectContainers
{
    /// <summary>Lists the paths of a store that hold a container the name may be.</summary>
    /// <param name="store">The data store, read with its paths and containers.</param>
    /// <param name="name">The object the script names.</param>
    /// <returns>The names of the paths, each once; empty when the store holds no such container.</returns>
    public static IReadOnlyList<string> PathsHolding(IDataStoreImplementationConfiguration store, ScriptObjectName name)
    {
        return [.. store.Paths
            .Where(path => name.Path is null || string.Equals(path.Name, name.Path, StringComparison.OrdinalIgnoreCase))
            .Where(path => path.Containers.Any(container => string.Equals(container.Name, name.Name, StringComparison.OrdinalIgnoreCase)))
            .Select(path => path.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }
}
