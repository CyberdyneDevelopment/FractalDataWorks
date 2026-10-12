using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a validation constraint for a schema field.
/// </summary>
/// <remarks>
/// IFieldConstraint defines validation rules that field values must satisfy.
/// Different constraint types provide different validation behaviors such as
/// range checking, pattern matching, and custom validation logic.
/// </remarks>
public interface IFieldConstraint
{
    /// <summary>
    /// Gets the constraint type identifier.
    /// </summary>
    /// <value>The type of constraint (e.g., "Range", "Pattern", "Custom").</value>
    string ConstraintType { get; }

    /// <summary>
    /// Gets the constraint parameters.
    /// </summary>
    /// <value>Parameters that configure the constraint behavior.</value>
    IReadOnlyDictionary<string, object> Parameters { get; }

    /// <summary>
    /// Gets the error message to display when validation fails.
    /// </summary>
    /// <value>A human-readable error message describing the constraint violation.</value>
    string ErrorMessage { get; }

    /// <summary>
    /// Validates a value against this constraint.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="fieldName">The name of the field being validated.</param>
    /// <returns>A result indicating whether the value satisfies the constraint.</returns>
    /// <remarks>
    /// This method performs the actual constraint validation logic.
    /// The field name is provided for context in error messages.
    /// </remarks>
    IGenericResult ValidateValue(object? value, string fieldName);
}
