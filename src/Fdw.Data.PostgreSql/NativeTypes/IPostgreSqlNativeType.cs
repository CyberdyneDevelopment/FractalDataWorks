using Fdw.Collections;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Marker interface for PostgreSQL native type options within the
/// <see cref="PostgreSqlNativeTypes"/> TypeCollection.
/// </summary>
public interface IPostgreSqlNativeType : ITypeOption<int, PostgreSqlNativeTypeBase>
{
}
