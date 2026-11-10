using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Logging;
using Fdw.Services.Connections.Scripts;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests;

// FDW-4903: a script's rows stream from the open reader, each row read when it is asked for; a failure ends the read as one
// failed record, and whatever ends it releases the reader and what it was handed.
public sealed class ScriptedRecordSourceTests
{
    private static DataTable Ids(int count)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        for (var i = 1; i <= count; i++)
            table.Rows.Add(i);

        return table;
    }

    private static IEnumerable<IGenericMessage> StoreFailed(Exception exception) => [new GenericMessage("store failed: " + exception.Message)];

    private static IRecordSource<DataRecord> Open(CountingReader reader, int? take, Released released)
    {
        var opened = ScriptedRows.Open(reader, [reader, released], take, StoreFailed, NullLogger.Instance);
        opened.IsSuccess.ShouldBeTrue(opened.CurrentMessage);
        return opened.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARowIsReadWhenItIsAskedForAndNotBefore()
    {
        var reader = new CountingReader(Ids(5));
        var released = new Released();
        await using var source = Open(reader, null, released);

        var readWhenFirstArrived = 0;
        var ids = new List<object?>();
        await foreach (var record in source.Read(TestContext.Current.CancellationToken))
        {
            record.IsSuccess.ShouldBeTrue(record.CurrentMessage);
            if (ids.Count == 0)
                readWhenFirstArrived = reader.Reads;
            ids.Add(record.Value["Id"]);
        }

        readWhenFirstArrived.ShouldBe(1);
        ids.ShouldBe([1, 2, 3, 4, 5]);
        released.Count.ShouldBe(1);
        reader.Released.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheSchemaIsTheResultsOwnColumns()
    {
        var table = Ids(1);
        table.Columns.Add("Team", typeof(string));
        using var source = Open(new CountingReader(table), null, new Released());

        source.Schema.Fields.Select(field => field.Name).ShouldBe(["Id", "Team"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheRowAfterTheMostIsAFailedRecordThatEndsTheReadAfterTheRowsBeforeIt()
    {
        var reader = new CountingReader(Ids(5));
        var released = new Released();
        await using var source = Open(reader, 2, released);

        var outcomes = new List<IGenericResult<DataRecord>>();
        await foreach (var record in source.Read(TestContext.Current.CancellationToken))
            outcomes.Add(record);

        outcomes.Select(record => record.IsSuccess).ShouldBe([true, true, false]);
        outcomes[2].CurrentMessage.ShouldNotBeNull().ShouldContain("2 row");
        reader.Reads.ShouldBe(3);
        released.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStoreFailurePartWayIsAFailedRecordSayingHowManyRowsWereReadAndWhatTheStoreSaid()
    {
        var reader = new CountingReader(Ids(5), failOnRead: 3);
        var released = new Released();
        await using var source = Open(reader, null, released);

        var outcomes = new List<IGenericResult<DataRecord>>();
        await foreach (var record in source.Read(TestContext.Current.CancellationToken))
            outcomes.Add(record);

        outcomes.Select(record => record.IsSuccess).ShouldBe([true, true, false]);
        var said = outcomes[2].Messages.Select(message => message.Message).ToList();
        said.ShouldContain(message => message.Contains("after 2 row", StringComparison.Ordinal));
        said.ShouldContain(message => message.Contains("the store went away", StringComparison.Ordinal));
        released.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task CancellingTheReadReleasesTheReaderAndWhatItWasHanded()
    {
        var reader = new CountingReader(Ids(5));
        var released = new Released();
        var source = Open(reader, null, released);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var record in source.Read(cancel.Token))
            {
                record.IsSuccess.ShouldBeTrue(record.CurrentMessage);
                await cancel.CancelAsync();
            }
        });

        reader.Released.ShouldBeTrue();
        released.Count.ShouldBe(1);
        reader.Reads.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task StoppingEarlyReleasesTheReaderWithoutReadingTheRest()
    {
        var reader = new CountingReader(Ids(5));
        var released = new Released();
        var source = Open(reader, null, released);

        await foreach (var record in source.Read(TestContext.Current.CancellationToken))
        {
            record.IsSuccess.ShouldBeTrue(record.CurrentMessage);
            break;
        }

        reader.Released.ShouldBeTrue();
        released.Count.ShouldBe(1);
        reader.Reads.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task DisposingASourceNobodyReadReleasesItOnce()
    {
        var reader = new CountingReader(Ids(2));
        var released = new Released();
        var source = Open(reader, null, released);

        await source.DisposeAsync();
        source.Dispose();

        reader.Released.ShouldBeTrue();
        released.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AColumnNamedTwiceFailsTheOpenNamingItAndReleasesWhatItWasHanded()
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("ID", typeof(int));
        var reader = new CountingReader(table);
        var released = new Released();

        var opened = ScriptedRows.Open(reader, [reader, released], null, StoreFailed, NullLogger.Instance);

        opened.IsSuccess.ShouldBeFalse();
        opened.CurrentMessage.ShouldNotBeNull().ShouldContain("ID");
        reader.Released.ShouldBeTrue();
        released.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASecondReadIsAFailedRecordRatherThanAnEmptyResult()
    {
        var source = Open(new CountingReader(Ids(2)), null, new Released());
        await foreach (var record in source.Read(TestContext.Current.CancellationToken))
            record.IsSuccess.ShouldBeTrue(record.CurrentMessage);

        var again = new List<IGenericResult<DataRecord>>();
        await foreach (var record in source.Read(TestContext.Current.CancellationToken))
            again.Add(record);

        again.ShouldHaveSingleItem().IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASynchronousReadIsAFailedRecord()
    {
        using var source = Open(new CountingReader(Ids(2)), null, new Released());

        source.Read().ShouldHaveSingleItem().IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AConnectionThatDoesNotStreamRefusesAStreamSayingToRouteItWithTheStreamDelegate()
    {
        var command = new Mock<IScriptStreamCommand>();
        command.SetupGet(c => c.CommandType).Returns("Stream");
        command.Setup(c => c.FirstProblem()).Returns((IGenericMessage?)null);

        var routed = await ScriptedCommands.Route(
            command.Object,
            _ => Task.FromResult<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>>(GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Success([])),
            _ => Task.FromResult<IGenericResult<int>>(GenericResult<int>.Success(0)),
            NullLogger.Instance);

        routed.IsSuccess.ShouldBeFalse();
        routed.FirstFailureMessage().ShouldNotBeNull().Message.Code.ShouldBe(ScriptedCommandsLog.StreamNotRouted(NullLogger.Instance).Code);
        routed.CurrentMessage.ShouldNotBeNull().ShouldContain("does not route Stream");
        routed.CurrentMessage.ShouldContain("ScriptedCommands.Route with the stream delegate");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AConnectionThatStreamsAnswersAStreamWithItsRecordSource()
    {
        var command = new Mock<IScriptStreamCommand>();
        command.SetupGet(c => c.CommandType).Returns("Stream");
        command.Setup(c => c.FirstProblem()).Returns((IGenericMessage?)null);
        using var source = Open(new CountingReader(Ids(1)), null, new Released());

        var routed = await ScriptedCommands.Route(
            command.Object,
            _ => Task.FromResult<IGenericResult<IReadOnlyList<IDictionary<string, object?>>>>(GenericResult<IReadOnlyList<IDictionary<string, object?>>>.Success([])),
            _ => Task.FromResult<IGenericResult<int>>(GenericResult<int>.Success(0)),
            _ => Task.FromResult<IGenericResult<IRecordSource<DataRecord>>>(GenericResult<IRecordSource<DataRecord>>.Success(source)),
            NullLogger.Instance);

        routed.IsSuccess.ShouldBeTrue(routed.CurrentMessage);
        routed.Value.ShouldBeSameAs(source);
    }
}
