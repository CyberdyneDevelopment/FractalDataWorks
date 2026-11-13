using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// FDW-4488: the data set domain answers a data set's status. What a status is judged by (the writers and their latest runs, the window) is read once for a whole request, each data
/// set's status is then looked up, and the counts list every status, highest rank first, with the number of data sets that have it.
/// </summary>
public sealed class DataSetStatusProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static Mock<IEffectiveSettingsProvider> Settings(int? hours)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        var value = hours ?? 0;
        settings.Setup(s => s.TryGetEffectiveValue<int>(SettingDefinitions.DataSetStatusWindowHours, out value, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(hours is not null);
        return settings;
    }

    private static Mock<IDataSetWriterRuns> Runs(params (string DataSet, string Pipeline, IExecutionStateType State)[] writes)
    {
        IReadOnlyDictionary<string, IReadOnlyList<DataSetWriterRun>> map = writes
            .GroupBy(w => w.DataSet, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<DataSetWriterRun>)[.. g.Select(w => new DataSetWriterRun(w.Pipeline, true, Run(w.State)))],
                StringComparer.OrdinalIgnoreCase);
        var runs = new Mock<IDataSetWriterRuns>();
        runs.Setup(r => r.ByDataSet(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriterRun>>>.Success(map));
        return runs;
    }

    private static IExecutionItem Run(IExecutionStateType state)
    {
        var run = new Mock<IExecutionItem>();
        run.SetupGet(r => r.State).Returns(state);
        run.SetupGet(r => r.CompletedAt).Returns(Now - TimeSpan.FromHours(1));
        return run.Object;
    }

    // The data set domain over the data sets given: the list at any depth, and each by name at any depth (a data set that is not given does not exist).
    private static Mock<IDataSetConfigurationProvider> Configuration(params IDataSetImplementationConfiguration[] dataSets)
    {
        var configuration = new Mock<IDataSetConfigurationProvider>();
        configuration.Setup(c => c.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>.Success(dataSets));
        configuration.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, int _, CancellationToken _) =>
                GenericResult<IDataSetImplementationConfiguration>.Success(Array.Find(dataSets, d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase))!));
        return configuration;
    }

    private static DataSetStatusProvider Over(Mock<IDataSetWriterRuns> runs, int? hours, Mock<IDataSetConfigurationProvider> configuration) =>
        new(configuration.Object, runs.Object, Settings(hours).Object, new Mock<ITenantContext>().Object, new FixedTime(), NullLogger<DataSetStatusProvider>.Instance);

    private static DataSetStatusProvider Over(Mock<IDataSetWriterRuns> runs, int? hours, params IDataSetImplementationConfiguration[] dataSets) =>
        Over(runs, hours, Configuration(dataSets));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task AWindowThatIsNotStatedStopsTheReadAndNamesTheSetting()
    {
        var view = await Over(Runs(), null).Read(TestContext.Current.CancellationToken);

        view.IsFailure.ShouldBeTrue();
        view.Messages[^1].Code.ShouldBe("DATASETS-90003");
        view.Messages[^1].Message.ShouldContain(SettingDefinitions.DataSetStatusWindowHours);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task EachDataSetsStatusIsLookedUpFromOneReadOfTheWritersAndTheirRuns()
    {
        var runs = Runs(("Orders", "Load", ExecutionStateTypes.Completed), ("Customers", "Load", ExecutionStateTypes.Completed));
        var view = await Over(runs, 168).Read(TestContext.Current.CancellationToken);

        view.IsSuccess.ShouldBeTrue();
        view.Value!.StatusOf(new SimpleDataSetConfiguration { Name = "orders" }).Value!.Name.ShouldBe("Current");
        view.Value.StatusOf(new SimpleDataSetConfiguration { Name = "Customers" }).Value!.Name.ShouldBe("Current");
        view.Value.StatusOf(new SimpleDataSetConfiguration { Name = "Nobody" }).Value!.Name.ShouldBe("Unwritten");
        runs.Verify(r => r.ByDataSet(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task AVariationHasNoStatusOfItsOwn()
    {
        var view = await Over(Runs(), 168).Read(TestContext.Current.CancellationToken);

        var status = view.Value!.StatusOf(new VariationDataSetConfiguration { Name = "ByDivision" });

        status.IsSuccess.ShouldBeTrue();
        status.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task EveryStatusIsCountedHighestRankFirstWithItsNumberOfDataSets()
    {
        var runs = Runs(("Orders", "Load", ExecutionStateTypes.Failed), ("Customers", "Sync", ExecutionStateTypes.Completed), ("Prices", "Sync", ExecutionStateTypes.Completed));
        var provider = Over(
            runs,
            168,
            new SimpleDataSetConfiguration { Name = "Orders" },
            new SimpleDataSetConfiguration { Name = "Customers" },
            new SimpleDataSetConfiguration { Name = "Prices" },
            new CompoundDataSetConfiguration { Name = "Orphan" },
            new VariationDataSetConfiguration { Name = "ByDivision" });

        var counts = await provider.Counts(TestContext.Current.CancellationToken);

        counts.IsSuccess.ShouldBeTrue();
        var statuses = counts.Value.ShouldNotBeNull();
        statuses.Select(c => c.Status.Name).ToArray().ShouldBe(["Unwritten", "Failed", "Running", "Warning", "Current", "Idle"]);
        statuses.ToDictionary(c => c.Status.Name, c => c.Count).ShouldBe(
            new Dictionary<string, int> { ["Unwritten"] = 1, ["Failed"] = 1, ["Running"] = 0, ["Warning"] = 0, ["Current"] = 2, ["Idle"] = 0 });
    }

    private static GraphDataSetConfiguration Graph(string name, params string[] readsDataSets) => new()
    {
        Name = name,
        Components = [.. readsDataSets.Select((read, position) => new GraphDataSetComponentConfiguration { Name = $"src{position}", ComponentType = "DataSetSource", DataSetName = read })],
    };

    /// <summary>FDW-5296: a Graph data set is written by no pipeline, so it is never "Unwritten": it takes the status of the pipelines that write the data sets it reads.</summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task AGraphTakesTheStatusOfThePipelinesThatWriteTheDataSetsItReads()
    {
        var runs = Runs(("Orders", "Load", ExecutionStateTypes.Completed), ("Prices", "Sync", ExecutionStateTypes.Failed));
        var view = await Over(runs, 168).Read(TestContext.Current.CancellationToken);

        view.Value!.StatusOf(Graph("OverOrders", "Orders")).Value!.Name.ShouldBe("Current");
        view.Value.StatusOf(Graph("OverBoth", "orders", "Prices")).Value!.Name.ShouldBe("Failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task AGraphThatReadsNoDataSetAPipelineWritesIsStatedNotApplicableAndNeverUnwritten()
    {
        var view = await Over(Runs(("Orders", "Load", ExecutionStateTypes.Completed)), 168).Read(TestContext.Current.CancellationToken);
        var fromAStore = new GraphDataSetConfiguration
        {
            Name = "FromAStore",
            Components = [new GraphDataSetComponentConfiguration { Name = "file", ComponentType = "ContainerSource", DataStoreName = "Landing", ContainerName = "orders.csv" }],
        };
        var abstractOnly = new GraphDataSetConfiguration
        {
            Name = "Abstract",
            Intents = [new GraphDataSetIntentConfiguration { IntentKind = "DataSet", SourceDataSetId = Guid.NewGuid(), SourceDataSetName = "Orders", Ordinal = 0 }],
        };

        view.Value!.StatusOf(fromAStore).Value!.Name.ShouldBe("NotApplicable");
        view.Value.StatusOf(abstractOnly).Value!.Name.ShouldBe("NotApplicable");
        view.Value.StatusOf(Graph("OverNobody", "Nobody")).Value!.Name.ShouldBe("NotApplicable");
        view.Value.StatusOf(Graph("Empty")).Value!.Name.ShouldBe("NotApplicable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task TheStatusOfEveryOtherKindIsUnchangedByTheGraphBranch()
    {
        var view = await Over(Runs(("Orders", "Load", ExecutionStateTypes.Completed)), 168).Read(TestContext.Current.CancellationToken);

        view.Value!.StatusOf(new SimpleDataSetConfiguration { Name = "Orders" }).Value!.Name.ShouldBe("Current");
        view.Value.StatusOf(new CompoundDataSetConfiguration { Name = "Orphan" }).Value!.Name.ShouldBe("Unwritten");
        view.Value.StatusOf(new VariationDataSetConfiguration { Name = "ByDivision" }).Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task TheCountsListNotApplicableOnlyWhenAGraphHasItSoAHostWithNoGraphReturnsTheRowsItAlwaysDid()
    {
        var runs = Runs(("Orders", "Load", ExecutionStateTypes.Completed));
        var without = await Over(runs, 168, new SimpleDataSetConfiguration { Name = "Orders" }).Counts(TestContext.Current.CancellationToken);
        var with = await Over(runs, 168, new SimpleDataSetConfiguration { Name = "Orders" }, Graph("FromNobody", "Nobody"), Graph("OverOrders", "Orders"))
            .Counts(TestContext.Current.CancellationToken);

        without.Value!.Select(c => c.Status.Name).ToArray().ShouldBe(["Unwritten", "Failed", "Running", "Warning", "Current", "Idle"]);
        with.Value!.Select(c => c.Status.Name).ToArray().ShouldBe(["NotApplicable", "Unwritten", "Failed", "Running", "Warning", "Current", "Idle"]);
        with.Value!.ToDictionary(c => c.Status.Name, c => c.Count).ShouldBe(
            new Dictionary<string, int> { ["NotApplicable"] = 1, ["Unwritten"] = 0, ["Failed"] = 0, ["Running"] = 0, ["Warning"] = 0, ["Current"] = 2, ["Idle"] = 0 });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task AFailedReadOfTheWritersIsTheFailureOfTheCounts()
    {
        var runs = new Mock<IDataSetWriterRuns>();
        runs.Setup(r => r.ByDataSet(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriterRun>>>.Failure(new Fdw.Messages.GenericMessage("the pipelines could not be read")));

        var counts = await Over(runs, 168).Counts(TestContext.Current.CancellationToken);

        counts.IsFailure.ShouldBeTrue();
    }

    private static async Task<IDataSetStatusView> ViewOver(Mock<IDataSetWriterRuns> runs, Mock<IDataSetConfigurationProvider> configuration) =>
        (await Over(runs, 168, configuration).Read(TestContext.Current.CancellationToken)).Value!;

    private static async Task<string?> Nested(IDataSetStatusView view, IDataSetImplementationConfiguration dataSet)
    {
        var status = await view.StatusOfNested(dataSet, TestContext.Current.CancellationToken);
        status.IsSuccess.ShouldBeTrue(status.CurrentMessage);
        return status.Value?.Name;
    }

    /// <summary>FDW-5299: a Graph over a Graph takes the status of the pipelines that write what the nested Graph reads, however deep, and a Graph a list read without its components is read whole.</summary>
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task AGraphOverAGraphTakesTheStatusOfThePipelinesThatWriteWhatTheNestedGraphReads()
    {
        var runs = Runs(("Orders", "Load", ExecutionStateTypes.Completed), ("Prices", "Sync", ExecutionStateTypes.Failed));
        var view = await ViewOver(runs, Configuration(Graph("Top", "Mid"), Graph("Mid", "Orders"), Graph("OverPrices", "Mid", "Prices")));

        (await Nested(view, new GraphDataSetConfiguration { Name = "Top" })).ShouldBe("Current");
        (await Nested(view, new GraphDataSetConfiguration { Name = "OverPrices" })).ShouldBe("Failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ThreeLevelsOfNestingAreFollowedThroughAVariationToTheWritersBelow()
    {
        var runs = Runs(("Orders", "Load", ExecutionStateTypes.Failed));
        var view = await ViewOver(
            runs,
            Configuration(
                Graph("Top", "ByDivision"),
                new VariationDataSetConfiguration { Name = "ByDivision", SourceDataSetName = "Mid" },
                Graph("Mid", "Low"),
                Graph("Low", "Orders")));

        (await Nested(view, new GraphDataSetConfiguration { Name = "Top" })).ShouldBe("Failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ACycleThroughNestedGraphsEndsAndStillTakesTheWritersItMetOnTheWay()
    {
        var view = await ViewOver(
            Runs(("Orders", "Load", ExecutionStateTypes.Completed)),
            Configuration(Graph("A", "B"), Graph("B", "A", "Orders")));

        (await Nested(view, new GraphDataSetConfiguration { Name = "A" })).ShouldBe("Current");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ANestedGraphThatReadsNoWrittenDataSetLeavesTheGraphNotApplicable()
    {
        var view = await ViewOver(Runs(("Orders", "Load", ExecutionStateTypes.Completed)), Configuration(Graph("Top", "Mid"), Graph("Mid"), Graph("Empty")));

        (await Nested(view, new GraphDataSetConfiguration { Name = "Top" })).ShouldBe("NotApplicable");
        (await Nested(view, new GraphDataSetConfiguration { Name = "Empty" })).ShouldBe("NotApplicable");
        (await Nested(view, new GraphDataSetConfiguration { Name = "NotStored" })).ShouldBe("NotApplicable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ADataSetBelowAnotherGraphIsReadOncePerRequestHoweverManyGraphsReachIt()
    {
        var configuration = Configuration(Graph("One", "Shared"), Graph("Two", "Shared"), Graph("Shared", "Orders"));
        var view = await ViewOver(Runs(("Orders", "Load", ExecutionStateTypes.Completed)), configuration);

        (await Nested(view, new GraphDataSetConfiguration { Name = "One" })).ShouldBe("Current");
        (await Nested(view, new GraphDataSetConfiguration { Name = "Two" })).ShouldBe("Current");

        configuration.Verify(c => c.Get("Shared", ConfigurationLevels.Children, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task APageWithNoGraphReadsNothingMoreAndEveryOtherKindAnswersAsBefore()
    {
        var configuration = Configuration(new SimpleDataSetConfiguration { Name = "Orders" });
        var view = await ViewOver(Runs(("Orders", "Load", ExecutionStateTypes.Completed)), configuration);

        (await Nested(view, new SimpleDataSetConfiguration { Name = "Orders" })).ShouldBe("Current");
        (await Nested(view, new CompoundDataSetConfiguration { Name = "Orphan" })).ShouldBe("Unwritten");
        (await Nested(view, new VariationDataSetConfiguration { Name = "ByDivision" })).ShouldBeNull();

        configuration.Verify(c => c.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task AFailedReadOfANestedDefinitionIsTheFailureOfTheStatusNotAGuess()
    {
        var configuration = new Mock<IDataSetConfigurationProvider>();
        configuration.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Failure(new Fdw.Messages.GenericMessage("the data set could not be read")));
        var view = await ViewOver(Runs(), configuration);

        var status = await view.StatusOfNested(new GraphDataSetConfiguration { Name = "Top" }, TestContext.Current.CancellationToken);

        status.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task TheCountsFollowANestedGraphToTheWritersBelowIt()
    {
        var counts = await Over(
                Runs(("Orders", "Load", ExecutionStateTypes.Completed)),
                168,
                new SimpleDataSetConfiguration { Name = "Orders" },
                Graph("Top", "Mid"),
                Graph("Mid", "Orders"))
            .Counts(TestContext.Current.CancellationToken);

        counts.IsSuccess.ShouldBeTrue(counts.CurrentMessage);
        counts.Value!.ToDictionary(c => c.Status.Name, c => c.Count)["Current"].ShouldBe(3);
        counts.Value!.Select(c => c.Status.Name).ShouldNotContain("NotApplicable");
    }
}
