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

    private static DataSetStatusProvider Over(Mock<IDataSetWriterRuns> runs, int? hours, params IDataSetImplementationConfiguration[] dataSets)
    {
        var configuration = new Mock<IDataSetConfigurationProvider>();
        configuration.Setup(c => c.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>.Success(dataSets));
        return new DataSetStatusProvider(configuration.Object, runs.Object, Settings(hours).Object, new Mock<ITenantContext>().Object, new FixedTime(), NullLogger<DataSetStatusProvider>.Instance);
    }

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
}
