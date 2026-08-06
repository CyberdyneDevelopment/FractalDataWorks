using System;
using System.Data;
using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for data type converters - ZERO generic parameters.
/// Follows FDW pattern with constructor parameters (not abstract properties).
/// Always provides DbType metadata for parameter creation.
/// </summary>
public abstract class DataTypeConverterBase(
    int id,
    string name,
    string sourceType,
    Type targetClrType,
    DbType dbType) : TypeOptionBase<int, DataTypeConverterBase>(id, name), IDataTypeConverter
{
    /// <summary>
    /// Gets the source type name (e.g., "int" for SQL, "integer+int64" for JSON Schema).
    /// </summary>
    public string SourceType { get; } = sourceType;

    /// <summary>
    /// Gets the target CLR type.
    /// </summary>
    public Type TargetClrType { get; } = targetClrType;

    /// <summary>
    /// Gets the generic ADO.NET DbType - ALWAYS provided for parameter creation.
    /// </summary>
    public DbType DbType { get; } = dbType;

    /// <summary>
    /// Convert database value to CLR type.
    /// </summary>
    public abstract object? ToClr(object? dbValue);

    /// <summary>
    /// Convert CLR value to database value.
    /// </summary>
    public abstract object? ToDb(object? clrValue);

    /// <summary>
    /// Size for variable-length types. Default: null (not applicable).
    /// </summary>
    public virtual int? Size => null;

    /// <summary>
    /// Precision for numeric types. Default: null (not applicable).
    /// </summary>
    public virtual byte? Precision => null;

    /// <summary>
    /// Scale for numeric types. Default: null (not applicable).
    /// </summary>
    public virtual byte? Scale => null;
}
