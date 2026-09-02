using System;
using System.Globalization;
using Fdw.Services.Calculations.Configuration;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Caching;
using Fdw.Services.Calculations.Logging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Calculations.Caching;

/// <summary>
/// Implementation of calculation caching using IDistributedCache.
/// </summary>
public sealed class CalculationCacheService : ICalculationCacheService
{
    private readonly IDistributedCache _cache;
    private readonly CacheKeyGenerator _keyGenerator;
    private readonly CalculationCacheConfiguration _options;
    private readonly ILogger<CalculationCacheService> _logger;

    private long _hits;
    private long _misses;
    private readonly ConcurrentDictionary<string, int> _entrySizes = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationCacheService"/> class.
    /// </summary>
    /// <param name="cache">The distributed cache instance.</param>
    /// <param name="keyGenerator">The cache key generator.</param>
    /// <param name="options">The calculation cache options.</param>
    /// <param name="logger">Optional logger instance.</param>
    public CalculationCacheService(
        IDistributedCache cache,
        CacheKeyGenerator keyGenerator,
        CalculationCacheConfiguration options,
        ILogger<CalculationCacheService>? logger)
    {
        _cache = cache;
        _keyGenerator = keyGenerator;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<CalculationCacheService>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<CachedCalculationResult?>> TryGet(
        string calculationType,
        decimal[] values,
        string? dataSourceVersion,
        CancellationToken cancellationToken = default)
    {
        CalculationCacheLog.TryGetStarting(_logger, calculationType, values.Length);

        if (!_options.Enabled)
        {
            CalculationCacheLog.TryGetSkippedDisabled(_logger);
            return GenericResult<CachedCalculationResult?>.Success(null);
        }

        var cacheKey = _keyGenerator.Generate(calculationType, values, dataSourceVersion);
        CalculationCacheLog.TryGetKeyGenerated(_logger, cacheKey, calculationType);

        try
        {
            var cached = await _cache.GetAsync(cacheKey, cancellationToken).ConfigureAwait(false);

            if (cached is null)
            {
                Interlocked.Increment(ref _misses);
                CalculationCacheLog.CacheMiss(_logger, calculationType, cacheKey);
                return GenericResult<CachedCalculationResult?>.Success(null);
            }

            var json = Encoding.UTF8.GetString(cached);
            var result = JsonSerializer.Deserialize<CachedCalculationResult>(json);

            if (result is null)
            {
                CalculationCacheLog.CacheDeserializationFailed(_logger, cacheKey, "Deserialized to null");
                return GenericResult<CachedCalculationResult?>.Success(null);
            }

            Interlocked.Increment(ref _hits);
            CalculationCacheLog.CacheHit(_logger, calculationType, cacheKey);
            return GenericResult<CachedCalculationResult?>.Success(result);
        }
        catch (Exception ex)
        {
            CalculationCacheLog.CacheOperationFailed(_logger, cacheKey, ex.Message);
            return GenericResult<CachedCalculationResult?>.Failure(
                CalculationCacheLog.CacheOperationFailed(_logger, cacheKey, ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Set(
        string calculationType,
        decimal[] values,
        decimal result,
        CalculationCacheEntryOptions? options,
        CancellationToken cancellationToken = default)
    {
        CalculationCacheLog.SetStarting(_logger, calculationType, values.Length, result);

        if (!_options.Enabled)
        {
            CalculationCacheLog.SetSkippedDisabled(_logger);
            return GenericResult.Success();
        }

        var cacheKey = _keyGenerator.Generate(calculationType, values, null);
        var ttlMinutes = GetTtl(calculationType, options);
        var now = DateTimeOffset.UtcNow;

        var cachedResult = new CachedCalculationResult
        {
            CalculationType = calculationType,
            Result = result,
            InputCount = values.Length,
            CachedAt = now,
            ExpiresAt = now.AddMinutes(ttlMinutes)
        };

        try
        {
            var json = JsonSerializer.Serialize(cachedResult);
            var bytes = Encoding.UTF8.GetBytes(json);

            CalculationCacheLog.SetSerialized(_logger, bytes.Length, cacheKey);

            if (bytes.Length > _options.MaxCachedResultSizeBytes)
            {
                CalculationCacheLog.ResultTooLarge(_logger, bytes.Length, _options.MaxCachedResultSizeBytes);
                return GenericResult.Success();
            }

            var cacheOptions = new DistributedCacheEntryOptions();

            if (options?.UseSlidingExpiration == true)
            {
                cacheOptions.SlidingExpiration = TimeSpan.FromMinutes(ttlMinutes);
            }
            else
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ttlMinutes);
            }

            await _cache.SetAsync(cacheKey, bytes, cacheOptions, cancellationToken).ConfigureAwait(false);

            _entrySizes[cacheKey] = bytes.Length;
            CalculationCacheLog.CacheEntrySet(_logger, cacheKey);
            CalculationCacheLog.ResultCached(_logger, calculationType, ttlMinutes);

            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                CalculationCacheLog.CacheOperationFailed(_logger, cacheKey, ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<int>> Invalidate(
        string calculationType,
        CancellationToken cancellationToken = default)
    {
        CalculationCacheLog.InvalidateStarting(_logger, calculationType);

        var prefix = _keyGenerator.GenerateTypePrefix(calculationType);
        var count = 0;

        try
        {
            foreach (var key in _entrySizes.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                    _entrySizes.TryRemove(key, out _);
                    count++;
                }
            }
        }
        catch (Exception ex)
        {
            CalculationCacheLog.InvalidateKeyFailed(_logger, calculationType, ex.Message);
            return GenericResult<int>.Failure(
                CalculationCacheLog.InvalidateKeyFailed(_logger, calculationType, ex.Message));
        }

        CalculationCacheLog.CacheInvalidated(_logger, count, calculationType);
        return GenericResult<int>.Success(count);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<int>> InvalidateByDataSource(
        string dataSourceId,
        CancellationToken cancellationToken = default)
    {
        CalculationCacheLog.InvalidateByDataSourceStarting(_logger, dataSourceId);

        var count = 0;

        try
        {
            foreach (var key in _entrySizes.Keys)
            {
                await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                _entrySizes.TryRemove(key, out _);
                count++;
            }
        }
        catch (Exception ex)
        {
            CalculationCacheLog.InvalidateKeyFailed(_logger, dataSourceId, ex.Message);
            return GenericResult<int>.Failure(
                CalculationCacheLog.InvalidateKeyFailed(_logger, dataSourceId, ex.Message));
        }

        CalculationCacheLog.InvalidateByDataSourceCompleted(_logger, count, dataSourceId);
        return GenericResult<int>.Success(count);
    }

    /// <inheritdoc/>
    public Task<IGenericResult<CalculationCacheStatistics>> GetStatistics(
        CancellationToken cancellationToken = default)
    {
        CalculationCacheLog.GetStatisticsStarting(_logger);

        var hits = Interlocked.Read(ref _hits);
        var misses = Interlocked.Read(ref _misses);

        long totalSize = 0;
        foreach (var size in _entrySizes.Values)
        {
            totalSize += size;
        }

        var stats = new CalculationCacheStatistics
        {
            TotalHits = hits,
            TotalMisses = misses,
            CachedEntries = _entrySizes.Count,
            TotalSizeBytes = totalSize
        };

        CalculationCacheLog.CacheStatisticsRetrieved(_logger, hits, misses, stats.HitRate);

        return Task.FromResult(GenericResult<CalculationCacheStatistics>.Success(stats));
    }

    private int GetTtl(string calculationType, CalculationCacheEntryOptions? options)
    {
        if (options?.TtlMinutes.HasValue == true)
        {
            return Math.Min(options.TtlMinutes.Value, _options.MaxTtlMinutes);
        }

        if (_options.TtlByCalculationType.TryGetValue(calculationType, out var typeTtlText)
            && int.TryParse(typeTtlText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var typeTtl))
        {
            return Math.Min(typeTtl, _options.MaxTtlMinutes);
        }

        return _options.DefaultTtlMinutes;
    }
}
