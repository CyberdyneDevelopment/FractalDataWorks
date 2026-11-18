using System;
using System.Linq;

namespace Fdw.Services.Connections;

/// <summary>
/// Says whether a path or schema name is one of a data store's working areas (FDW-6391).
/// </summary>
/// <remarks>
/// A working area is a place whose tables exist only while a pipeline runs, so lineage and the script save check treat a table in one as if the pipeline never
/// touched it. The judgement is by name alone: nothing is looked up in the store's catalog, so a table that was dropped or never discovered is judged the same
/// as one the store holds. A store that states none has none, and a name with no path is never in one.
/// </remarks>
public static class WorkingAreas
{
    /// <summary>Whether <paramref name="name"/> is a working area of <paramref name="store"/>.</summary>
    /// <param name="store">The data store that states its working areas.</param>
    /// <param name="name">The path or schema name a container or a script's table sits in; null or blank is never in one.</param>
    /// <returns><see langword="true"/> when the store states a working area equal to the name, ignoring case; the whole name must match.</returns>
    public static bool Contains(IDataStoreImplementationConfiguration store, string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && store.WorkingPaths.Any(working => string.Equals(working, name, StringComparison.OrdinalIgnoreCase));
}
