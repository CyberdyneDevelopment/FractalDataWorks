using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Sparkline visualization type - compact inline chart for trend indication.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(VisualizationTypes), "Sparkline", RestrictToCurrentCompilation = true)]
public sealed class SparklineVisualizationType : VisualizationTypeBase
{
    private static readonly IReadOnlyList<Type> SupportedTypes = new[]
    {
        typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SparklineVisualizationType"/> class.
    /// </summary>
    public SparklineVisualizationType()
        : base(8, "Sparkline", "Sparkline", "mdi-chart-timeline-variant", SupportedTypes)
    {
    }

    /// <inheritdoc/>
    public override bool CanVisualize(IReadOnlyList<string> columnTypes)
        => columnTypes.Any(ct => IsNumericType(ct));

    /// <inheritdoc/>
    public override VisualizationConfig GetDefaultConfiguration() => new()
    {
        MaxDataPoints = 200,
        ShowLegend = false,
        ShowAxisLabels = false
    };

    private static bool IsNumericType(string typeName)
        => string.Equals(typeName, "int", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "bigint", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "float", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "decimal", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "numeric", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "money", StringComparison.OrdinalIgnoreCase);
}
