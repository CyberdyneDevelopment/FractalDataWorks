using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
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

/// <summary>FDW-5294: reading an abstract Graph data set answers its own named refusal, with or without intended sources.</summary>
public sealed class GraphDataSetNotBoundTests
{
    private static DataSetExecutionContext Context(IDataSetImplementationConfiguration config)
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
            NullLogger.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public void TheNotBoundCodeIsDataTwentyOneThirtyTwo()
    {
        var notBound = DataServiceResultCodes.ByName("GraphDataSetNotBound");

        notBound.ShouldBeOfType<GraphDataSetNotBoundCode>();
        notBound.Code.ShouldBe("DATA-21032");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public async Task AnAbstractDataSetWithFieldsAndNothingFeedingItIsRefusedAsNotBoundNotAnsweredWithNoRows()
    {
        var graph = new GraphDataSetConfiguration { Name = "DailyWeather", Implementation = "Graph" };
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Day" });

        var result = await new GraphDataSetType().Execute<IEnumerable<object>>(
            Context(graph), new QueryCommand<object>(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetNotBoundCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public async Task IntendedSourcesDoNotMakeAnAbstractDataSetReadable()
    {
        var graph = new GraphDataSetConfiguration { Name = "DailyWeather", Implementation = "Graph" };
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Day" });
        graph.Intents.Add(new GraphDataSetIntentConfiguration { Id = Guid.NewGuid(), IntentKind = "DataStore", DataStoreName = "WeatherFeed" });

        var result = await new GraphDataSetType().Execute<IEnumerable<object>>(
            Context(graph), new QueryCommand<object>(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetNotBoundCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5294")]
    public async Task ABoundDataSetIsNotAnsweredAsNotBound()
    {
        var graph = new GraphDataSetConfiguration { Name = "GasPrices", Implementation = "Graph" };
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Price", FedByComponent = "Join1", FedByField = "Price" });

        var result = await new GraphDataSetType().Execute<IEnumerable<object>>(
            Context(graph), new QueryCommand<object>(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldNotBeOfType<GraphDataSetNotBoundCode>();
    }
}
