namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Request to validate a formula expression against a target DataSet.
/// </summary>
public sealed class ValidateFormulaPayload
{
    /// <summary>
    /// Gets or sets the formula expression to validate.
    /// </summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the target DataSet to validate the formula against.
    /// </summary>
    public string TargetDataSet { get; set; } = string.Empty;
}
