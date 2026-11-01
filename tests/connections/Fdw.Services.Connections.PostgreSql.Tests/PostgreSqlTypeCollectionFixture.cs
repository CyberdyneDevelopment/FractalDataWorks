using Fdw.Data;
using Fdw.Data.Abstractions;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests;

public sealed class PostgreSqlTypeCollectionFixture
{
    public PostgreSqlTypeCollectionFixture()
    {
        _ = JoinTypes.All();
        _ = FilterOperators.All();
        _ = SortDirections.All();
        _ = ContainerTypes.All();
    }
}

[CollectionDefinition(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlTestCollection : ICollectionFixture<PostgreSqlTypeCollectionFixture>
{
}
