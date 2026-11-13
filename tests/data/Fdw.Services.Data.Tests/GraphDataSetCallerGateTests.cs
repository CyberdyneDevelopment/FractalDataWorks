using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Execution;
using Fdw.Services.Data.Limits;
using Fdw.Services.Data.Transformations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-5298: the gate of a Graph data set is the data set, as it is for a Compound or Federated one. A caller the data set's own provider cannot show
/// it to is answered as if it did not exist and the runner is never reached; a caller who can see it has its graph run through the gateway the read
/// arrived on, which is the gateway of that caller, and no permission is asked of the sources.
/// </summary>
public sealed class GraphDataSetCallerGateTests
{
    private static GraphDataSetConfiguration BoundGraph()
    {
        var graph = new GraphDataSetConfiguration { Name = "GasPrices", Implementation = "Graph", FieldsOutComponent = "src", FieldsOutOutput = "output" };
        graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "src", ComponentType = "DataSetSource", DataSetName = "Readings" });
        return graph;
    }

    private static Mock<IDataSetConfigurationProvider> DataSetsAnswering(IDataSetImplementationConfiguration? found)
    {
        var dataSets = new Mock<IDataSetConfigurationProvider>();
        dataSets
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(found!));
        return dataSets;
    }

    private static Mock<IGraphDataSetRunner> RunnerAnswering(int rows)
    {
        IReadOnlyList<IDictionary<string, object?>> answer =
            [.. Enumerable.Range(0, rows).Select(i => (IDictionary<string, object?>)new Dictionary<string, object?>(StringComparer.Ordinal) { ["Id"] = i })];
        var runner = new Mock<IGraphDataSetRunner>();
        runner
            .Setup(r => r.Read(It.IsAny<IDataGateway>(), It.IsAny<IGraphDataSetConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Success(answer));
        return runner;
    }

    private static DataGatewayService Gateway(IDataSetConfigurationProvider dataSets, IGraphDataSetRunner runner)
    {
        var gateways = new Mock<IConfigurationGatewayProvider>();
        return new DataGatewayService(
            NullLoggerFactory.Instance,
            new Mock<IDataConnectionProvider>().Object,
            dataSets,
            new Mock<IDomainConfigurationProvider<IDataStoreImplementationConfiguration>>().Object,
            new Mock<IConnectionLimitResolver>().Object,
            new ConnectionLimitCounterStore(),
            new FieldMappingTransformationResolver(
                gateways.Object,
                new TransformationConfigurationProvider(
                    NullLogger<TransformationConfigurationProvider>.Instance, gateways.Object, "PlatformConfiguration", "transform", "Transformation")),
            dataStoreProvider: new Mock<IDataStoreProvider>().Object,
            graphRunner: runner);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5298")]
    public async Task ACallerWhoseProviderCannotShowTheGraphDataSetIsAnsweredNotFoundAndTheRunnerIsNeverReached()
    {
        var runner = RunnerAnswering(3);
        var gateway = Gateway(DataSetsAnswering(null).Object, runner.Object);

        var result = await gateway.Execute<IEnumerable<Dictionary<string, object?>>>(
            new QueryCommand<Dictionary<string, object?>>(), new DataSetTarget("GasPrices"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        runner.Verify(r => r.Read(It.IsAny<IDataGateway>(), It.IsAny<IGraphDataSetConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5298")]
    public async Task ACallerWhoCanSeeTheGraphDataSetHasItRunThroughTheGatewayTheReadArrivedOn()
    {
        var runner = RunnerAnswering(3);
        var gateway = Gateway(DataSetsAnswering(BoundGraph()).Object, runner.Object);

        var result = await gateway.Execute<IEnumerable<Dictionary<string, object?>>>(
            new QueryCommand<Dictionary<string, object?>>(), new DataSetTarget("GasPrices"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().Count().ShouldBe(3);
        runner.Verify(r => r.Read(It.Is<IDataGateway>(g => ReferenceEquals(g, gateway)), It.IsAny<IGraphDataSetConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
