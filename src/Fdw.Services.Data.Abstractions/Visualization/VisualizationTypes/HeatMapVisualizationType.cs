using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Heat map visualization type - displays data intensity using color gradients.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(VisualizationTypes), "HeatMap", RestrictToCurrentCompilation = true)]
public sealed class HeatMapVisualizationType : VisualizationTypeBase
{
    private static readonly IReadOnlyList<Type> SupportedTypes = new[]
    {
        typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="HeatMapVisualizationType"/> class.
    /// </summary>
    public HeatMapVisualizationType()
        : base(6, "HeatMap", "Heat Map", "mdi-gradient-horizontal", SupportedTypes)
    {
    }

    /// <inheritdoc/>
    public override bool CanVisualize(IReadOnlyList<string> columnTypes)
        => columnTypes.Any(ct => IsNumericType(ct)) && columnTypes.Count >= 2;

    /// <inheritdoc/>
    public override VisualizationConfig GetDefaultConfiguration() => new()
    {
        MaxDataPoints = 500,
        ShowLegend = true
    };

    private static bool IsNumericType(string typeName)
        => string.Equals(typeName, "int", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "bigint", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "float", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "decimal", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "numeric", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "money", StringComparison.OrdinalIgnoreCase);
}
