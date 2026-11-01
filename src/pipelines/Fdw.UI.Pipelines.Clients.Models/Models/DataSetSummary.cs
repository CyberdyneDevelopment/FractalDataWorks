namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Summary of a dataset for display in editor dropdowns.
/// </summary>
public sealed class DataSetSummary
{
    /// <summary>
    /// Gets or sets the dataset name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional description of the dataset.
    /// </summary>
    public string? Description { get; set; }
}
