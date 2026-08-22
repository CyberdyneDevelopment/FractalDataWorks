using Fdw.Data;
using Fdw.Data.Abstractions;
using Xunit;

namespace Fdw.Commands.Data.Extensions.Tests;

public sealed class DataTypeCollectionFixture
{
    public DataTypeCollectionFixture()
    {
        _ = SortDirections.All();
        _ = FilterOperators.All();
    }
}

[CollectionDefinition(nameof(DataTestCollection))]
public sealed class DataTestCollection : ICollectionFixture<DataTypeCollectionFixture>
{
}
