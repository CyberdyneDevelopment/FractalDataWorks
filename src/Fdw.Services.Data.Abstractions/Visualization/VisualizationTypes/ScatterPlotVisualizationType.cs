using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Scatter plot visualization type - displays correlation between two numeric dimensions.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(VisualizationTypes), "ScatterPlot", RestrictToCurrentCompilation = true)]
public sealed class ScatterPlotVisualizationType : VisualizationTypeBase
{
    private static readonly IReadOnlyList<Type> SupportedTypes = new[]
    {
        typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ScatterPlotVisualizationType"/> class.
    /// </summary>
    public ScatterPlotVisualizationType()
        : base(5, "ScatterPlot", "Scatter Plot", "mdi-chart-scatter-plot", SupportedTypes)
    {
    }

    /// <inheritdoc/>
    public override bool CanVisualize(IReadOnlyList<string> columnTypes)
    {
        int numericCount = columnTypes.Count(ct => IsNumericType(ct));
        return numericCount >= 2;
    }

    /// <inheritdoc/>
    public override VisualizationConfig GetDefaultConfiguration() => new()
    {
        MaxDataPoints = 1000
    };

    private static bool IsNumericType(string typeName)
        => string.Equals(typeName, "int", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "bigint", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "float", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "decimal", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "numeric", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "money", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "real", StringComparison.OrdinalIgnoreCase);
}
