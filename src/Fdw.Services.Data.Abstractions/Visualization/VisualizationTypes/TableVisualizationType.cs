using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Table visualization type - displays data in tabular format. This is the default visualization.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(VisualizationTypes), "Table", RestrictToCurrentCompilation = true)]
public sealed class TableVisualizationType : VisualizationTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableVisualizationType"/> class.
    /// </summary>
    public TableVisualizationType()
        : base(1, "Table", "Table", "mdi-table", Array.Empty<Type>())
    {
    }

    /// <inheritdoc/>
    public override bool CanVisualize(IReadOnlyList<string> columnTypes) => true;

    /// <inheritdoc/>
    public override VisualizationConfig GetDefaultConfiguration() => new()
    {
        ShowLegend = false,
        ShowAxisLabels = false,
        MaxDataPoints = 500
    };
}
