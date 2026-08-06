using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// CRTP base class for abstract data type options in the <see cref="DataTypes"/> TypeCollection.
/// </summary>
/// <remarks>
/// Each concrete subclass represents one portable abstract type (e.g., <c>Int64</c>, <c>String</c>).
/// Storage-specific layers map their native types (e.g., SQL Server <c>bigint</c>,
/// PostgreSQL <c>int8</c>) to the appropriate <see cref="DataTypeBase"/> instance.
/// </remarks>
public abstract class DataTypeBase : TypeOptionBase<int, DataTypeBase>, IDataType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataTypeBase"/> class.
    /// </summary>
    /// <param name="id">Unique numeric identifier within the <see cref="DataTypes"/> collection.</param>
    /// <param name="name">Canonical name of the abstract type (e.g., "Int64", "String").</param>
    protected DataTypeBase(int id, string name)
        : base(id, name)
    {
    }
}
