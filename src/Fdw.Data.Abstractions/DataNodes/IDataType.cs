using System;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type domain that normalizes storage-native types across different backends.
/// </summary>
/// <remarks>
/// Each storage backend maps its native types (e.g., SQL Server <c>bigint</c>,
/// PostgreSQL <c>int8</c>) to a shared abstract type (e.g., <c>Int64</c>) so that
/// DataSet field definitions remain portable across connection types.
/// <para>
/// Extends <see cref="ITypeOption{TKey,TBase}"/> so all abstract types are enumerable
/// via the <see cref="DataTypes"/> TypeCollection.
/// </para>
/// </remarks>
public interface IDataType : ITypeOption<int, DataTypeBase>
{
    /// <summary>
    /// Gets the canonical name of this abstract type (e.g., "Int64", "String", "Decimal").
    /// </summary>
    new string Name { get; }

    /// <summary>Gets the CLR type of the values this type reads: what <see cref="Convert"/> hands back.</summary>
    Type ClrType { get; }

    /// <summary>
    /// Gets the fully qualified name of <see cref="ClrType"/>, such as <c>System.Int32</c>: where the type comes from.
    /// </summary>
    /// <remarks>
    /// A field states its type by <see cref="Name"/> and nothing stores this value. It is here for a reader that
    /// wants to know which .NET type a name stands for.
    /// </remarks>
    string FullName { get; }

    /// <summary>
    /// Gets whether a value of this type has a text form that can be placed in an expression (a filter's <c>{{name}}</c>
    /// reference). Binary does not.
    /// </summary>
    bool RendersInExpressions { get; }

    /// <summary>
    /// Converts a value to this type's CLR value: text is parsed with the invariant culture, and a value already of this type's <see cref="ClrType"/> is kept as it is.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>
    /// The value as <see cref="ClrType"/>, or a failure when it is not a value of this type. A value that does not convert
    /// is refused; no value is ever invented for it.
    /// </returns>
    IGenericResult<object> Convert(object? value);

    /// <summary>Writes a value of this type as invariant text, the form that <see cref="Convert"/> reads back.</summary>
    /// <param name="value">A value of this type's <see cref="ClrType"/>.</param>
    /// <returns>The text.</returns>
    string Format(object value);
}
