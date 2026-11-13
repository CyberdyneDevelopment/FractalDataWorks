using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Graphs.Tests;

/// <summary>FDW-5291: a read that cannot start answers its own named failure, before any graph is built or run.</summary>
public sealed class GraphDataSetRunnerTests
{
    private static GraphDataSetRunner Runner(int rows = 10, int seconds = 5, int capacity = 2) =>
        new(new GraphDataSetReadOptions(rows, seconds, capacity), NullLogger<GraphDataSetRunner>.Instance);

    private static GraphDataSetConfiguration Graph(string? component, string? output)
    {
        var graph = new GraphDataSetConfiguration { Name = "Weather", Implementation = "Graph", FieldsOutComponent = component, FieldsOutOutput = output };
        graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "src", ComponentType = "DataSetSource" });
        return graph;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public async Task AGraphWithNoStatedEndForItsFieldsOutIsNotBound()
    {
        var result = await Runner().Read(new Mock<IDataGateway>().Object, Graph(null, null), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetNotBoundCode>();
    }

    [Theory]
    [InlineData(0, 5, 2)]
    [InlineData(10, 0, 2)]
    [InlineData(10, 5, 0)]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public async Task ALimitThatIsNotAPositiveNumberRefusesTheReadByName(int rows, int seconds, int capacity)
    {
        var result = await Runner(rows, seconds, capacity).Read(new Mock<IDataGateway>().Object, Graph("src", "output"), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetLimitNotStatedCode>();
    }

    private static GraphDataSetConfiguration OneSource()
    {
        var graph = Graph("src", "output");
        graph.Components[0].DataSetName = "Readings";
        graph.Components[0].BatchSize = 2;
        return graph;
    }

    private static IDataGateway Serving(Func<IRecordSource<DataRecord>> source)
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(GenericResult<IRecordSource<DataRecord>>.Success(source())));
        return gateway.Object;
    }

    private static IDataGateway ServingRows(int count)
    {
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows =
            [.. Enumerable.Range(0, count).Select(i => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { ["Id"] = i })];
        return Serving(() => new RowListRecordSource(RowListRecordSource.SchemaOfRows(rows), rows));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public async Task AGraphOfOneDataSetSourceIsRunThroughTheGatewayAndEveryRowIsAnswered()
    {
        var result = await Runner(rows: 100).Read(ServingRows(5), OneSource(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Select(row => (int)row["Id"]!).OrderBy(id => id).ShouldBe([0, 1, 2, 3, 4]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public async Task AReadOverTheRowLimitAnswersTheNamedFailureAndNoRows()
    {
        var result = await Runner(rows: 3).Read(ServingRows(5), OneSource(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetRowLimitExceededCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public async Task AReadOfExactlyTheRowLimitIsAnsweredWhole()
    {
        var result = await Runner(rows: 4).Read(ServingRows(4), OneSource(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Count.ShouldBe(4);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public async Task AReadThatRunsPastTheTimeLimitAnswersTheNamedFailureAndNoRows()
    {
        var result = await Runner(seconds: 1).Read(Serving(() => new BlockingRecordSource()), OneSource(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetTimeLimitExceededCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public async Task ASourceThatCannotBeOpenedAnswersTheRunFailureNamingTheReason()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IRecordSource<DataRecord>>.Failure(new GenericMessage("source unavailable")));

        var result = await Runner().Read(gateway.Object, OneSource(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetRunFailedCode>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5291")]
    public async Task ACallerWhoCancelsIsNotAnsweredAsTheTimeLimit()
    {
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancelled.CancelAfter(TimeSpan.FromMilliseconds(200));

        var result = await Runner(seconds: 30).Read(Serving(() => new BlockingRecordSource()), OneSource(), cancelled.Token);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldNotBeOfType<GraphDataSetTimeLimitExceededCode>();
    }
}
