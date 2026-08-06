using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// StatSet visualization type - displays statistical summary (count, mean, median, etc.).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(VisualizationTypes), "StatSet", RestrictToCurrentCompilation = true)]
public sealed class StatSetVisualizationType : VisualizationTypeBase
{
    private static readonly IReadOnlyList<Type> SupportedTypes = new[]
    {
        typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="StatSetVisualizationType"/> class.
    /// </summary>
    public StatSetVisualizationType()
        : base(7, "StatSet", "Statistical Summary", "mdi-sigma", SupportedTypes)
    {
    }

    /// <inheritdoc/>
    public override bool CanVisualize(IReadOnlyList<string> columnTypes)
        => columnTypes.Any(ct => IsNumericType(ct));

    /// <inheritdoc/>
    public override VisualizationConfig GetDefaultConfiguration() => new()
    {
        ShowLegend = false,
        ShowAxisLabels = false,
        MaxDataPoints = 10000
    };

    private static bool IsNumericType(string typeName)
        => string.Equals(typeName, "int", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "bigint", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "float", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "decimal", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "numeric", StringComparison.OrdinalIgnoreCase)
        || string.Equals(typeName, "money", StringComparison.OrdinalIgnoreCase);
}
