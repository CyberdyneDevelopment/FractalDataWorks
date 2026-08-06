using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.Caching;

/// <summary>
/// Service for caching calculation results.
/// </summary>
public interface ICalculationCacheService
{
    /// <summary>
    /// Attempts to retrieve a cached calculation result.
    /// </summary>
    /// <param name="calculationType">The type of calculation.</param>
    /// <param name="values">The input values.</param>
    /// <param name="dataSourceVersion">Optional data source version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached result if found, or null if not cached.</returns>
    Task<IGenericResult<CachedCalculationResult?>> TryGet(
        string calculationType,
        decimal[] values,
        string? dataSourceVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Caches a calculation result.
    /// </summary>
    /// <param name="calculationType">The type of calculation.</param>
    /// <param name="values">The input values.</param>
    /// <param name="result">The calculation result to cache.</param>
    /// <param name="options">Optional cache entry options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> Set(
        string calculationType,
        decimal[] values,
        decimal result,
        CalculationCacheEntryOptions? options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached entries for a specific calculation type.
    /// </summary>
    /// <param name="calculationType">The calculation type to invalidate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the number of invalidated entries.</returns>
    Task<IGenericResult<int>> Invalidate(
        string calculationType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached entries associated with a data source.
    /// </summary>
    /// <param name="dataSourceId">The data source identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the number of invalidated entries.</returns>
    Task<IGenericResult<int>> InvalidateByDataSource(
        string dataSourceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current cache statistics.</returns>
    Task<IGenericResult<CalculationCacheStatistics>> GetStatistics(
        CancellationToken cancellationToken = default);
}
