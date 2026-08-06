using Fdw.Collections;

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
}
