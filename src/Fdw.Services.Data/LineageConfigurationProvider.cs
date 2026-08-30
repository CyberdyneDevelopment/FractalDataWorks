using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions.Caching;
using Fdw.Data;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Reads the configuration rows lineage is assembled from.
/// </summary>
/// <remarks>
/// <para>
/// Lineage spans containers no single domain provider covers — DataSets and their sources in the
/// <c>data</c> schema, transformation chains and their steps in <c>transform</c>. The transform
/// containers have no configuration types of their own, so there is no per-container provider to
/// inject and the endpoints were reading them straight off a gateway, naming the store, schema and
/// container at each call site.
/// </para>
/// <para>
/// This is the provider those endpoints inject instead. It holds the one gateway lineage reads
/// through and the only copy of which store and schema those containers live in, so an endpoint
/// names a container and nothing else. Giving the transform domain real configuration types and
/// providers would be better still, and would retire this — it is a seam, not a destination.
/// </para>
/// </remarks>
public class LineageConfigurationProvider
{
    /// <summary>The connection lineage rows are read from.</summary>
    public const string DataStoreName = "PlatformConfiguration";

    private readonly IConfigurationGatewayProvider _gatewayProvider;

    /// <summary>Initializes a new instance of the <see cref="LineageConfigurationProvider"/> class.</summary>
    /// <param name="gatewayProvider">Resolves the gateway for this provider's store.</param>
    public LineageConfigurationProvider(IConfigurationGatewayProvider gatewayProvider)
    {
        _gatewayProvider = gatewayProvider ?? throw new ArgumentNullException(nameof(gatewayProvider));
    }

    /// <summary>Reads every row of a container in the <c>data</c> schema.</summary>
    /// <typeparam name="T">The record type the rows map to.</typeparam>
    /// <param name="containerName">The container to read.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public virtual Task<IReadOnlyList<T>> ReadData<T>(
        string containerName, CancellationToken cancellationToken = default)
        where T : class
        => Read<T>("data", containerName, null, cancellationToken);

    /// <summary>Reads every row of a container in the <c>transform</c> schema.</summary>
    /// <typeparam name="T">The record type the rows map to.</typeparam>
    /// <param name="containerName">The container to read.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public virtual Task<IReadOnlyList<T>> ReadTransform<T>(
        string containerName, CancellationToken cancellationToken = default)
        where T : class
        => Read<T>("transform", containerName, null, cancellationToken);

    /// <summary>Reads every row of a container, cached.</summary>
    /// <typeparam name="T">The record type the rows map to.</typeparam>
    /// <param name="pathName">The schema the container lives in.</param>
    /// <param name="containerName">The container to read.</param>
    /// <param name="cacheDuration">How long the result stays cached.</param>
    /// <param name="invalidationTags">Tags that evict it.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <remarks>
    /// The cache policy is built here rather than at each call site. Every lineage endpoint cached
    /// these reads and each assembled the same three metadata keys by hand, so the duration and the
    /// tag wiring were copied per endpoint and could drift between them.
    /// </remarks>
    public virtual Task<IReadOnlyList<T>> ReadCached<T>(
        string pathName,
        string containerName,
        TimeSpan cacheDuration,
        string[] invalidationTags,
        CancellationToken cancellationToken = default)
        where T : class
        => Read<T>(
            pathName,
            containerName,
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                [CachePolicy.CacheEnabledKey] = true,
                [CachePolicy.CacheDurationKey] = cacheDuration,
                [CachePolicy.CacheInvalidationTagsKey] = invalidationTags
            },
            cancellationToken);

    /// <summary>Reads every row of a container, carrying cache metadata.</summary>
    /// <typeparam name="T">The record type the rows map to.</typeparam>
    /// <param name="pathName">The schema the container lives in.</param>
    /// <param name="containerName">The container to read.</param>
    /// <param name="metadata">Command metadata, typically a cache policy.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// The rows, or empty when the read fails. Lineage is a whole-graph view assembled from several
    /// containers and renders what it can find; one unreadable container degrades the graph rather
    /// than failing the request, which is the behaviour these endpoints already had.
    /// </returns>
    public virtual async Task<IReadOnlyList<T>> Read<T>(
        string pathName,
        string containerName,
        IReadOnlyDictionary<string, object>? metadata,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var gateway = _gatewayProvider.Get(DataStoreName);
        if (gateway.IsFailure || gateway.Value is not { } resolved)
            return [];

        var command = metadata is null
            ? new QueryCommand<T>()
            : new QueryCommand<T> { Metadata = metadata };

        var result = await resolved
            .Execute<IEnumerable<T>>(command, new DataStoreTarget(DataStoreName, pathName, containerName), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess && result.Value is { } rows ? rows.ToList() : [];
    }
}
