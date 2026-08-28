using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Base class for chart type options using the CRTP pattern.
/// </summary>
/// <remarks>
/// Inherit from this class and apply <c>[TypeOption(typeof(ChartTypes), "YourName")]</c>
/// to register a new chart type. Encoding requirement lists and icon hint are set via
/// constructor arguments — no property overrides.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class ChartTypeBase : TypeOptionBase<int, ChartTypeBase>, IChartType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChartTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique numeric identifier for this chart type.</param>
    /// <param name="name">The registry name (used by <c>ChartTypes.ByName()</c>).</param>
    /// <param name="displayName">The human-readable name shown in the chart-type picker.</param>
    /// <param name="category">The group for organising the chart-type picker (e.g. "Comparison").</param>
    /// <param name="iconHint">The icon hint string passed to the renderer.</param>
    /// <param name="requiredEncodings">Encoding role names that MUST be bound.</param>
    /// <param name="optionalEncodings">Encoding role names that may optionally be bound.</param>
    protected ChartTypeBase(
        int id,
        string name,
        string displayName,
        string category,
        string iconHint,
        IReadOnlyList<string>? requiredEncodings = null,
        IReadOnlyList<string>? optionalEncodings = null)
        : base(id, name, name, displayName, displayName, category)
    {
        IconHint = iconHint;
        RequiredEncodings = requiredEncodings ?? [];
        OptionalEncodings = optionalEncodings ?? [];
    }

    /// <inheritdoc />
    public string IconHint { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> RequiredEncodings { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> OptionalEncodings { get; }
}
