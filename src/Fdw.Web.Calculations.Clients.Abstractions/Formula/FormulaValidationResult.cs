using System.Collections.Generic;

namespace Fdw.Web.Calculations.Clients.Formula;

/// <summary>
/// Result of a formula validation operation.
/// </summary>
public class FormulaValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the formula is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the list of validation errors.
    /// </summary>
    public IReadOnlyList<FormulaError> Errors { get; set; } = [];

    /// <summary>
    /// Gets or sets the fields referenced by the formula.
    /// </summary>
    public IReadOnlyList<string> ReferencedFields { get; set; } = [];

    /// <summary>
    /// Gets or sets the inferred result type of the formula, if determinable.
    /// </summary>
    public string? InferredResultType { get; set; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static FormulaValidationResult Success(IReadOnlyList<string> referencedFields, string? resultType) =>
        new()
        {
            IsValid = true,
            ReferencedFields = referencedFields,
            InferredResultType = resultType
        };

    /// <summary>
    /// Creates a failed validation result with a list of errors.
    /// </summary>
    public static FormulaValidationResult Failure(IReadOnlyList<FormulaError> errors) =>
        new()
        {
            IsValid = false,
            Errors = errors
        };

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    public static FormulaValidationResult Failure(FormulaError error) =>
        new()
        {
            IsValid = false,
            Errors = [error]
        };
}
