using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Expressions.Results;
using Fdw.Results;

namespace Fdw.Expressions;

/// <summary>
/// Compiles and caches LINQ expressions for high-performance data operations.
/// Thread-safe with concurrent caching.
/// </summary>
public sealed class ExpressionBuilder : IExpressionBuilder
{
    private readonly ConcurrentDictionary<string, object> _cache = new(StringComparer.Ordinal);
    private long _cacheHits;
    private long _cacheMisses;
    private TimeSpan _totalCompilationTime = TimeSpan.Zero;
    private readonly object _statsLock = new();

    /// <inheritdoc/>
    public IExpressionCacheStatistics Statistics => new ExpressionCacheStatistics(
        _cacheHits,
        _cacheMisses,
        _cache.Count,
        _totalCompilationTime);

    /// <inheritdoc/>
    public Func<IDataRow, bool> BuildPredicate(
        IDataSchema schema,
        Expression<Func<IDataRow, bool>> predicate)
    {
        var key = $"Predicate:{schema.Name}:{predicate}";
        return (Func<IDataRow, bool>)GetOrCompile(key, () => predicate.Compile());
    }

    /// <inheritdoc/>
    public Func<IDataRow, TResult> BuildSelector<TResult>(
        IDataSchema schema,
        Expression<Func<IDataRow, TResult>> selector)
    {
        var key = $"Selector:{schema.Name}:{typeof(TResult).FullName}:{selector}";
        return (Func<IDataRow, TResult>)GetOrCompile(key, () => selector.Compile());
    }

    /// <inheritdoc/>
    public IFieldAccessor<TValue> BuildFieldAccessor<TValue>(
        IDataSchema schema,
        string fieldName)
    {
        var key = $"FieldAccessor:{schema.Name}:{fieldName}:{typeof(TValue).FullName}";
        return (IFieldAccessor<TValue>)GetOrCompile(key, () =>
        {
            var ordinal = schema.GetOrdinal(fieldName);
            return new CompiledFieldAccessor<TValue>(fieldName, ordinal);
        });
    }

    /// <inheritdoc/>
    public Func<IDataRow[], TResult> BuildAggregation<TResult>(
        IDataSchema schema,
        Expression<Func<IDataRow[], TResult>> aggregator)
    {
        var key = $"Aggregation:{schema.Name}:{typeof(TResult).FullName}:{aggregator}";
        return (Func<IDataRow[], TResult>)GetOrCompile(key, () => aggregator.Compile());
    }

    /// <inheritdoc/>
    public Func<IDataRow, IDataRow, bool> BuildJoinPredicate(
        IDataSchema leftSchema,
        IDataSchema rightSchema,
        Expression<Func<IDataRow, IDataRow, bool>> joinPredicate)
    {
        var key = $"JoinPredicate:{leftSchema.Name}:{rightSchema.Name}:{joinPredicate}";
        return (Func<IDataRow, IDataRow, bool>)GetOrCompile(key, () => joinPredicate.Compile());
    }

    private object GetOrCompile(string key, Func<object> factory)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            System.Threading.Interlocked.Increment(ref _cacheHits);
            return cached;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var compiled = factory();
        stopwatch.Stop();

        // Another thread may have added it concurrently; prefer the winner
        var result = _cache.GetOrAdd(key, compiled);

        // Only count as a miss if we actually compiled (i.e., the item wasn't in the cache before)
        if (ReferenceEquals(result, compiled))
        {
            System.Threading.Interlocked.Increment(ref _cacheMisses);
            lock (_statsLock)
            {
                _totalCompilationTime += stopwatch.Elapsed;
            }
        }
        else
        {
            // Another thread won the race; count as a hit since we got a cached value
            System.Threading.Interlocked.Increment(ref _cacheHits);
        }

        return result;
    }

    /// <inheritdoc/>
    public IGenericResult<Func<IDataRow, TResult>> CompileFormula<TResult>(
        IDataSchema schema,
        string formula)
    {
        var key = $"Formula:{schema.Name}:{typeof(TResult).FullName}:{formula}";

        try
        {
            var compiled = GetOrCompile(key, () =>
            {
                var parser = new FormulaParser(schema);
                return (object)parser.Parse<TResult>(formula).Compile();
            });

            return GenericResult<Func<IDataRow, TResult>>.Success((Func<IDataRow, TResult>)compiled);
        }
        catch (Exception ex)
        {
            return GenericResult<Func<IDataRow, TResult>>.Failure(
                ExpressionResultCodes.ByName("FormulaCompilationFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc/>
    public void ClearCache()
    {
        _cache.Clear();
        _cacheHits = 0;
        _cacheMisses = 0;
        lock (_statsLock)
        {
            _totalCompilationTime = TimeSpan.Zero;
        }
    }
}