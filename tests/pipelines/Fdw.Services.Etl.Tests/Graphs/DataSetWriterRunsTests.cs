using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Graphs;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-4488: the pipelines side answers the data set domain's question: which pipelines write each data set and how each one's latest run went, from one read of the writers and one
/// read of the latest runs.
/// </summary>
public sealed class DataSetWriterRunsTests
{
    private static Mock<IDataSetWriters> Writers(params (string DataSet, string Pipeline)[] writes)
    {
        IReadOnlyDictionary<string, IReadOnlyList<DataSetWriter>> map = writes
            .GroupBy(w => w.DataSet, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DataSetWriter>)[.. g.Select(w => new DataSetWriter(Guid.NewGuid(), w.Pipeline, true))], StringComparer.OrdinalIgnoreCase);
        var writers = new Mock<IDataSetWriters>();
        writers.Setup(w => w.ByDataSet(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyDictionary<string, IReadOnlyList<DataSetWriter>>>.Success(map));
        return writers;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task NoWritersAtAllReadsNoRuns()
    {
        var tracker = new Mock<IExecutionTracker>();

        var result = await new DataSetWriterRuns(Writers().Object, tracker.Object).ByDataSet(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
        tracker.Verify(t => t.GetLatestRuns(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task EachWriterCarriesItsPipelinesLatestRunFromOneRead()
    {
        var run = new Mock<IExecutionItem>().Object;
        var tracker = new Mock<IExecutionTracker>();
        tracker.Setup(t => t.GetLatestRuns(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<string, IExecutionItem>>.Success(
                new Dictionary<string, IExecutionItem>(StringComparer.OrdinalIgnoreCase) { ["Load"] = run }));

        var result = await new DataSetWriterRuns(Writers(("Orders", "Load"), ("Customers", "Load"), ("Prices", "Sync")).Object, tracker.Object)
            .ByDataSet(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!["orders"].ShouldHaveSingleItem().LatestRun.ShouldBeSameAs(run);
        result.Value["Prices"].ShouldHaveSingleItem().LatestRun.ShouldBeNull();
        tracker.Verify(t => t.GetLatestRuns(It.Is<IReadOnlyCollection<string>>(names => names.Count == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AFailedReadOfTheLatestRunsIsTheFailureOfTheAnswer()
    {
        var tracker = new Mock<IExecutionTracker>();
        tracker.Setup(t => t.GetLatestRuns(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyDictionary<string, IExecutionItem>>.Failure(new Fdw.Messages.GenericMessage("the runs could not be read")));

        var result = await new DataSetWriterRuns(Writers(("Orders", "Load")).Object, tracker.Object).ByDataSet(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }
}
