using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Says the name of a place that scopes a definition: an organization or a workspace (FDW-2620). It is what follows a definition's name, in parentheses,
/// where a version held at that place is shown beside the tenant's.
/// </summary>
public interface IPlaceNames
{
    /// <summary>Reads the name of an organization or a workspace.</summary>
    /// <param name="placeKind">One of <see cref="PlaceKinds"/>: Organization or Workspace.</param>
    /// <param name="placeId">The id of the place.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The name; or the failure when the kind is the tenant or not known, the place does not exist, or the read failed.</returns>
    Task<IGenericResult<string>> Get(string placeKind, Guid placeId, CancellationToken cancellationToken = default);
}
