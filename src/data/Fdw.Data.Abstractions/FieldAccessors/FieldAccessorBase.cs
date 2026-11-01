using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Data.Abstractions.FieldAccessors;

/// <summary>
/// Base class for field accessors.
/// Provides reflection-free field/property access for POCO types.
/// Key is the type name for O(1) lookup via FieldAccessorCollection.
/// </summary>
public abstract class FieldAccessorBase : TypeOptionBase<string, FieldAccessorBase>, IFieldAccessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldAccessorBase"/> class.
    /// </summary>
    /// <param name="typeName">The type name used as the lookup key.</param>
    /// <param name="targetType">The target .NET type this accessor provides access to.</param>
    protected FieldAccessorBase(string typeName, Type targetType)
        : base(typeName, typeName)
    {
        TargetType = targetType;
    }

    /// <inheritdoc/>
    public Type TargetType { get; }

    /// <inheritdoc/>
    public abstract IReadOnlyList<string> FieldNames { get; }

    /// <inheritdoc/>
    public abstract IGenericResult<object?> GetValue(object instance, string fieldName);

    /// <inheritdoc/>
    public abstract IGenericResult<decimal> GetDecimalValue(object instance, string fieldName);
}
