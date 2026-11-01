using Fdw.Data;
using Fdw.Data.Abstractions;
using Xunit;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// Warms up TypeCollections required by the shared-base translator tests.
/// Mirror of DataMsSqlTypeCollectionFixture — same warmup, different collection name.
/// </summary>
public sealed class SqlTranslatorTestFixture
{
    public SqlTranslatorTestFixture()
    {
        _ = JoinTypes.All();
        _ = FilterOperators.All();
        _ = SortDirections.All();
        _ = ContainerTypes.All();
    }
}

[CollectionDefinition(nameof(SqlTranslatorTestCollection))]
public sealed class SqlTranslatorTestCollection : ICollectionFixture<SqlTranslatorTestFixture>
{
}
