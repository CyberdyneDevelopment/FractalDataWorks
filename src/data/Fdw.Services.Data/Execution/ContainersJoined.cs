using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Services.Data.Execution;

/// <summary>
/// The containers other than its own that a command names: the ones a query joins (FDW-4328).
/// </summary>
/// <remarks>
/// The translator finds a join target by name among the containers of the primary container's store, so a command that joins needs those containers
/// resolved with its own. Container names are unique within a store, so a name is all that identifies one.
/// </remarks>
public static class ContainersJoined
{
    /// <summary>Gets the names of the other containers <paramref name="command"/> names, once each.</summary>
    /// <param name="command">The command.</param>
    /// <param name="container">The container the command is run on, which is not counted among them.</param>
    /// <returns>The targets and sources of its joins; none for a command that does not join, a vectorized one among them.</returns>
    public static IReadOnlyList<string> By(IGenericCommand command, string container)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command is not IQueryCommand { Joins.Count: > 0 } query)
            return [];

        return [.. query.Joins
            .SelectMany(join => new[] { join.TargetContainerName, join.SourceContainerName })
            .Where(name => !string.IsNullOrWhiteSpace(name) && !string.Equals(name, container, StringComparison.Ordinal))
            .Select(name => name!)
            .Distinct(StringComparer.Ordinal)];
    }
}
