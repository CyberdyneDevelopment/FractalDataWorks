using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Tests.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;
using static Fdw.Services.Etl.Tests.Pipelines.Loads.LoadTestDoubles;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Mike's case (FDW-4903): a script that selects millions of rows, loaded into a database. The rows stream from the script to an
/// insert load in the same stage, so what is in flight at any moment is what the links buffer and the batches being built and
/// written, never the whole result, and every row is inserted once.
/// </summary>
public sealed class ScriptToDatabaseStreamingTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    // An insert load of the Id column into Players.
    private static LoadUnit InsertIds()
    {
        var step = new PipelineLoadStepConfiguration
        {
            Name = "load",
            Kind = "Insert",
            IsEnabled = true,
            Columns = [new PipelineLoadColumnConfiguration { Name = "Id", Role = "Write", ColumnName = "Id", Ordinal = 0 }],
        };
        return LoadUnit.ForContainer("graph destination 'players'", LoadStepKinds.ByName("Insert"), step, "Exports", "exports", "Players");
    }

    // A database connection that counts and keeps the Id of every row inserted through it, from whichever thread inserts it.
    private sealed class CountingDatabase
    {
        private readonly int[] _timesInserted;
        private int _inserted;

        public CountingDatabase(int rows)
        {
            _timesInserted = new int[rows];
            var transaction = new Mock<IDataConnectionTransaction>();
            transaction
                .Setup(t => t.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IDataCommand command, IDataContainer container, CancellationToken token) =>
                {
                    var rowsInserted = ((InsertCommand<IDictionary<string, object?>>)command).Data.ToList();
                    foreach (var row in rowsInserted)
                        Interlocked.Increment(ref _timesInserted[(int)row["Id"]!]);
                    Interlocked.Add(ref _inserted, rowsInserted.Count);
                    return GenericResult<int>.Success(rowsInserted.Count);
                });
            transaction.Setup(t => t.Commit(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            transaction.Setup(t => t.Rollback(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            var connection = new Mock<ITransactionalDataConnection>();
            connection
                .Setup(c => c.BeginTransaction(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => GenericResult<IDataConnectionTransaction>.Success(transaction.Object));
            Connection = connection.Object;
        }

        public ITransactionalDataConnection Connection { get; }

        public int Inserted => Volatile.Read(ref _inserted);

        public IReadOnlyList<int> TimesInserted => _timesInserted;
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(20_000, 100, 2)]
    [InlineData(200_000, 500, 4)]
    public async Task AScriptLoadedIntoADatabaseHoldsOnlyWhatTheLinksBufferAndInsertsEveryRowOnce(int rows, int batchSize, int linkCapacity)
    {
        var connectionId = Guid.NewGuid();
        var database = new CountingDatabase(rows);
        var mostInFlight = 0;
        var script = new GeneratedScript(rows, handedOut => mostInFlight = Math.Max(mostInFlight, handedOut - database.Inserted));
        var executor = new PipelineLoadExecutor(
            new LoadTargetResolver(null, DataStoreProviderOf(("Exports", "exports", "Players", Container("Players", connectionId))).Object),
            ConnectionProviderOf((connectionId, database.Connection)).Object,
            new TypeResolver(new Dictionary<Guid, IConnectionType> { [connectionId] = ConnectionType(AllCapabilities) }),
            NullLogger.Instance);
        var source = new FlowScriptSource("src", new ScriptingGateway([], stream: script).Gateway, "Warehouse", "SELECT Id FROM dbo.Big", [], rows, batchSize);
        var destination = new FlowLoadDestination("dst", executor, "export", InsertIds(), null, new Dictionary<string, object?>(), Now);

        var result = await new FlowExecutor().Run(Graph([source, destination], [Link("src", "dst")]), Options(linkCapacity), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        database.Inserted.ShouldBe(rows);
        database.TimesInserted.ShouldAllBe(times => times == 1);

        // In flight: the batch the source is filling, the batches the link buffers, and the batch the load is writing.
        mostInFlight.ShouldBeLessThanOrEqualTo((linkCapacity + 2) * batchSize);
        script.Released.ShouldBeTrue();
    }
}
