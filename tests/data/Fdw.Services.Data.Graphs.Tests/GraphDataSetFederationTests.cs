using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Graphs.Tests;

/// <summary>
/// FDW-6970: a Graph data set whose join reads two different stores (here two data sets) runs those reads under the FDW-level federation strategy. With
/// no FederationSettings registered the read is refused naming why, never run ungated; under each shipped strategy it returns the same rows.
/// </summary>
public sealed class GraphDataSetFederationTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW6970")]
    public async Task AJoinAcrossStoresWithNoFederationSettingsIsRefusedNamingWhy()
    {
        var result = await Runner(null).Read(Gateway(), Joined(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("FederationSettings");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW6970")]
    public async Task AJoinAcrossStoresUnderAStrategyThatIsNotRegisteredIsRefusedNamingIt()
    {
        var result = await Runner(Settings("NoSuchStrategy")).Read(Gateway(), Joined(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("NoSuchStrategy");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW6970")]
    [InlineData("Sequential")]
    [InlineData("Parallel")]
    [InlineData("Optimized")]
    public async Task AJoinAcrossStoresReturnsTheSameRowsUnderEachShippedStrategy(string strategy)
    {
        var result = await Runner(Settings(strategy)).Read(Gateway(), Joined(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().Select(row => (row["OrderId"], row["r_Amount"])).ShouldBe([(1, 5), (2, 7), (3, 9)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW6970")]
    public async Task TheDataSetsOwnStrategyIsUsedOverTheFdwLevelDefault()
    {
        var graph = Joined();
        graph.FederationStrategy = "Sequential";

        var result = await Runner(Settings("NoSuchStrategy")).Read(Gateway(), graph, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW6970")]
    public async Task ADataSetStatingAStrategyNoMemberCarriesIsRefusedNamingIt()
    {
        var graph = Joined();
        graph.FederationStrategy = "Fastest";

        var result = await Runner(Settings("Optimized")).Read(Gateway(), graph, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Fastest");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW6970")]
    [InlineData(FederationResolution.ReadsInParallelOption, "false")]
    [InlineData(FederationResolution.MaxParallelReadsOption, "1")]
    [InlineData(FederationResolution.OrdersSourcesOption, "true")]
    public async Task TheDataSetsOwnOptionValuesAreReadAndTheRowsStayTheSame(string option, string value)
    {
        var graph = Joined();
        graph.FederationOptions.Add(new DataSetFederationOptionConfiguration { OptionName = option, OptionValue = value });

        var result = await Runner(Settings("Parallel")).Read(Gateway(), graph, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().Select(row => (row["OrderId"], row["r_Amount"])).ShouldBe([(1, 5), (2, 7), (3, 9)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW6970")]
    public async Task AnOptionTheStrategyDoesNotHaveIsRefusedNamingIt()
    {
        var graph = Joined();
        graph.FederationOptions.Add(new DataSetFederationOptionConfiguration { OptionName = "Fastest", OptionValue = "true" });

        var result = await Runner(Settings("Optimized")).Read(Gateway(), graph, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Fastest");
    }

    // FDW-level settings that name the default strategy and state no option value, so each strategy's declared values apply.
    private static FederationSettings Settings(string defaultStrategy) => new(defaultStrategy, new Dictionary<string, FederationStrategySettings>());

    private static GraphDataSetRunner Runner(FederationSettings? federation) =>
        new(
            new GraphDataSetReadOptions(100, 10, 2),
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            NullLogger<GraphDataSetRunner>.Instance,
            federation);

    // Orders joined to refunds, each read through a data set of its own: a join across two stores.
    private static GraphDataSetConfiguration Joined()
    {
        var graph = new GraphDataSetConfiguration { Name = "OrderRefunds", Implementation = "Graph", FieldsOutComponent = "join", FieldsOutOutput = "output" };
        graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "orders", ComponentType = "DataSetSource", DataSetName = "Orders", BatchSize = 2 });
        graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "refunds", ComponentType = "DataSetSource", DataSetName = "Refunds", BatchSize = 2 });
        var join = new GraphDataSetComponentConfiguration { Name = "join", ComponentType = "MergeJoin", JoinKind = "Inner", RightPrefix = "r_", BatchSize = 2 };
        join.Columns.Add(new GraphDataSetComponentColumnConfiguration { SettingName = "leftKeys", Ordinal = 0, ColumnName = "OrderId" });
        join.Columns.Add(new GraphDataSetComponentColumnConfiguration { SettingName = "rightKeys", Ordinal = 0, ColumnName = "OrderId" });
        graph.Components.Add(join);
        graph.Links.Add(new GraphDataSetLinkConfiguration { FromComponent = "orders", FromOutput = "output", ToComponent = "join", ToInput = "left" });
        graph.Links.Add(new GraphDataSetLinkConfiguration { FromComponent = "refunds", FromOutput = "output", ToComponent = "join", ToInput = "right" });
        return graph;
    }

    // A gateway that describes stores to the planner (the data set sources need no description) and serves each data set's rows.
    private static IDataGateway Gateway()
    {
        var gateway = new Mock<IDataGateway>(MockBehavior.Strict);
        gateway.As<IPushdownPlanningGateway>();
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand _, DataSetTarget target, CancellationToken _) => Task.FromResult(Serve(string.Equals(target.DataSet, "Orders", System.StringComparison.Ordinal)
                ? [Row(("OrderId", 1), ("Total", 50)), Row(("OrderId", 2), ("Total", 70)), Row(("OrderId", 3), ("Total", 90))]
                : [Row(("OrderId", 1), ("Amount", 5)), Row(("OrderId", 2), ("Amount", 7)), Row(("OrderId", 3), ("Amount", 9))])));
        return gateway.Object;
    }

    private static IGenericResult<IRecordSource<DataRecord>> Serve(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows) =>
        GenericResult<IRecordSource<DataRecord>>.Success(new RowListRecordSource(RowListRecordSource.SchemaOfRows(rows), rows));

    private static Dictionary<string, object?> Row(params (string Name, object? Value)[] fields) =>
        fields.ToDictionary(field => field.Name, field => field.Value, StringComparer.Ordinal);
}
