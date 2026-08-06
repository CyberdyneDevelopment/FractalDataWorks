namespace Fdw.Web.Calculations.Clients.Formula;

/// <summary>
/// Represents an error found during formula validation.
/// </summary>
public class FormulaError
{
    /// <summary>
    /// Gets or sets the line number (1-based) where the error occurs.
    /// </summary>
    public int Line { get; set; } = 1;

    /// <summary>
    /// Gets or sets the column number (1-based) where the error occurs.
    /// </summary>
    public int Column { get; set; } = 1;

    /// <summary>
    /// Gets or sets the length of the erroneous token or expression.
    /// </summary>
    public int Length { get; set; } = 1;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity of the error.
    /// </summary>
    public IFormulaErrorSeverity Severity { get; set; } = FormulaErrorSeverities.Error;

    /// <summary>
    /// Gets or sets an optional suggested fix for the error.
    /// </summary>
    public string? SuggestedFix { get; set; }
}
