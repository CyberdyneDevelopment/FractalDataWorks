using System;
using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a single field definition within a data schema.
/// </summary>
/// <remarks>
/// ISchemaField describes the characteristics of a data field including
/// its type, constraints, and validation rules. This information is used
/// for data validation, type conversion, and query optimization.
/// </remarks>
public interface ISchemaField
{
    /// <summary>
    /// Gets the field name.
    /// </summary>
    /// <value>The name of the field as it appears in the data.</value>
    string Name { get; }

    /// <summary>
    /// Gets the field's display name.
    /// </summary>
    /// <value>A human-readable name for UI purposes, or the field name if not specified.</value>
    string DisplayName { get; }

    /// <summary>
    /// Gets the field's data type.
    /// </summary>
    /// <value>The .NET type that values in this field should conform to.</value>
    Type DataType { get; }

    /// <summary>
    /// Gets a value indicating whether this field is required.
    /// </summary>
    /// <value><c>true</c> if the field must have a non-null value; otherwise, <c>false</c>.</value>
    bool IsRequired { get; }

    /// <summary>
    /// Gets a value indicating whether this field should be indexed.
    /// </summary>
    /// <value><c>true</c> if the field should be indexed for performance; otherwise, <c>false</c>.</value>
    bool IsIndexed { get; }

    /// <summary>
    /// Gets the maximum length for string fields.
    /// </summary>
    /// <value>The maximum string length, or null if not applicable or unlimited.</value>
    int? MaxLength { get; }

    /// <summary>
    /// Gets the default value for this field.
    /// </summary>
    /// <value>The default value to use when the field is not provided, or null if no default.</value>
    object? DefaultValue { get; }

    /// <summary>
    /// Gets the field description.
    /// </summary>
    /// <value>A description of the field's purpose and content.</value>
    string? Description { get; }

    /// <summary>
    /// Gets validation constraints for this field.
    /// </summary>
    /// <value>A collection of validation rules that values must satisfy.</value>
    IReadOnlyList<IFieldConstraint> Constraints { get; }

    /// <summary>
    /// Gets metadata about this field.
    /// </summary>
    /// <value>Additional properties and configuration information.</value>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Validates a value for this field.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>A result indicating whether the value is valid for this field.</returns>
    /// <remarks>
    /// This method performs comprehensive field-level validation including
    /// type checking, constraint validation, and format validation.
    /// </remarks>
    IGenericResult ValidateValue(object? value);

    /// <summary>
    /// Attempts to convert a value to the field's data type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A result containing the converted value, or failure if conversion is not possible.</returns>
    /// <remarks>
    /// This method handles type conversion with appropriate error handling.
    /// It considers the field's data type and any format-specific conversion rules.
    /// </remarks>
    IGenericResult<object?> ConvertValue(object? value);
}
