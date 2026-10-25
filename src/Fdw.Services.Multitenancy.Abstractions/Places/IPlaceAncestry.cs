using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Says which places are at or above a place in the scope tree, nearest first (FDW-2512).
/// </summary>
/// <remarks>
/// The tree is configuration, read through the providers that hold it: a workspace names its organization, or its tenant when it names none, and an
/// organization names its tenant. A kind of definition finds the one that applies to a place by matching its own owner column against these steps and
/// taking the smallest depth.
/// <para>
/// This asks nothing of the caller: it says what is above a place, not whether the caller may know it. A caller asks about a place whose tenant it has
/// already checked, as an endpoint does with <c>IOrganizationScope</c> or <c>IScopeAuthority</c>.
/// </para>
/// </remarks>
public interface IPlaceAncestry
{
    /// <summary>Reads the places at or above a place.</summary>
    /// <param name="placeKind">One of <see cref="PlaceKinds"/>.</param>
    /// <param name="placeId">The id of the place.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The place itself at depth 0 and each place above it, nearest first; or the failure: a kind that is not known, a place that does not exist, or a
    /// read of the configuration that failed.
    /// </returns>
    Task<IGenericResult<IReadOnlyList<PlaceStep>>> Get(string placeKind, Guid placeId, CancellationToken cancellationToken = default);
}
