using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Line chart visualization type - displays data as connected data points over a sequence.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(VisualizationTypes), "LineChart", RestrictToCurrentCompilation = true)]
public sealed class LineChartVisualizationType : VisualizationTypeBase
{
    private static readonly IReadOnlyList<Type> SupportedTypes = new[]
    {
        typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal), typeof(DateTime)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="LineChartVisualizationType"/> class.
    /// </summary>
    public LineChartVisualizationType()
        : base(3, "LineChart", "Line Chart", "mdi-chart-line", SupportedTypes)
    {
    }

    /// <inheritdoc/>
    public override bool CanVisualize(IReadOnlyList<string> columnTypes)
        => columnTypes.Any(ct => IsNumericOrDateType(ct));

    /// <inheritdoc/>
    public override VisualizationConfig GetDefaultConfiguration() => new()
    {
        MaxDataPoints = 500
    };

    private static bool IsNumericOrDateType(string typeName)
        => string.Equals(typeName, "int", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "bigint", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "float", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "decimal", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "numeric", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "datetime", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "datetime2", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "date", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "money", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "real", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "smallint", StringComparison.OrdinalIgnoreCase);
}
