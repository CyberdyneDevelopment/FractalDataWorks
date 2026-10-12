using System.Collections.Generic;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Response containing the results of a formula preview, including validation and sample output.
/// </summary>
public sealed class PreviewFormulaResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the formula is syntactically and semantically valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the primary error message if the formula is invalid, or null if valid.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the collection of validation error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of field names referenced by the formula.
    /// </summary>
    public IReadOnlyList<string> ReferencedFields { get; set; } = [];

    /// <summary>
    /// Gets or sets the inferred result data type of the formula, or null if it cannot be determined.
    /// </summary>
    public string? InferredResultType { get; set; }

    /// <summary>
    /// Gets or sets the sample output data rows, or null if no preview data is available.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<object>>? SampleData { get; set; }

    /// <summary>
    /// Gets or sets the column names for the sample data, or null if no preview data is available.
    /// </summary>
    public IReadOnlyList<string>? ColumnNames { get; set; }
}
