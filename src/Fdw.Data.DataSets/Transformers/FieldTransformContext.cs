using System;
using System.Collections.Generic;
using System.Threading;

namespace Fdw.Data.DataSets;

/// <summary>
/// Runtime state available to all field transforms during execution.
/// Provides access to the current record, operating date, and execution timestamp.
/// </summary>
public sealed class FieldTransformContext
{
    /// <summary>
    /// Gets the operating date for this transform execution.
    /// </summary>
    public DateOnly OperatingDate { get; init; }

    /// <summary>
    /// Gets the timestamp when this transform execution started.
    /// </summary>
    public DateTimeOffset ExecutionTimestamp { get; init; }

    /// <summary>
    /// Gets the current record's field values, keyed by logical field name.
    /// Transforms that reference other fields (e.g., FallbackFromField, ConditionalDivide)
    /// read sibling values from this dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, object?> CurrentRecord { get; init; } = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the cancellation token for this transform execution.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}
