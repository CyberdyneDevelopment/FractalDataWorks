using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Data.DataContainers.Abstractions.Results;
using Fdw.Results;

namespace Fdw.Data.DataContainers.Abstractions;

/// <summary>
/// Represents a field in a schema.
/// </summary>
public class SchemaField : ISchemaField
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaField"/> class.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="dataType">The field data type.</param>
    /// <param name="ordinal">The ordinal position.</param>
    public SchemaField(string name, Type dataType, int ordinal)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        Ordinal = ordinal;
    }

    /// <summary>
    /// Gets the field name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the display name for UI purposes.
    /// </summary>
    public string DisplayName => Name;

    /// <summary>
    /// Gets the field's data type.
    /// </summary>
    public Type DataType { get; }

    /// <summary>
    /// Gets the ordinal position of this field in the schema.
    /// </summary>
    public int Ordinal { get; }

    /// <summary>
    /// Gets a value indicating whether this field is required.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Gets a value indicating whether this field should be indexed.
    /// </summary>
    public bool IsIndexed { get; init; }

    /// <summary>
    /// Gets the maximum length for string fields.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Gets the default value for this field.
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Gets the field description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets validation constraints for this field.
    /// </summary>
    public IReadOnlyList<IFieldConstraint> Constraints { get; init; } = Array.Empty<IFieldConstraint>();

    /// <summary>
    /// Gets metadata about this field.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>(StringComparer.Ordinal);

    /// <summary>
    /// Validates a value for this field.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>A result indicating whether the value is valid.</returns>
    public IGenericResult ValidateValue(object? value)
    {
        // Basic validation
        if (value == null)
        {
            return IsRequired
                ? GenericResult.Failure(
                    DataContainerResultCodes.ByName("FieldRequired"),
                    ResultDetails.Create("FieldName", Name))
                : GenericResult.Success();
        }

        if (!DataType.IsInstanceOfType(value))
        {
            return GenericResult.Failure(
                DataContainerResultCodes.ByName("FieldTypeMismatch"),
                ResultDetails.Create("FieldName", Name, "ExpectedType", DataType.Name, "ActualType", value.GetType().Name));
        }

        return GenericResult.Success();
    }

    /// <summary>
    /// Attempts to convert a value to the field's data type.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A result containing the converted value or failure.</returns>
    public IGenericResult<object?> ConvertValue(object? value)
    {
        if (value == null)
        {
            return IsRequired
                ? GenericResult<object?>.Failure(
                    DataContainerResultCodes.ByName("FieldRequired"),
                    ResultDetails.Create("FieldName", Name))
                : GenericResult<object?>.Success(null);
        }

        try
        {
            if (DataType.IsInstanceOfType(value))
                return GenericResult<object?>.Success(value);

            var converted = Convert.ChangeType(value, DataType, System.Globalization.CultureInfo.InvariantCulture);
            return GenericResult<object?>.Success(converted);
        }
        catch (Exception ex)
        {
            return GenericResult<object?>.Failure(
                DataContainerResultCodes.ByName("FieldConversionFailed"),
                ResultDetails.Create("TargetType", DataType.Name, "ErrorMessage", ex.Message));
        }
    }
}