using System;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Builders;
using Fdw.Services.Data.DataNodes;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataNodes.Tests;

/// <summary>
/// Tests the store-to-path back-reference produced by <c>DataStoreBuilderBase.Build</c>.
/// <see cref="IDataNodePath.Store"/> is declared NON-nullable (and enforced as such by
/// <c>DetachedDataPath</c>), but both runtime <c>DataPath</c> construction sites passed
/// <c>store: null!</c> and nothing back-wired it, so <c>Store</c> was null on every path the builder
/// produced. Because <c>DataPath.ContainerNotFoundResult</c> reads <c>Store.Name</c> into its
/// ResultDetails, a container MISS threw a NullReferenceException instead of returning a failure
/// result — the fail-loud helper failing in the one way it must never fail.
/// </summary>
public sealed class DataStoreBackReferenceTests
{
    private const string StoreName = "TestStore";

    // Why: two paths with a container in each mirrors the real shape that exposed the defect —
    // ImplementationConfigurationProviderBase.FindForeignKey probes path 'data' for container 'Connection'
    // (which lives in path 'conn') as its deliberate parent-FK-vs-data-FK test, so a cross-path MISS
    // is a normal control-flow event on every boot, not an error condition.
    private static DataStoreConfiguration CreateStoreConfig()
    {
        var storeId = Guid.NewGuid();
        var connPathId = Guid.NewGuid();
        var dataPathId = Guid.NewGuid();

        return new DataStoreConfiguration
        {
            Id = storeId,
            Name = StoreName,
            Paths =
            [
                new DataPathConfiguration
                {
                    Id = connPathId,
                    Name = "conn",
                    DataStoreId = storeId,
                    Containers =
                    [
                        new DataContainerConfiguration
                        {
                            Id = Guid.NewGuid(),
                            Name = "Connection",
                            DataPathId = connPathId,
                            Fields = [],
                        },
                    ],
                },
                new DataPathConfiguration
                {
                    Id = dataPathId,
                    Name = "data",
                    DataStoreId = storeId,
                    Containers =
                    [
                        new DataContainerConfiguration
                        {
                            Id = Guid.NewGuid(),
                            Name = "DataStore",
                            DataPathId = dataPathId,
                            Fields = [],
                        },
                    ],
                },
            ],
        };
    }

    private static async Task<IDataStore> BuildStore()
    {
        var builder = new GenericDataStoreBuilder(FormatTypes.ByName("Json"));
        builder.Configure(CreateStoreConfig()).IsSuccess.ShouldBeTrue();

        var buildResult = await builder.Build(TestContext.Current.CancellationToken);
        buildResult.IsSuccess.ShouldBeTrue();
        buildResult.Value.ShouldNotBeNull();

        return buildResult.Value;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuiltPathCarriesOwningStoreBackReference()
    {
        // Arrange / Act
        var store = await BuildStore();

        // Assert — every path points back at the SAME store instance that owns it.
        store.Paths.Count.ShouldBe(2);
        foreach (var path in store.Paths)
        {
            path.Store.ShouldNotBeNull();
            path.Store.ShouldBeSameAs(store);
            path.Store.Name.ShouldBe(StoreName);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ContainerMissReturnsFailureResultRatherThanThrowing()
    {
        // Arrange — path 'data' genuinely does not register 'Connection'; the miss is expected.
        var dataPath = (await BuildStore()).Path("data");
        dataPath.IsSuccess.ShouldBeTrue();
        dataPath.Value.ShouldNotBeNull();

        // Act — before the back-reference was wired this threw NullReferenceException from
        // ContainerNotFoundResult while building its ResultDetails.
        var result = dataPath.Value.Container("Connection");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(DataStoresResultCodes.ContainerNotFoundInPath);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task NodeMissReturnsFailureResultRatherThanThrowing()
    {
        // Arrange — Node and Container share ContainerNotFoundResult, so both surfaces must be proven.
        var dataPath = (await BuildStore()).Path("data");
        dataPath.IsSuccess.ShouldBeTrue();
        dataPath.Value.ShouldNotBeNull();

        // Act
        var result = dataPath.Value.Node("Connection");

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(DataStoresResultCodes.ContainerNotFoundInPath);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void SetPathsRejectsASecondCall()
    {
        // Arrange — set-once, mirroring DataPath.SetContainers: a second call is a wiring defect.
        var store = new DataStore(StoreName, Guid.NewGuid(), []);
        store.SetPaths([]);

        // Act / Assert
        Should.Throw<InvalidOperationException>(() => store.SetPaths([]));
    }
}
