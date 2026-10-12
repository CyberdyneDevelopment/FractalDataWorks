namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Request to preview a formula expression against a target DataSet.
/// </summary>
public sealed class PreviewFormulaRequest
{
    /// <summary>
    /// Gets or sets the formula expression to preview.
    /// </summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the target DataSet to evaluate the formula against.
    /// </summary>
    public string TargetDataSet { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of rows to return in the preview.
    /// </summary>
    public int MaxRows { get; set; } = 5;
}
