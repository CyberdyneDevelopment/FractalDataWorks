using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>Reads a threshold as a payload (FDW-3948).</summary>
public static class ThresholdReads
{
    /// <summary>Reads a threshold by name.</summary>
    /// <param name="thresholds">The threshold provider.</param>
    /// <param name="name">The threshold's name.</param>
    /// <param name="ct">Cancels the call.</param>
    /// <returns>The payload; a null value when no threshold has that name; a failure when it cannot be read.</returns>
    public static async Task<IGenericResult<ThresholdPayload?>> Find(IThresholdConfigurationProvider thresholds, string name, CancellationToken ct)
    {
        var stored = await thresholds.Get(name, ct).ConfigureAwait(false);
        if (stored.IsFailure)
        {
            return stored.ToNewResult<ThresholdPayload?>();
        }

        if (stored.Value is null)
        {
            return GenericResult<ThresholdPayload?>.Success(null);
        }

        var payload = ThresholdMapper.ToPayload(stored.Value);
        return payload.IsFailure ? payload.ToNewResult<ThresholdPayload?>() : GenericResult<ThresholdPayload?>.Success(payload.Value);
    }

    /// <summary>Reads a threshold that has just been written; one that is not there is an error, not a null.</summary>
    /// <param name="thresholds">The threshold provider.</param>
    /// <param name="name">The threshold's name.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="ct">Cancels the call.</param>
    /// <returns>The payload; a failure when it cannot be read back.</returns>
    public static async Task<IGenericResult<ThresholdPayload>> Read(IThresholdConfigurationProvider thresholds, string name, ILogger logger, CancellationToken ct)
    {
        var found = await Find(thresholds, name, ct).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return found.ToNewResult<ThresholdPayload>();
        }

        if (found.Value is null)
        {
            ThresholdEndpointLog.SavedButUnreadable(logger, name);
            return ThresholdRefusal.Of<ThresholdPayload>(name, "it was saved and cannot be read back");
        }

        return GenericResult<ThresholdPayload>.Success(found.Value);
    }
}
