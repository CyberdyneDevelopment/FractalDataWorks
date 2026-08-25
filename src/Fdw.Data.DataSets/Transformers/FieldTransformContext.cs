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
    /// Gets the configured parameter values for the transform step being executed, keyed by
    /// parameter name, as stored in <c>transform.FieldMappingTransformParameter</c>.
    /// </summary>
    /// <remarks>
    /// Why the context.Parameters live on the context rather than in their own argument: a transform needs
    /// exactly two things beyond the value it is given - what it was configured with, and what it is
    /// running inside. Splitting those across two arguments produced a second, parameterless calling
    /// convention, and the one shim that bridged them supplied an empty bag, so every transform
    /// reached through it ran unconfigured. With one context there is nothing to bridge.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the cancellation token for this transform execution.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}
