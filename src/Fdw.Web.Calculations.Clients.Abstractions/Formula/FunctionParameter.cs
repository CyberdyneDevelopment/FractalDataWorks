namespace Fdw.Web.Calculations.Clients.Formula;

/// <summary>
/// Describes a single parameter of a formula function.
/// </summary>
public class FunctionParameter
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of the parameter.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the parameter is optional.
    /// </summary>
    public bool IsOptional { get; set; }
}
