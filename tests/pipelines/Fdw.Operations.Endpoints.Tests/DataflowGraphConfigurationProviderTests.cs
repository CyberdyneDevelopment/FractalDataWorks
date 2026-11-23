using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Clients.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Tests.Shared;

namespace Fdw.Operations.Endpoints.Tests;

/// <summary>
/// Unit tests for <see cref="DataflowGraphConfigurationProvider"/>.
///
/// Only <see cref="IConfigurationGateway"/> is faked. The real provider code runs under test.
/// </summary>
[Trait("Priority", "P1")]
public class DataflowGraphConfigurationProviderTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static DataflowGraphConfigurationProvider MakeProvider(Mock<IConfigurationGateway> gateway)
        => MakeProvider(gateway, new Mock<IDataSetConfigurationProvider>());

    private static DataflowGraphConfigurationProvider MakeProvider(
        Mock<IConfigurationGateway> gateway, Mock<IDataSetConfigurationProvider> dataSets)
        => MakeProvider(gateway, dataSets, new Mock<IDataSetVariationConfigurationProvider>());

    private static DataflowGraphConfigurationProvider MakeProvider(
        Mock<IConfigurationGateway> gateway,
        Mock<IDataSetConfigurationProvider> dataSets,
        Mock<IDataSetVariationConfigurationProvider> variations)
        => MakeProvider(gateway, dataSets, variations, new Mock<Fdw.Services.Data.IDataStoreConfigurationProvider>(MockBehavior.Strict));

    private static DataflowGraphConfigurationProvider MakeProvider(
        Mock<IConfigurationGateway> gateway,
        Mock<IDataSetConfigurationProvider> dataSets,
        Mock<IDataSetVariationConfigurationProvider> variations,
        Mock<Fdw.Services.Data.IDataStoreConfigurationProvider> dataStores)
        => new DataflowGraphConfigurationProvider(
            GatewayProviderFor(gateway.Object),
            dataSets.Object,
            variations.Object,
            new Mock<ICategoryConfigurationProvider>().Object,
            dataStores.Object,
            NullLogger<DataflowGraphConfigurationProvider>.Instance);

    // Why the double answers for any connection: these tests exercise what the provider does with its
    // gateway, not which one it selects.
    private static AnyConnectionGateways GatewayProviderFor(IConfigurationGateway gateway)
        => new AnyConnectionGateways(gateway);

    private sealed class AnyConnectionGateways : IConfigurationGatewayProvider
    {
        private readonly IConfigurationGateway _gateway;

        public AnyConnectionGateways(IConfigurationGateway gateway) => _gateway = gateway;

        public int KeyChunkSize => 1000;

        public IGenericResult<IConfigurationGateway> Get(string connectionName)
            => GenericResult<IConfigurationGateway>.Success(_gateway);

        public IGenericResult Register(IConfigurationGateway gateway) => GenericResult.Success();
    }

    // ── LoadDataSets ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadDataSets_WhenGatewayReturnsRows_ReturnsList()
    {
        var gateway = new Mock<IConfigurationGateway>();
        GatewayWriteTransaction.StubOn(gateway);
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<DataSetRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataSetRecord>>.Success([
                new DataSetRecord { Id = Guid.NewGuid(), Name = "DS1" },
                new DataSetRecord { Id = Guid.NewGuid(), Name = "DS2" }
            ]));

        var result = await MakeProvider(gateway).LoadDataSets(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(2);
    }

    [Fact]
    public async Task LoadDataSets_WhenGatewayFails_ReturnsFailure()
    {
        var gateway = new Mock<IConfigurationGateway>();
        GatewayWriteTransaction.StubOn(gateway);
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<DataSetRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataSetRecord>>.Failure(new GenericMessage("DB error")));

        var result = await MakeProvider(gateway).LoadDataSets(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── LoadDataStores ────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadDataStores_ReadsTheStoresThroughTheDataStoreProviderSoEachCarriesItsFilledDescription()
    {
        // data.DataStore has no Description column (FDW-6208): the graph takes each store from the data store provider, which fills it, and never queries the
        // table itself. The store's own record is enough to draw it, so no path or container below it is read.
        var stores = new Mock<Fdw.Services.Data.IDataStoreConfigurationProvider>(MockBehavior.Strict);
        stores.Setup(p => p.Get(ConfigurationLevels.RecordOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<Fdw.Services.Connections.IDataStoreImplementationConfiguration>>.Success(
                [new Fdw.Services.Connections.DataStoreImplementationConfiguration { Id = Guid.NewGuid(), Name = "OpsDb", Implementation = "MsSql", Description = "Operational runtime data" }]));

        var result = await MakeProvider(new Mock<IConfigurationGateway>(MockBehavior.Strict), new Mock<IDataSetConfigurationProvider>(MockBehavior.Strict),
            new Mock<IDataSetVariationConfigurationProvider>(MockBehavior.Strict), stores).LoadDataStores(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var store = result.Value.ShouldNotBeNull().ShouldHaveSingleItem();
        store.Name.ShouldBe("OpsDb");
        store.Description.ShouldBe("Operational runtime data");
        stores.VerifyAll();
    }

    [Fact]
    public async Task LoadDataStores_WhenTheDataStoreProviderFails_ReturnsItsFailure()
    {
        var stores = new Mock<Fdw.Services.Data.IDataStoreConfigurationProvider>(MockBehavior.Strict);
        stores.Setup(p => p.Get(ConfigurationLevels.RecordOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<Fdw.Services.Connections.IDataStoreImplementationConfiguration>>.Failure(new GenericMessage("language not stated")));

        var result = await MakeProvider(new Mock<IConfigurationGateway>(MockBehavior.Strict), new Mock<IDataSetConfigurationProvider>(MockBehavior.Strict),
            new Mock<IDataSetVariationConfigurationProvider>(MockBehavior.Strict), stores).LoadDataStores(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("language not stated");
    }

    // ── LoadDataSetSources ────────────────────────────────────────────────────

    [Fact]
    public async Task LoadDataSetSources_ReturnsTheComposedDataSets()
    {
        var dataSet = new SimpleDataSetConfiguration { Id = Guid.NewGuid(), Name = "Orders" };
        dataSet.Sources.Add(new DataSetSourceConfiguration { Id = Guid.NewGuid(), SourceName = "Src1", Priority = 1 });

        var dataSets = new Mock<IDataSetConfigurationProvider>();
        dataSets.Setup(d => d.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>.Success([dataSet]));

        var result = await MakeProvider(new Mock<IConfigurationGateway>(), dataSets)
            .LoadDataSetSources(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
        result.Value[0].Sources[0].SourceName.ShouldBe("Src1");
    }

    [Fact]
    public async Task LoadDataSetSources_WhenTheDomainFails_ReturnsFailure()
    {
        var dataSets = new Mock<IDataSetConfigurationProvider>();
        dataSets.Setup(d => d.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>.Failure(new GenericMessage("DB error")));

        var result = await MakeProvider(new Mock<IConfigurationGateway>(), dataSets)
            .LoadDataSetSources(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── LoadVariations ────────────────────────────────────────────────────────

    private static async IAsyncEnumerable<DataSetVariationDomainConfiguration> Rows(
        IReadOnlyList<DataSetVariationDomainConfiguration> rows, Action<IGenericResult>? finish = null)
    {
        foreach (var row in rows)
            yield return row;

        finish?.Invoke(GenericResult.Failure(new GenericMessage("page broke")));
        await Task.CompletedTask;
    }

    private static Mock<IDataSetVariationConfigurationProvider> VariationsAnswering(
        IGenericResult<ConfigurationPage<DataSetVariationDomainConfiguration>> answer)
    {
        var variations = new Mock<IDataSetVariationConfigurationProvider>();
        variations.Setup(v => v.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(answer);
        return variations;
    }

    [Fact]
    public async Task LoadVariations_ReturnsEveryRowOfThePage()
    {
        var rows = new[]
        {
            new DataSetVariationDomainConfiguration { Id = Guid.NewGuid(), Name = "sales/A", BaseDataSetId = Guid.NewGuid() },
            new DataSetVariationDomainConfiguration { Id = Guid.NewGuid(), Name = "sales/B", BaseDataSetId = Guid.NewGuid() }
        };
        var variations = VariationsAnswering(GenericResult<ConfigurationPage<DataSetVariationDomainConfiguration>>.Success(
            new ConfigurationPage<DataSetVariationDomainConfiguration>(0, 10, false, _ => Rows(rows))));

        var result = await MakeProvider(new Mock<IConfigurationGateway>(), new Mock<IDataSetConfigurationProvider>(), variations)
            .LoadVariations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Select(v => v.Name).ShouldBe(["sales/A", "sales/B"]);
    }

    [Fact]
    public async Task LoadVariations_WhenTheReadFails_ReturnsFailure()
    {
        var variations = VariationsAnswering(
            GenericResult<ConfigurationPage<DataSetVariationDomainConfiguration>>.Failure(new GenericMessage("DB error")));

        var result = await MakeProvider(new Mock<IConfigurationGateway>(), new Mock<IDataSetConfigurationProvider>(), variations)
            .LoadVariations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task LoadVariations_WhenThePageEndsInFailure_ReturnsFailureNotAShortList()
    {
        var rows = new[] { new DataSetVariationDomainConfiguration { Id = Guid.NewGuid(), Name = "sales/A" } };
        var variations = VariationsAnswering(GenericResult<ConfigurationPage<DataSetVariationDomainConfiguration>>.Success(
            new ConfigurationPage<DataSetVariationDomainConfiguration>(0, 10, false, finish => Rows(rows, finish))));

        var result = await MakeProvider(new Mock<IConfigurationGateway>(), new Mock<IDataSetConfigurationProvider>(), variations)
            .LoadVariations(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── PipelineExists ────────────────────────────────────────────────────────

    [Fact]
    public async Task PipelineExists_WhenMatchingPipelineFound_ReturnsTrue()
    {
        var gateway = new Mock<IConfigurationGateway>();
        GatewayWriteTransaction.StubOn(gateway);
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success([
                new Dictionary<string, object?> { ["Name"] = "MyPipeline" }
            ]));

        var result = await MakeProvider(gateway).PipelineExists("mypipeline", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task PipelineExists_WhenNoPipelineMatches_ReturnsFalse()
    {
        var gateway = new Mock<IConfigurationGateway>();
        GatewayWriteTransaction.StubOn(gateway);
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success([
                new Dictionary<string, object?> { ["Name"] = "OtherPipeline" }
            ]));

        var result = await MakeProvider(gateway).PipelineExists("Missing", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task PipelineExists_WhenGatewayFails_ReturnsFailure()
    {
        var gateway = new Mock<IConfigurationGateway>();
        GatewayWriteTransaction.StubOn(gateway);
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(new GenericMessage("DB error")));

        var result = await MakeProvider(gateway).PipelineExists("X", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
