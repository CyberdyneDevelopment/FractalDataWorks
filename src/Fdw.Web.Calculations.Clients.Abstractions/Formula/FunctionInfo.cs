using System.Collections.Generic;

namespace Fdw.Web.Calculations.Clients.Formula;

/// <summary>
/// Describes a built-in or user-defined function available in formula expressions.
/// </summary>
public class FunctionInfo
{
    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the function category (e.g., "Aggregate", "Math", "String").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the syntax signature for the function.
    /// </summary>
    public string Syntax { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the function does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the return type of the function.
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of parameters the function accepts.
    /// </summary>
    public IReadOnlyList<FunctionParameter> Parameters { get; set; } = [];
}
