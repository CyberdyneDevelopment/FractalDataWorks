using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The type an update saves a graph as (FDW-6718): the stored one. A request that leaves the type out keeps it; one that states another is refused, because changing a
/// pipeline's type is not part of an update.
/// </summary>
public static class GraphUpdateType
{
    /// <summary>Chooses the type an update saves.</summary>
    /// <param name="requested">What the request states, or null when it leaves the type out.</param>
    /// <param name="stored">The stored pipeline's implementation.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The type to save as, or the coded refusal.</returns>
    public static IGenericResult<string> Choose(string? requested, string stored, ILogger? logger = null)
    {
        // An absent type keeps the stored one: the same field of the stored record, which an update that omits a field keeps.
        if (string.IsNullOrWhiteSpace(requested))
            return GenericResult<string>.Success(stored);

        return string.Equals(requested, stored, StringComparison.Ordinal)
            ? GenericResult<string>.Success(stored)
            : GenericResult<string>.Failure(GraphImplementationLog.ImplementationChanged(logger ?? NullLogger.Instance, requested, stored));
    }
}
