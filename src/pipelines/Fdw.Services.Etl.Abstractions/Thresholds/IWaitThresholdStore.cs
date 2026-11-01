using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions.Thresholds;

/// <summary>Reads and writes the level of thresholds on waiting runs an owner states (FDW-3161).</summary>
public interface IWaitThresholdStore
{
    /// <summary>Reads the level an owner states.</summary>
    /// <param name="ownerKind">The kind of the owner, as <c>WaitThresholdOwners</c> names it.</param>
    /// <param name="ownerName">The name of the pipeline or schedule; none for the server.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The level; an owner that states none answers Inherit with no thresholds. A failure naming why when the owner does not exist or the level cannot be read.</returns>
    Task<IGenericResult<WaitThresholdLevelPayload>> Read(string ownerKind, string? ownerName, CancellationToken cancellationToken = default);

    /// <summary>Judges and writes the level an owner states, its mode and its thresholds together.</summary>
    /// <param name="ownerKind">The kind of the owner, as <c>WaitThresholdOwners</c> names it.</param>
    /// <param name="ownerName">The name of the pipeline or schedule; none for the server.</param>
    /// <param name="level">The level.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The level as a later read answers it; a refusal naming the member and why, or the failure to write it.</returns>
    Task<IGenericResult<WaitThresholdLevelPayload>> Write(string ownerKind, string? ownerName, WaitThresholdLevelPayload level, CancellationToken cancellationToken = default);
}
