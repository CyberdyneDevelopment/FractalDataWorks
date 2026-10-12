using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Orchestration.Abstractions.Caching;

/// <summary>
/// Cache for orchestration definitions.
/// </summary>
/// <remarks>
/// Caches orchestration definitions to avoid repeatedly loading them from storage.
/// Definitions change infrequently, so caching provides significant performance benefits.
/// </remarks>
public interface IDefinitionCache
{
    /// <summary>
    /// Gets an orchestration definition from the cache.
    /// </summary>
    /// <typeparam name="TOrchestration">The orchestration type.</typeparam>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="version">Optional specific version. If null, returns the latest.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached orchestration, or null if not found.</returns>
    Task<TOrchestration?> GetDefinition<TOrchestration>(
        string orchestrationId,
        string? version = null,
        CancellationToken cancellationToken = default)
        where TOrchestration : class, IOrchestration;

    /// <summary>
    /// Caches an orchestration definition.
    /// </summary>
    /// <typeparam name="TOrchestration">The orchestration type.</typeparam>
    /// <param name="orchestration">The orchestration to cache.</param>
    /// <param name="options">Cache options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CacheDefinition<TOrchestration>(
        TOrchestration orchestration,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
        where TOrchestration : class, IOrchestration;

    /// <summary>
    /// Invalidates a cached orchestration definition.
    /// </summary>
    /// <param name="orchestrationId">The orchestration ID.</param>
    /// <param name="version">Optional specific version. If null, invalidates all versions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateDefinition(
        string orchestrationId,
        string? version = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all cached orchestration IDs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of cached orchestration IDs.</returns>
    Task<IReadOnlyList<string>> GetCachedIds(CancellationToken cancellationToken = default);
}
