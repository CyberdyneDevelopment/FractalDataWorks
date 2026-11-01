using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Says which place is directly above a place of one kind, from the configuration that holds places of that kind (FDW-2538). The ancestry of a place is
/// these answers followed up the tree, so a new level is a new source and nothing else changes.
/// </summary>
public interface IPlaceParentSource
{
    /// <summary>Gets the kind of place this source answers for: one of <see cref="PlaceKinds"/>.</summary>
    string Kind { get; }

    /// <summary>Reads the place directly above a place of this kind.</summary>
    /// <param name="placeId">The id of the place.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The place above, or none when the place is the top of the tree; or the failure when the place does not exist or the read failed.</returns>
    Task<IGenericResult<PlaceRef?>> Above(Guid placeId, CancellationToken cancellationToken = default);
}
