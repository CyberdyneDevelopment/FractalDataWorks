using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-4488: the writers, their latest runs and the window are read once for a whole list, and each data set's status is then looked up.
/// </summary>
public sealed class DataSetStatusLookupTests
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

    private static Mock<IDataSetWriters> Writers(params (string DataSet, string Pipeline)[] writes)
    {
        var writers = new Mock<IDataSetWriters>();
        IReadOnlyDictionary<string, IReadOnlyList<DataSetWriter>> map = writes
            .GroupBy(w => w.DataSet, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DataSetWriter>)[.. g.Select(w => new DataSetWriter(Guid.NewGuid(), w.Pipeline, true))], StringComparer.OrdinalIgnoreCase);
        writers.Setup(w => w.ByDataSet(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriter>>>.Success(map));
        return writers;
    }

    private static Mock<IExecutionTracker> Runs(string pipeline, IExecutionStateType state, TimeSpan? completedAgo)
    {
        var run = new Mock<IExecutionItem>();
        run.SetupGet(r => r.State).Returns(state);
        run.SetupGet(r => r.CompletedAt).Returns(completedAgo is { } ago ? Now - ago : null);
        var tracker = new Mock<IExecutionTracker>();
        tracker.Setup(t => t.GetLatestRuns(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<string, IExecutionItem>>.Success(
                new Dictionary<string, IExecutionItem>(StringComparer.OrdinalIgnoreCase) { [pipeline] = run.Object }));
        return tracker;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task AWindowThatIsNotStatedStopsTheReadAndNamesTheSetting()
    {
        var lookup = await DataSetStatusLookup.Read(
            Writers().Object, new Mock<IExecutionTracker>().Object, Settings(null).Object, null, new FixedTime(), NullLogger.Instance, TestContext.Current.CancellationToken);

        lookup.IsFailure.ShouldBeTrue();
        lookup.Messages[^1].Code.ShouldBe("DATASETS-90003");
        lookup.Messages[^1].Message.ShouldContain(SettingDefinitions.DataSetStatusWindowHours);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task EachDataSetsStatusIsLookedUpFromOneReadOfTheWritersAndTheirRuns()
    {
        var tracker = Runs("Load", ExecutionStateTypes.Completed, TimeSpan.FromHours(2));
        var lookup = await DataSetStatusLookup.Read(
            Writers(("Orders", "Load"), ("Customers", "Load")).Object, tracker.Object, Settings(168).Object, null, new FixedTime(), NullLogger.Instance, TestContext.Current.CancellationToken);

        lookup.IsSuccess.ShouldBeTrue();
        lookup.Value!.StatusOf("orders", NullLogger.Instance).Value!.Name.ShouldBe("Current");
        lookup.Value.StatusOf("Customers", NullLogger.Instance).Value!.Name.ShouldBe("Current");
        lookup.Value.StatusOf("Nobody", NullLogger.Instance).Value!.Name.ShouldBe("Unwritten");
        tracker.Verify(t => t.GetLatestRuns(It.Is<IReadOnlyCollection<string>>(names => names.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async Task NoWritersAtAllReadsNoRuns()
    {
        var tracker = new Mock<IExecutionTracker>();
        var lookup = await DataSetStatusLookup.Read(
            Writers().Object, tracker.Object, Settings(168).Object, null, new FixedTime(), NullLogger.Instance, TestContext.Current.CancellationToken);

        lookup.Value!.StatusOf("Orders", NullLogger.Instance).Value!.Name.ShouldBe("Unwritten");
        tracker.Verify(t => t.GetLatestRuns(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
