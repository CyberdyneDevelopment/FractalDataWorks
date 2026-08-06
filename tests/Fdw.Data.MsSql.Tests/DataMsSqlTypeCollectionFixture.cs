using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
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

        // Why: TestKvpRow is a [GenerateMapper] POCO defined in this test assembly — the
        // TypeOptionModuleInitializerGenerator only auto-registers TypeOptions from REFERENCED
        // assemblies, so an in-assembly mapper needs manual registration (same requirement as
        // Fdw.Services.Tests's ServicesTypeCollectionFixture).
        PocoMapperCollection.RegisterMember(new TestKvpRowPocoMapper());
    }
}

[CollectionDefinition(nameof(DataMsSqlTestCollection))]
public sealed class DataMsSqlTestCollection : ICollectionFixture<DataMsSqlTypeCollectionFixture>
{
}
