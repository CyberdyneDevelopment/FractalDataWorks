using Fdw.Data;
using Fdw.Data.Abstractions;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests;

public sealed class PostgreSqlTypeCollectionFixture
{
    public PostgreSqlTypeCollectionFixture()
    {
        // Why: TypeCollections must be warmed up so ByName/ById return the correct
        // type options — not the NotFound sentinel. Mirrors DataMsSqlTypeCollectionFixture.
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
