using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fdw.Data.DataSets.Abstractions;
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
/// FDW-4488: the estate-wide counts list every status, highest rank first, with the number of data sets that have it; variations are not counted.
/// </summary>
public sealed class ListDataSetStatusCountsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTime : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static async System.Threading.Tasks.Task<DataSetStatusLookup> Lookup(params (string DataSet, string Pipeline, IExecutionStateType State)[] writes)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        var hours = 168;
        settings.Setup(s => s.TryGetEffectiveValue<int>(SettingDefinitions.DataSetStatusWindowHours, out hours, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(true);

        var writers = new Mock<IDataSetWriters>();
        IReadOnlyDictionary<string, IReadOnlyList<DataSetWriter>> map = writes
            .GroupBy(w => w.DataSet, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DataSetWriter>)[.. g.Select(w => new DataSetWriter(Guid.NewGuid(), w.Pipeline, true))], StringComparer.OrdinalIgnoreCase);
        writers.Setup(w => w.ByDataSet(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriter>>>.Success(map));

        var latest = new Dictionary<string, IExecutionItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var write in writes)
        {
            var run = new Mock<IExecutionItem>();
            run.SetupGet(r => r.State).Returns(write.State);
            run.SetupGet(r => r.CompletedAt).Returns(Now - TimeSpan.FromHours(1));
            latest[write.Pipeline] = run.Object;
        }

        var tracker = new Mock<IExecutionTracker>();
        tracker.Setup(t => t.GetLatestRuns(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<string, IExecutionItem>>.Success(latest));

        var read = await DataSetStatusLookup.Read(writers.Object, tracker.Object, settings.Object, null, new FixedTime(), NullLogger.Instance, TestContext.Current.CancellationToken);
        return read.Value!;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async System.Threading.Tasks.Task EveryStatusIsListedHighestRankFirstWithItsNumberOfDataSets()
    {
        var lookup = await Lookup(("Orders", "Load", ExecutionStateTypes.Failed), ("Customers", "Sync", ExecutionStateTypes.Completed), ("Prices", "Sync", ExecutionStateTypes.Completed));
        IDataSetImplementationConfiguration[] dataSets =
        [
            new SimpleDataSetConfiguration { Name = "Orders" },
            new SimpleDataSetConfiguration { Name = "Customers" },
            new SimpleDataSetConfiguration { Name = "Prices" },
            new CompoundDataSetConfiguration { Name = "Orphan" },
        ];

        var counts = ListDataSetStatusCountsEndpointBase.Count(dataSets, lookup, NullLogger.Instance);

        counts.IsSuccess.ShouldBeTrue();
        counts.Value!.Select(c => c.Status).ShouldBe(["Unwritten", "Failed", "Running", "Warning", "Current", "Idle"]);
        counts.Value.ToDictionary(c => c.Status, c => c.Count).ShouldBe(
            new Dictionary<string, int> { ["Unwritten"] = 1, ["Failed"] = 1, ["Running"] = 0, ["Warning"] = 0, ["Current"] = 2, ["Idle"] = 0 });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataSets")]
    public async System.Threading.Tasks.Task AVariationIsNotCounted()
    {
        var lookup = await Lookup();
        IDataSetImplementationConfiguration[] dataSets = [new VariationDataSetConfiguration { Name = "ByDivision" }, new SimpleDataSetConfiguration { Name = "Teams" }];

        var counts = ListDataSetStatusCountsEndpointBase.Count(dataSets, lookup, NullLogger.Instance);

        counts.Value!.Sum(c => c.Count).ShouldBe(1);
    }
}
