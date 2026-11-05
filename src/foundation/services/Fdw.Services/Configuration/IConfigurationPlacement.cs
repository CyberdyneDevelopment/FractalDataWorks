using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Configuration;

/// <summary>
/// Puts a newly made member somewhere, in the unit of work that makes it (FDW-3692).
/// </summary>
/// <remarks>
/// The service base cannot name where a member goes: that is the business of a domain above it (the workspaces domain places a data set
/// in its owner's default workspace). So the base asks, through this, at the one point every member is made, and the domain answers. The
/// placement runs inside the same unit as the member's own rows, so a placement that fails takes the member with it and nothing is
/// ever made unplaced. A placement that cannot place says why with a failed result; it never places the member somewhere it guessed.
/// </remarks>
public interface IConfigurationPlacement
{
    /// <summary>Places a member that was just made in <paramref name="unit"/>.</summary>
    /// <param name="request">What was made.</param>
    /// <param name="unit">The unit of work the member was made in; the placement writes into it.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the failure that stops the member being made.</returns>
    Task<IGenericResult> Place(PlacementRequest request, ConfigurationWriteScope unit, CancellationToken cancellationToken = default);
}
