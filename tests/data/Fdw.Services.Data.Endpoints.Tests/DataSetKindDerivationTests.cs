using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Connections;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// A data set's kind is worked out from its sources (FDW-6806): one source is Simple, several in one data store Compound, several across
/// stores Federated, and a DataSet source makes it Federated. One store is judged by the stores' Ids, never by their names.
/// </summary>
public sealed class DataSetKindDerivationTests
{
    private static readonly Guid SalesId = Guid.Parse("68060000-0000-0000-0000-000000000001");
    private static readonly Guid CrmId = Guid.Parse("68060000-0000-0000-0000-000000000002");

    private static IDataStoreConfigurationProvider Stores()
    {
        var stores = new Mock<IDataStoreConfigurationProvider>(MockBehavior.Strict);
        Store(stores, "Sales", SalesId);
        Store(stores, "SalesAlias", SalesId);
        Store(stores, "Crm", CrmId);
        stores.Setup(s => s.Get("Missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(null!));
        return stores.Object;
    }

    private static void Store(Mock<IDataStoreConfigurationProvider> stores, string name, Guid id) =>
        stores.Setup(s => s.Get(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataStoreImplementationConfiguration>.Success(Mock.Of<IDataStoreImplementationConfiguration>(c => c.Id == id)));

    private static Task<IGenericResult<string>> Derive(params (string Source, string? DataStoreName, string? SourceKind)[] sources) =>
        DataSetKindDerivation.Derive("Orders", sources, Stores(), NullLogger.Instance, TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Category", "DataSets.FDW6806")]
    public async Task OneSourceIsSimple()
        => (await Derive(("A", "Sales", "DataStore"))).Value.ShouldBe("Simple");

    [Fact]
    [Trait("Category", "DataSets.FDW6806")]
    public async Task SeveralSourcesInOneStoreAreCompoundJudgedByTheStoresIdNotItsName()
        => (await Derive(("A", "Sales", "DataStore"), ("B", "SalesAlias", "DataStore"))).Value.ShouldBe("Compound");

    [Fact]
    [Trait("Category", "DataSets.FDW6806")]
    public async Task SourcesAcrossStoresAreFederated()
        => (await Derive(("A", "Sales", "DataStore"), ("B", "Crm", "DataStore"))).Value.ShouldBe("Federated");

    [Fact]
    [Trait("Category", "DataSets.FDW6806")]
    public async Task ADataSetSourceMakesItFederated()
        => (await Derive(("A", "Sales", "DataStore"), ("B", null, "DataSet"))).Value.ShouldBe("Federated");

    [Fact]
    [Trait("Category", "DataSets.FDW6806")]
    public async Task ASourceThatNamesNoStoreIsRefusedNeverAssumed()
    {
        var result = await Derive(("A", "Sales", "DataStore"), ("B", " ", "DataStore"));

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20038");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6806")]
    public async Task AStoreThatDoesNotExistIsRefusedNamingIt()
    {
        var result = await Derive(("A", "Sales", "DataStore"), ("B", "Missing", "DataStore"));

        result.IsFailure.ShouldBeTrue();
        result.Code!.Code.ShouldBe("DATASETS-20039");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Missing");
    }
}
