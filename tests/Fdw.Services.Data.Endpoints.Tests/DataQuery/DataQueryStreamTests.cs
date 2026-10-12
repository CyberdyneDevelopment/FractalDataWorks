using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests.DataQuery;

/// <summary>
/// FDW-1209: a caller no page limit applies to is answered with every row, read batch after batch as the response is written.
/// </summary>
public sealed class DataQueryStreamTests
{
    private static IGenericResult<IEnumerable<Dictionary<string, object?>>> Rows(params string[] names)
        => GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(
            names.Select(name => DataSetQueryTests.Row(("Name", name))).ToList());

    private static async Task<List<string>> Names(DataQueryStream stream)
    {
        var names = new List<string>();
        await foreach (var row in stream.Rows)
            names.Add((string)row["Name"]!);

        return names;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task EveryRowIsReadAcrossBatchesAndNoneIsLeftBehind()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .SetupSequence(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Rows("Ada", "Grace", "Edsger"))
            .ReturnsAsync(Rows("Edsger", "Alan", "Barbara"))
            .ReturnsAsync(Rows("Barbara"));
        var query = new DataSetQuery(DataSetQueryTests.Provider(gateway), NullLogger.Instance);

        var opened = await DataQueryStream.Open(
            query, DataSetQueryTests.People(), new DataQueryRequest { DataSetName = "People" }, 2, 100, DataSetQuery.Unscoped,
            NullLogger.Instance, TestContext.Current.CancellationToken);
        var stream = opened.Value.ShouldNotBeNull();
        var names = await Names(stream);

        names.ShouldBe(new[] { "Ada", "Grace", "Edsger", "Alan", "Barbara" });
        stream.Failure.ShouldBeNull();
        stream.RowCount.ShouldBe(names.Count);
        stream.Complete.ShouldBeTrue();
        gateway.Invocations.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task EachBatchIsReadFromWhereTheLastEndedInKeyOrder()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .SetupSequence(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Rows("Ada", "Grace", "Edsger"))
            .ReturnsAsync(Rows("Edsger"));
        var query = new DataSetQuery(DataSetQueryTests.Provider(gateway), NullLogger.Instance);

        var opened = await DataQueryStream.Open(
            query, DataSetQueryTests.People(), new DataQueryRequest { DataSetName = "People", Skip = 10 }, 2, 100, DataSetQuery.Unscoped,
            NullLogger.Instance, TestContext.Current.CancellationToken);
        await Names(opened.Value.ShouldNotBeNull());

        var skips = gateway.Invocations.Select(i => ((IQueryCommand)i.Arguments[0]).Paging.ShouldNotBeNull().Skip).ToList();
        skips.ShouldBe(new[] { 10, 12 });
        foreach (var invocation in gateway.Invocations)
            ((IQueryCommand)invocation.Arguments[0]).Ordering.ShouldNotBeNull().OrderedFields.Select(f => f.PropertyName).ShouldBe(new[] { "Name" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ABatchThatFailsAfterTheFirstThrowsSoTheResponseCannotEndAsAWholeOne()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .SetupSequence(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Rows("Ada", "Grace", "Edsger"))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(new GenericMessage("the store went away")));
        var query = new DataSetQuery(DataSetQueryTests.Provider(gateway), NullLogger.Instance);

        var opened = await DataQueryStream.Open(
            query, DataSetQueryTests.People(), new DataQueryRequest { DataSetName = "People" }, 2, 100, DataSetQuery.Unscoped,
            NullLogger.Instance, TestContext.Current.CancellationToken);
        var stream = opened.Value.ShouldNotBeNull();
        var names = new List<string>();

        var thrown = await Should.ThrowAsync<DataQueryStreamException>(async () =>
        {
            await foreach (var row in stream.Rows)
                names.Add((string)row["Name"]!);
        });

        names.ShouldBe(new[] { "Ada", "Grace" });
        thrown.RowsWritten.ShouldBe(2);
        thrown.DataSetName.ShouldBe("People");
        thrown.Failure.ShouldNotBeNull().IsFailure.ShouldBeTrue();
        stream.Failure.ShouldNotBeNull().IsFailure.ShouldBeTrue();
        stream.RowCount.ShouldBe(2);
        stream.Complete.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheStreamsOwnFailureIsFoundWhetherThrownDirectlyOrCarriedByAnother()
    {
        var failure = GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(new GenericMessage("the store went away"));
        var own = new DataQueryStreamException("People", 2, failure);

        DataQueryStream.CausedBy(own).ShouldBeSameAs(own);
        DataQueryStream.CausedBy(new System.InvalidOperationException("wrapped", own)).ShouldBeSameAs(own);
        DataQueryStream.CausedBy(new System.InvalidOperationException("another")).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AFirstBatchThatFailsRefusesTheStreamBeforeTheResponseStarts()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(new GenericMessage("the store went away")));
        var query = new DataSetQuery(DataSetQueryTests.Provider(gateway), NullLogger.Instance);

        var opened = await DataQueryStream.Open(
            query, DataSetQueryTests.People(), new DataQueryRequest { DataSetName = "People" }, 2, 100, DataSetQuery.Unscoped,
            NullLogger.Instance, TestContext.Current.CancellationToken);

        opened.IsFailure.ShouldBeTrue();
    }

    // Why one query: batches are cut by offset, and without a key to order by a row could repeat or be dropped between two of
    // them. The gateway here would hand the same three rows to a second batch, so a batched read shows as repeats and a second call.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ADataSetWithNoKeyFieldIsReadWholeInASingleQuery()
    {
        var keyless = DataSetQueryTests.People();
        foreach (var field in keyless.Fields)
            field.IsKey = false;
        var gateway = DataSetQueryTests.Gateway(
        [
            DataSetQueryTests.Row(("Name", "Ada")),
            DataSetQueryTests.Row(("Name", "Grace")),
            DataSetQueryTests.Row(("Name", "Edsger")),
        ]);
        var query = new DataSetQuery(DataSetQueryTests.Provider(gateway), NullLogger.Instance);

        var opened = await DataQueryStream.Open(
            query, keyless, new DataQueryRequest { DataSetName = "People" }, 2, 100, DataSetQuery.Unscoped,
            NullLogger.Instance, TestContext.Current.CancellationToken);
        var stream = opened.Value.ShouldNotBeNull();
        var names = await Names(stream);

        names.ShouldBe(new[] { "Ada", "Grace", "Edsger" });
        stream.Failure.ShouldBeNull();
        stream.RowCount.ShouldBe(3);
        stream.Complete.ShouldBeTrue();
        gateway.Invocations.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TheCallersFilterIsNotChangedByReadingBatches()
    {
        var gateway = new Mock<IDataGateway>();
        gateway
            .SetupSequence(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Rows("Ada", "Grace", "Edsger"))
            .ReturnsAsync(Rows("Edsger"));
        var query = new DataSetQuery(DataSetQueryTests.Provider(gateway), NullLogger.Instance);
        var request = new DataQueryRequest { DataSetName = "People" };

        var opened = await DataQueryStream.Open(
            query, DataSetQueryTests.People(), request, 2, 100, DataSetQuery.Unscoped, NullLogger.Instance, TestContext.Current.CancellationToken);
        await Names(opened.Value.ShouldNotBeNull());

        request.Filter.ShouldBeNull();
        request.Skip.ShouldBe(0);
    }
}
