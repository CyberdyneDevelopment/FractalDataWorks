namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Request to update an existing calculation definition.
/// </summary>
public sealed class UpdateCalculationDefinitionRequest
{
    /// <summary>
    /// Gets or sets the name of the calculation definition.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the field where the calculation result is stored.
    /// </summary>
    public string ResultFieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type of the calculation result.
    /// </summary>
    public string ResultDataType { get; set; } = "decimal";

    /// <summary>
    /// Gets or sets the calculation formula expression.
    /// </summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the calculation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the calculation is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
}
