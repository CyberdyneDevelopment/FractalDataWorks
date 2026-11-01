using System.Threading.Tasks;

namespace Fdw.Calculations.Contracts.Hubs;

/// <summary>
/// Client-side SignalR hub interface for calculation notifications.
/// </summary>
public interface ICalculationHubClient
{
    /// <summary>Notifies that a calculation has started.</summary>
    Task CalculationStarted(CalculationStartedEvent evt);

    /// <summary>Notifies calculation progress updates.</summary>
    Task CalculationProgress(CalculationProgressEvent evt);

    /// <summary>Notifies that a calculation completed successfully.</summary>
    Task CalculationCompleted(CalculationCompletedEvent evt);

    /// <summary>Notifies that a calculation failed.</summary>
    Task CalculationFailed(CalculationFailedEvent evt);

    /// <summary>Notifies cache statistics updates.</summary>
    Task CacheStatisticsUpdated(CacheStatisticsEvent evt);
}
