using System.Collections.Generic;
using Fdw.Configuration.Endpoints.Logging;
using Fdw.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Configuration.Endpoints;

/// <summary>
/// Keeps the containers that hold configurations of their own and refuses the ones that hold parts of another container's configuration.
/// </summary>
/// <remarks>
/// A container whose rows hang from another container's rows (an implementation body, a typed child list, or a key/value property
/// collection such as a connection's MsSqlConnectionProperty, whose secrets sit in its Value column) is not an instance, and these
/// endpoints never read it (FDW-4644). Which containers those are is read from the keys each container declares
/// (<see cref="DataContainerOwnership.IsOwnedByAnotherContainer"/>), never from a container's or a column's name.
/// </remarks>
internal static class TopLevelConfigurationContainers
{
    /// <summary>The containers of <paramref name="containers"/> that hold configurations of their own; each refused one is logged.</summary>
    /// <param name="containers">The containers of a category.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The top-level containers, in the order given.</returns>
    internal static List<IDataContainer> Of(IReadOnlyList<IDataContainer> containers, ILogger logger)
    {
        var kept = new List<IDataContainer>(containers.Count);
        foreach (var container in containers)
        {
            if (container.IsOwnedByAnotherContainer())
            {
                ConfigurationEndpointLog.ChildContainerRefused(logger, container.Parent.Name, container.Name);
                continue;
            }

            kept.Add(container);
        }

        return kept;
    }
}
