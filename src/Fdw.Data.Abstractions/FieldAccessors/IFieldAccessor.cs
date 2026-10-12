using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Data.Abstractions.FieldAccessors;

/// <summary>
/// Provides compile-time property/field access for POCO types.
/// Part of the FieldAccessors TypeCollection for reflection-free field value extraction.
/// Key is the type name for O(1) lookup.
/// </summary>
public interface IFieldAccessor : ITypeOption<string, FieldAccessorBase>
{
    /// <summary>
    /// The .NET type this accessor provides access to.
    /// </summary>
    Type TargetType { get; }

    /// <summary>
    /// Gets the names of all accessible fields/properties on the target type.
    /// </summary>
    IReadOnlyList<string> FieldNames { get; }

    /// <summary>
    /// Gets a field/property value from an instance by name.
    /// </summary>
    /// <param name="instance">The object instance to extract the value from.</param>
    /// <param name="fieldName">The name of the field/property to access.</param>
    /// <returns>Result containing the value (possibly null) or failure information.</returns>
    IGenericResult<object?> GetValue(object instance, string fieldName);

    /// <summary>
    /// Gets a field/property value as decimal from an instance by name.
    /// Performs type conversion for numeric types.
    /// </summary>
    /// <param name="instance">The object instance to extract the value from.</param>
    /// <param name="fieldName">The name of the field/property to access.</param>
    /// <returns>Result containing the decimal value or failure information.</returns>
    IGenericResult<decimal> GetDecimalValue(object instance, string fieldName);
}
