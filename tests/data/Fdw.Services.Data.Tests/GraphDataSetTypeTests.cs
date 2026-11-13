using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Results;
using Fdw.Services.Data.Transformations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>FDW-5287, FDW-5291: a Graph data set is a kind of data set, and a read of it runs its graph and shapes the rows, or is refused by name.</summary>
public sealed class GraphDataSetTypeTests
{
    private static DataSetExecutionContext Context(
        IDataSetImplementationConfiguration config, IGraphDataSetRunner? runner = null, IDataGateway? gateway = null)
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

    // Why a component: a graph with none and no field fed is abstract and is answered as not bound before any run (FDW-5294).
    private static GraphDataSetConfiguration Bound(string name)
    {
        var graph = new GraphDataSetConfiguration { Name = name, Implementation = "Graph" };
        graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "src", ComponentType = "DataSetSource" });
        return graph;
    }

    private static IReadOnlyList<IDictionary<string, object?>> Rows(params int[] ids) =>
        [.. ids.Select(id => (IDictionary<string, object?>)new Dictionary<string, object?> { ["Id"] = id, ["Name"] = $"row{id}" })];

    private static IGraphDataSetRunner Runs(IGenericResult<IReadOnlyList<IDictionary<string, object?>>> answer)
    {
        var runner = new Mock<IGraphDataSetRunner>();
        runner.Setup(r => r.Read(It.IsAny<IDataGateway>(), It.IsAny<IGraphDataSetConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer);
        return runner.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5287")]
    public void GraphIsAKindOfDataSetAndTheOlderKindsAreStillThere()
    {
        var kind = DataSetTypes.ByName("Graph");

        ReferenceEquals(kind, DataSetTypes.NotFound).ShouldBeFalse();
        kind.ShouldBeOfType<GraphDataSetType>();
        kind.PagesInStore.ShouldBeFalse();
        kind.AggregatesInStore.ShouldBeFalse();
        DataSetTypes.ByName("Simple").ShouldBeOfType<SimpleDataSetType>();
        DataSetTypes.ByName("Compound").ShouldBeOfType<CompoundDataSetType>();
        DataSetTypes.ByName("Federated").ShouldBeOfType<FederatedDataSetType>();
        DataSetTypes.ByName("Variation").ShouldBeOfType<VariationDataSetType>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public async Task AHostThatRegisteredNoRunnerRefusesTheReadByNameAndAnswersNoRows()
    {
        var graph = Bound("GasPrices");
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Price", FedByComponent = "Join1", FedByField = "Price" });

        var result = await new GraphDataSetType().Execute<IEnumerable<object>>(
            Context(graph), new QueryCommand<object>(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetRunnerMissingCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public async Task ARunThatCannotFinishAnswersItsOwnNamedFailureAndNoRows()
    {
        var graph = Bound("GasPrices");
        var failure = GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Failure(DataServiceResultCodes.ByName("GraphDataSetNotBound"));

        var result = await new GraphDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(
            Context(graph, Runs(failure), new Mock<IDataGateway>().Object), new QueryCommand<Dictionary<string, object?>>(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetNotBoundCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public async Task ARunThatFinishesAnswersEveryRowWhenTheReadStatesNoFilterOrderOrPage()
    {
        var graph = Bound("GasPrices");
        var runner = Runs(GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Success(Rows(3, 1, 2)));

        var result = await new GraphDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(
            Context(graph, runner, new Mock<IDataGateway>().Object), new QueryCommand<Dictionary<string, object?>>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Select(row => (int)row["Id"]!).ShouldBe([3, 1, 2]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public async Task TheCallersFilterOrderAndPageAreAppliedToTheRowsInThatOrder()
    {
        var graph = Bound("GasPrices");
        var runner = Runs(GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Success(Rows(5, 1, 4, 2, 3, 6)));
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Filter = new FilterExpression { Root = new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("GreaterThan"), Value = 1 } },
            Ordering = new OrderingExpression { OrderedFields = [new OrderedField { PropertyName = "Id", Direction = SortDirections.ByName("Descending") }] },
            Paging = new PagingExpression { Skip = 1, Take = 2 }
        };

        var result = await new GraphDataSetType().Execute<IEnumerable<Dictionary<string, object?>>>(
            Context(graph, runner, new Mock<IDataGateway>().Object), command, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Select(row => (int)row["Id"]!).ShouldBe([5, 4]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5287")]
    public async Task ADataSetOfAnotherKindIsAKindMismatchNotAGraphRefusal()
    {
        var simple = new SimpleDataSetConfiguration { Name = "Teams", Implementation = "Graph" };

        var result = await new GraphDataSetType().Execute<IEnumerable<object>>(
            Context(simple), new QueryCommand<object>(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<DataSetKindMismatchCode>();
    }
}
