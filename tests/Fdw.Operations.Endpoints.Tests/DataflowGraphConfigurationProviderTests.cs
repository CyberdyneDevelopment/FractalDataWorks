using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Clients.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

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
        => new DataflowGraphConfigurationProvider(
            GatewayProviderFor(gateway.Object),
            NullLogger<DataflowGraphConfigurationProvider>.Instance);

    // Why the double answers for any connection: these tests exercise what the provider does with its
    // gateway, not which one it selects.
    private static IConfigurationGatewayProvider GatewayProviderFor(IConfigurationGateway gateway)
        => new AnyConnectionGateways(gateway);

    private sealed class AnyConnectionGateways : IConfigurationGatewayProvider
    {
        private readonly IConfigurationGateway _gateway;

        public AnyConnectionGateways(IConfigurationGateway gateway) => _gateway = gateway;

        public IGenericResult<IConfigurationGateway> Get(string connectionName)
            => GenericResult<IConfigurationGateway>.Success(_gateway);

        public IGenericResult Register(IConfigurationGateway gateway) => GenericResult.Success();
    }

    // ── LoadDataSets ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadDataSets_WhenGatewayReturnsRows_ReturnsList()
    {
        var gateway = new Mock<IConfigurationGateway>();
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
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<DataSetRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataSetRecord>>.Failure(new GenericMessage("DB error")));

        var result = await MakeProvider(gateway).LoadDataSets(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── LoadDataStores ────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadDataStores_WhenGatewayReturnsRows_ReturnsList()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<DataStoreRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataStoreRecord>>.Success([
                new DataStoreRecord { ConfigurationId = Guid.NewGuid(), StoreType = "MsSql" }
            ]));

        var result = await MakeProvider(gateway).LoadDataStores(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
    }

    [Fact]
    public async Task LoadDataStores_WhenGatewayFails_ReturnsFailure()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<DataStoreRecord>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataStoreRecord>>.Failure(new GenericMessage("DB error")));

        var result = await MakeProvider(gateway).LoadDataStores(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── LoadSources ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadSources_WhenGatewayReturnsRows_ReturnsList()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<DataSetSourceConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataSetSourceConfiguration>>.Success([
                new DataSetSourceConfiguration { Id = Guid.NewGuid(), DataSetId = Guid.NewGuid(), SourceName = "Src1", Priority = 1 }
            ]));

        var result = await MakeProvider(gateway).LoadSources(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Count.ShouldBe(1);
        result.Value[0].SourceName.ShouldBe("Src1");
    }

    [Fact]
    public async Task LoadSources_WhenGatewayFails_ReturnsFailure()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<DataSetSourceConfiguration>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<DataSetSourceConfiguration>>.Failure(new GenericMessage("DB error")));

        var result = await MakeProvider(gateway).LoadSources(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── PipelineExists ────────────────────────────────────────────────────────

    [Fact]
    public async Task PipelineExists_WhenMatchingPipelineFound_ReturnsTrue()
    {
        var gateway = new Mock<IConfigurationGateway>();
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
        gateway.Setup(g => g.DataStores).Returns((System.Collections.Generic.IReadOnlyList<Fdw.Data.Abstractions.IDataStore>)System.Array.Empty<Fdw.Data.Abstractions.IDataStore>());

        gateway.Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(
                It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(new GenericMessage("DB error")));

        var result = await MakeProvider(gateway).PipelineExists("X", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
