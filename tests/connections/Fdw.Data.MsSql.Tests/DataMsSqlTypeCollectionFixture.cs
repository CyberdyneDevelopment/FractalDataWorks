using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql.Tests.Translators;
using Xunit;

namespace Fdw.Data.MsSql.Tests;

public sealed class DataMsSqlTypeCollectionFixture
{
    public DataMsSqlTypeCollectionFixture()
    {
        _ = JoinTypes.All();
        _ = FilterOperators.All();
        _ = SortDirections.All();
        _ = ContainerTypes.All();
    }
}

[CollectionDefinition(nameof(DataMsSqlTestCollection))]
public sealed class DataMsSqlTestCollection : ICollectionFixture<DataMsSqlTypeCollectionFixture>
{
}
