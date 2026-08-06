using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>
/// Base class for POCO mappers.
/// Provides default implementation structure for reflection-free mapping.
/// Key is the fully-qualified type name for lookup.
/// </summary>
public abstract class PocoMapperBase : TypeOptionBase<string, PocoMapperBase>, IPocoMapper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PocoMapperBase"/> class.
    /// </summary>
    /// <param name="typeFullName">The fully-qualified type name used as the lookup key.</param>
    /// <param name="targetType">The target .NET type this mapper creates.</param>
    protected PocoMapperBase(string typeFullName, Type targetType)
        : base(typeFullName, targetType.Name)
    {
        TargetType = targetType;
    }

    /// <inheritdoc/>
    public Type TargetType { get; }

    /// <inheritdoc/>
    public abstract IGenericResult<object> MapFromReader(DbDataReader reader, IStorageContainer container);

    /// <inheritdoc/>
    public abstract IGenericResult<object> MapFromDictionary(IDictionary<string, object?> data);

    /// <inheritdoc/>
    public abstract IReadOnlyList<string> GetPropertyNames();

    /// <inheritdoc/>
    public abstract IReadOnlyDictionary<string, object?> MapToParameters(object instance);

    /// <inheritdoc/>
    public abstract void SetValue(object instance, string columnName, object? value);

    /// <inheritdoc/>
    public abstract IList CreateList();

    /// <inheritdoc/>
    public abstract Array CreateArray(int length);

    /// <inheritdoc/>
    public abstract IGenericConfiguration? GetTypedBody(object parent);

    /// <inheritdoc/>
    public abstract void SetTypedBody(object parent, IGenericConfiguration? body);

    /// <inheritdoc/>
    public abstract IReadOnlyList<IChildCascadeDescriptor> CascadeChildren { get; }
}
