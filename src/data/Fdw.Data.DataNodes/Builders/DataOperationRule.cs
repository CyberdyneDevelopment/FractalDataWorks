using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Builders;

/// <summary>
/// The one rule for the operations a command performs against a container (FDW-6682): the container's type states every one of them.
/// </summary>
/// <remarks>
/// Why one rule: a container type declares the operations it supports (<see cref="IStorageContainer.SupportedOperations"/>), and nothing judged a command against them, so
/// an insert into a view was built and sent to the server. The connection asks this rule before it calls a translator that states its operations
/// (<c>IStatesOperations</c>), so nothing is built or sent for a command the container's type does not state. A container that states no operation states none: no
/// operation is assumed for it. Beside <see cref="DataContainerTypeRule"/>, which judges the type a container states; this judges the command against that type.
/// </remarks>
public static class DataOperationRule
{
    /// <summary>Checks that the container's type states every operation the command performs.</summary>
    /// <param name="container">The container the command runs against.</param>
    /// <param name="operations">The operations the command performs.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>A success, or a coded failure naming the container, its type, the first operation it does not state, and the operations it states.</returns>
    public static IGenericResult Check(IStorageContainer container, IReadOnlyList<string> operations, ILogger logger)
    {
        var stated = container.SupportedOperations;
        var missing = operations.FirstOrDefault(operation => !stated.Contains(operation, StringComparer.Ordinal));
        return missing is null
            ? GenericResult.Success()
            : GenericResult.Failure(DataOperationLog.OperationNotStated(
                logger, container.Name, container.ContainerType.Name, missing, stated.Length == 0 ? "none" : string.Join(", ", stated)));
    }
}
