using Fdw.Services.Pipelines.Abstractions.DataDestination;
using Fdw.Services.Pipelines.Abstractions.DataSource;
using Fdw.Services.Pipelines.Abstractions.WriteMode;
using Xunit;

namespace Fdw.Services.Pipelines.Abstractions.Tests;

/// <summary>
/// Fixture that ensures pipeline TypeCollections are fully initialized
/// before any tests run. This prevents race conditions in the source-generated
/// EnsureFrozen() method when xUnit runs tests in parallel.
/// </summary>
public sealed class PipelinesTypeCollectionFixture
{
    public PipelinesTypeCollectionFixture()
    {
        _ = DataDestinationKinds.All();
        _ = DataSourceKinds.All();
        _ = WriteModes.All();
    }
}

/// <summary>
/// Collection definition that applies the PipelinesTypeCollectionFixture to all tests.
/// </summary>
[CollectionDefinition(nameof(PipelinesTestCollection))]
public sealed class PipelinesTestCollection : ICollectionFixture<PipelinesTypeCollectionFixture>
{
}
