using System.Collections.Generic;

namespace Fdw.Services.Data.Abstractions.Visualization;

/// <summary>
/// Response model for listing available visualization types.
/// </summary>
public sealed class VisualizationTypeListResponse
{
    /// <summary>
    /// Gets or sets the available visualization types.
    /// </summary>
    public IReadOnlyList<VisualizationTypeItem> Types { get; set; } = [];
}
