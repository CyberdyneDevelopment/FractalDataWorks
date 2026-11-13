using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Transformations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-5300: a Variation whose root is a Graph data set reads through <see cref="DataSetResolver"/> and <see cref="DataSetQuery"/> as every variation
/// does: the chain's filters apply to the graph's result, once; an abstract root answers the named not-bound failure; the root's kind decides whether
/// the row routes answer a paged query; a Simple-rooted variation reads as before. Nothing of the Variation kind is touched.
/// </summary>
public sealed class VariationOverGraphReadTests
{
    private const string EastFilter = "{\"field\":\"Region\",\"operator\":\"Equal\",\"value\":\"East\"}";
    private const string GasFilter = "{\"field\":\"Product\",\"operator\":\"Equal\",\"value\":\"Gas\"}";
    private const string NotBoundCode = "DATA-21032";

    private static GraphDataSetFieldConfiguration Field(string name, int ordinal) =>
        new() { Id = Guid.NewGuid(), Name = name, TypeName = "String", Ordinal = ordinal };

    // Why a component when bound: a graph with none and no field fed is abstract and is answered as not bound before any run (FDW-5294).
    private static GraphDataSetConfiguration Graph(bool bound)
    {
        var graph = new GraphDataSetConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "GasPrices",
            Implementation = "Graph",
            GraphFields = [Field("Region", 0), Field("Product", 1), Field("Amount", 2)],
        };
        if (bound)
            graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "src", ComponentType = "DataSetSource" });

        return graph;
    }

    private static Dictionary<string, object?> Row(string region, string product, int amount) =>
        new(StringComparer.Ordinal) { ["Region"] = region, ["Product"] = product, ["Amount"] = amount };

    private static IReadOnlyList<IDictionary<string, object?>> GraphResult() =>
    [
        Row("East", "Gas", 1), Row("East", "Gas", 2), Row("East", "Oil", 3), Row("West", "Gas", 4), Row("East", "Gas", 5), Row("West", "Oil", 6),
    ];

    private static Mock<IGraphDataSetRunner> RunnerAnswering()
    {
        var runner = new Mock<IGraphDataSetRunner>();
        runner
            .Setup(r => r.Read(It.IsAny<IDataGateway>(), It.IsAny<IGraphDataSetConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Success(GraphResult()));
        return runner;
    }

    private static DataSetExecutionContext Context(GraphDataSetConfiguration config, IGraphDataSetRunner runner, IDataGateway gateway)
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        return new DataSetExecutionContext(
            config,
            new Mock<IDataConnectionProvider>().Object,
            new Mock<IDataStoreProvider>().Object,
            new PredicatePushdownAnalyzer(NullLogger<PredicatePushdownAnalyzer>.Instance),
            new FieldMappingTransformationResolver(
                gateways.Object,
                new TransformationConfigurationProvider(
                    NullLogger<TransformationConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "transform", "Transformation")),
            NullLogger.Instance,
            gateway,
            runner);
    }

    // Why the gateway runs the real kind: the read goes DataSetQuery -> gateway -> the Graph kind -> the runner, so the filters meet the same code a host runs.
    private static Mock<IDataGateway> GatewayRunningTheGraphKind(GraphDataSetConfiguration graph, IGraphDataSetRunner runner)
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, DataSetTarget _, CancellationToken token) =>
                new GraphDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(Context(graph, runner, gateway.Object), command, token));
        return gateway;
    }

    private static VariationDataSetConfiguration View(string name, Guid rootId, Guid? parent, string filters) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        VariationName = name,
        Implementation = VariationDataSetConfiguration.ImplementationName,
        OwnerUserId = Guid.NewGuid(),
        BaseDataSetId = rootId,
        ParentVariationId = parent,
        StoredFilters = filters,
    };

    private static DataSetResolver ResolverOver(IDataSetImplementationConfiguration root, params VariationDataSetConfiguration[] variations)
    {
        // A variation is a data set of kind Variation, so the data set provider holds it beside the others.
        IDataSetImplementationConfiguration[] everyDataSet = [root, .. variations];
        var sets = new Mock<IDataSetConfigurationProvider>();
        sets.Setup(p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((string name, int _, CancellationToken _) =>
                Task.FromResult<IGenericResult<IDataSetImplementationConfiguration>>(
                    GenericResult<IDataSetImplementationConfiguration>.Success(Array.Find(everyDataSet, d => string.Equals(d.Name, name, StringComparison.Ordinal))!)));
        sets.Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, int _, CancellationToken _) =>
                Task.FromResult<IGenericResult<IDataSetImplementationConfiguration>>(
                    GenericResult<IDataSetImplementationConfiguration>.Success(Array.Find(everyDataSet, d => d.Id == id)!)));

        var views = new Mock<IDataSetVariationConfigurationProvider>();
        views.Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((Guid id, int _, CancellationToken _) =>
                Task.FromResult<IGenericResult<IDataSetVariationImplementationConfiguration?>>(
                    GenericResult<IDataSetVariationImplementationConfiguration?>.Success(Array.Find(variations, v => v.Id == id))));

        return new DataSetResolver(sets.Object, views.Object, NullLogger<DataSetResolver>.Instance);
    }

    private static async Task<IGenericResult<DataQueryPage>> ReadThroughTheRowRoutes(
        ResolvedDataSet resolved, IDataGateway gateway, int skip, int take, CancellationToken cancellationToken) =>
        await new DataSetQuery(NullLogger.Instance).Run(
            gateway,
            resolved.DataSet,
            new DataQueryRequest { DataSetName = resolved.Name, Skip = skip, Take = take },
            take,
            take,
            _ => Task.FromResult(StoredViewFilters.Of(resolved.ViewFilters, resolved.Name, NullLogger.Instance)),
            cancellationToken).ConfigureAwait(false);

    private static async Task<ResolvedDataSet> Resolved(DataSetResolver resolver, string name, CancellationToken cancellationToken)
    {
        var resolved = await resolver.Resolve(name, cancellationToken).ConfigureAwait(false);
        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        return resolved.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5300")]
    public async Task AVariationOverAGraphResolvesToTheGraphAsItsRootWithTheChainsFiltersParentFirst()
    {
        var graph = Graph(bound: true);
        var east = View("east", graph.Id, parent: null, EastFilter);
        var eastGas = View("east-gas", graph.Id, parent: east.Id, GasFilter);

        var resolved = await Resolved(ResolverOver(graph, east, eastGas), "east-gas", TestContext.Current.CancellationToken);

        resolved.DataSet.ShouldBeSameAs(graph);
        resolved.Variation.ShouldBeSameAs(eastGas);
        resolved.ParentName.ShouldBe("east");
        resolved.ViewFilters.ShouldBe([EastFilter, GasFilter]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5300")]
    public async Task TheChainsFiltersApplyToTheGraphsResultAndTheGraphRunsOnce()
    {
        var graph = Graph(bound: true);
        var east = View("east", graph.Id, parent: null, EastFilter);
        var eastGas = View("east-gas", graph.Id, parent: east.Id, GasFilter);
        var runner = RunnerAnswering();
        var resolved = await Resolved(ResolverOver(graph, east, eastGas), "east-gas", TestContext.Current.CancellationToken);

        var page = await ReadThroughTheRowRoutes(resolved, GatewayRunningTheGraphKind(graph, runner.Object).Object, 0, 10, TestContext.Current.CancellationToken);

        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        page.Value.ShouldNotBeNull().Rows.Select(row => row["Amount"]).ShouldBe([1, 2, 5]);
        runner.Verify(r => r.Read(It.IsAny<IDataGateway>(), graph, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5300")]
    public async Task ACallersPageIsCutOnceFromTheFilteredResult()
    {
        var graph = Graph(bound: true);
        var east = View("east", graph.Id, parent: null, EastFilter);
        var eastGas = View("east-gas", graph.Id, parent: east.Id, GasFilter);
        var resolved = await Resolved(ResolverOver(graph, east, eastGas), "east-gas", TestContext.Current.CancellationToken);

        var page = await ReadThroughTheRowRoutes(resolved, GatewayRunningTheGraphKind(graph, RunnerAnswering().Object).Object, 1, 1, TestContext.Current.CancellationToken);

        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        var read = page.Value.ShouldNotBeNull();
        read.Rows.Select(row => row["Amount"]).ShouldBe([2]);
        read.HasMoreRows.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5300")]
    public async Task AnAbstractGraphRootAnswersTheNamedNotBoundFailureThroughTheVariationAndNothingRuns()
    {
        var graph = Graph(bound: false);
        var east = View("east", graph.Id, parent: null, EastFilter);
        var runner = RunnerAnswering();
        var resolved = await Resolved(ResolverOver(graph, east), "east", TestContext.Current.CancellationToken);

        var page = await ReadThroughTheRowRoutes(resolved, GatewayRunningTheGraphKind(graph, runner.Object).Object, 0, 10, TestContext.Current.CancellationToken);

        page.IsFailure.ShouldBeTrue();
        page.Messages.Select(message => message.Code).ShouldContain(NotBoundCode);
        runner.Verify(r => r.Read(It.IsAny<IDataGateway>(), It.IsAny<IGraphDataSetConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5300")]
    public async Task AVariationOverARootThatDoesNotShapeItsOwnReadIsRefusedByTheRootsKindAndNothingIsRead()
    {
        var federated = new FederatedDataSetConfiguration { Id = Guid.NewGuid(), Name = "BreweriesInNflCities", Implementation = "Federated" };
        var east = View("east", federated.Id, parent: null, EastFilter);
        var gateway = new Mock<IDataGateway>();
        var resolved = await Resolved(ResolverOver(federated, east), "east", TestContext.Current.CancellationToken);

        var page = await ReadThroughTheRowRoutes(resolved, gateway.Object, 0, 10, TestContext.Current.CancellationToken);

        page.IsFailure.ShouldBeTrue();
        page.CurrentMessage.ShouldNotBeNull().ShouldContain("kind 'Federated' does not page in the store");
        gateway.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5300")]
    public async Task ASimpleRootedVariationSendsItsChainsFiltersToTheStoreAsBefore()
    {
        var players = new SimpleDataSetConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "AfcPlayers",
            Implementation = "Simple",
            Fields = [new DataSetFieldConfiguration { Name = "Region", Ordinal = 0, TypeName = "String" }],
        };
        var east = View("east", players.Id, parent: null, EastFilter);
        var received = new List<IDataCommand>();
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataSetTarget, CancellationToken>((command, _, _) => received.Add(command))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success([new Dictionary<string, object?>(StringComparer.Ordinal) { ["Region"] = "East" }]));
        var resolved = await Resolved(ResolverOver(players, east), "east", TestContext.Current.CancellationToken);

        var page = await ReadThroughTheRowRoutes(resolved, gateway.Object, 0, 10, TestContext.Current.CancellationToken);

        page.IsSuccess.ShouldBeTrue(page.CurrentMessage);
        resolved.DataSet.ShouldBeSameAs(players);
        received.ShouldHaveSingleItem().ShouldBeAssignableTo<IQueryCommand>().Filter.ShouldNotBeNull();
    }
}
