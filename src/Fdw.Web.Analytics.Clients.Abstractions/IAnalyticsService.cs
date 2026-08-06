using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.Analytics.Clients.Models;

namespace Fdw.Web.Analytics.Clients;

/// <summary>
/// Service for tracking and reporting calculation analytics.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Records a calculation execution.
    /// </summary>
    Task<IGenericResult> RecordExecution(CalculationExecutionRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets analytics for a time period.
    /// </summary>
    Task<IGenericResult<AnalyticsResponse>> GetAnalytics(AnalyticsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top calculations by usage.
    /// </summary>
    Task<IGenericResult<TopCalculationsResponse>> GetTopCalculations(TopCalculationsRequest request, CancellationToken cancellationToken = default);
}
