using Fdw.Collections;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// CRTP base class for PostgreSQL native type options in the <see cref="PostgreSqlNativeTypes"/> TypeCollection.
/// </summary>
/// <remarks>
/// Each concrete subclass represents one PostgreSQL native type (e.g., <c>int8</c>, <c>text</c>)
/// and exposes the abstract <see cref="IDataType"/> it maps to, enabling portable DataSet field definitions.
/// </remarks>
public abstract class PostgreSqlNativeTypeBase : TypeOptionBase<int, PostgreSqlNativeTypeBase>, IPostgreSqlNativeType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlNativeTypeBase"/> class.
    /// </summary>
    /// <param name="id">Unique numeric identifier within the <see cref="PostgreSqlNativeTypes"/> collection.</param>
    /// <param name="name">PostgreSQL native type name (e.g., "int8", "text").</param>
    protected PostgreSqlNativeTypeBase(int id, string name)
        : base(id, name)
    {
    }

    /// <summary>
    /// Gets the portable abstract data type this PostgreSQL native type maps to.
    /// </summary>
    public abstract IDataType AbstractType { get; }
}
