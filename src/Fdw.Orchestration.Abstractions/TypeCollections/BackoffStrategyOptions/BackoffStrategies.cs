using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Orchestration.Abstractions.TypeCollections.BackoffStrategyOptions;

/// <summary>
/// TypeCollection for backoff strategies.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for backoff strategies.
/// Source generator creates static properties for each registered backoff strategy.
/// </remarks>
[TypeCollection(typeof(BackoffStrategyBase), typeof(IBackoffStrategy), typeof(BackoffStrategies))]
public sealed partial class BackoffStrategies : TypeCollectionBase<BackoffStrategyBase, IBackoffStrategy>
{
}
