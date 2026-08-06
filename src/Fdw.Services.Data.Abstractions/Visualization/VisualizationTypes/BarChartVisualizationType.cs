using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Bar chart visualization type - displays data as vertical or horizontal bars.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(VisualizationTypes), "BarChart", RestrictToCurrentCompilation = true)]
public sealed class BarChartVisualizationType : VisualizationTypeBase
{
    private static readonly IReadOnlyList<Type> SupportedTypes = new[]
    {
        typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="BarChartVisualizationType"/> class.
    /// </summary>
    public BarChartVisualizationType()
        : base(2, "BarChart", "Bar Chart", "mdi-chart-bar", SupportedTypes)
    {
    }

    /// <inheritdoc/>
    public override bool CanVisualize(IReadOnlyList<string> columnTypes)
        => columnTypes.Any(ct => IsNumericType(ct));

    /// <inheritdoc/>
    public override VisualizationConfig GetDefaultConfiguration() => new()
    {
        MaxDataPoints = 100
    };

    private static bool IsNumericType(string typeName)
        => string.Equals(typeName, "int", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "bigint", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "float", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "decimal", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "numeric", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "money", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "real", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "smallint", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "tinyint", StringComparison.OrdinalIgnoreCase);
}
