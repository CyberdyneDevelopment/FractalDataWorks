using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;
using static Fdw.Services.Etl.Tests.Pipelines.Loads.LoadTestDoubles;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// An update destination reports the keys that matched no row (FDW-4988): under Fail the batch rolls back and the run fails, naming the count and a sample;
/// under Warn the batch commits and the run's records carry the total and up to the sample size of keys.
/// </summary>
public sealed class FlowLoadDestinationUnmatchedKeysTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 10, 11, TimeSpan.Zero);

    private readonly Guid _db = Guid.NewGuid();
    private readonly List<string> _events = [];

    private sealed class CommitReporter : IFlowReporter
    {
        public List<DestinationCommit> Commits { get; } = [];

        public Task<Guid?> StepStarting(string stepName, Guid? parent, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(Guid.NewGuid());

        public Task StepFinished(Guid stage, IGenericResult outcome, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StepCancelled(Guid stage, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task FlowFinished(Guid stage, FlowRunReport report, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DestinationCommitted(Guid stage, DestinationCommit commit, CancellationToken cancellationToken = default)
        {
            Commits.Add(commit);
            return Task.CompletedTask;
        }
    }

    private static LoadUnit Unit(string rule, int sample)
    {
        var row = new PipelineLoadStepConfiguration
        {
            Name = "dst",
            Kind = "Update",
            IsEnabled = true,
            UnmatchedKeysRule = rule,
            Columns =
            [
                new PipelineLoadColumnConfiguration { Name = "Key:Code", Role = "Key", ColumnName = "Code", Ordinal = 0 },
                new PipelineLoadColumnConfiguration { Name = "Write:Code", Role = "Write", ColumnName = "Code", Ordinal = 0 },
                new PipelineLoadColumnConfiguration { Name = "Write:Name", Role = "Write", ColumnName = "Name", Ordinal = 1 },
            ],
        };
        return LoadUnit.ForContainer("graph destination 'dst'", LoadStepKinds.ByName("Update"), row, "Store", "dbo", "Customers").WithUnmatchedKeySample(sample);
    }

    private static IDictionary<string, object?> Customer(string code, string name) => Fields(("Code", code), ("Name", name));

    private static BulkWriteAnswer OneUnmatched(IDataCommand command, int sample)
    {
        var first = command.ShouldBeOfType<BulkUpdateCommand<IDictionary<string, object?>>>().Data.First();
        IReadOnlyList<IReadOnlyDictionary<string, object?>> keys = sample == 0 ? [] : [new Dictionary<string, object?> { ["Code"] = first["Code"] }];
        return new BulkWriteAnswer(1, 1, keys);
    }

    private async Task<(FlowLoadDestination Destination, RecordingConnection Connection, CommitReporter Reporter, IGenericResult Ran)> Load(string rule, int sample)
    {
        var connection = new RecordingConnection("db", _events, answer: command => OneUnmatched(command, sample));
        var dataStores = DataStoreProviderOf(("Store", "dbo", "Customers", Container("Customers", _db)));
        var types = new TypeResolver(new Dictionary<Guid, IConnectionType> { [_db] = ConnectionType(["UnitOfWork"]) });
        var executor = new PipelineLoadExecutor(new LoadTargetResolver(null, dataStores.Object), ConnectionProviderOf((_db, connection.Connection)).Object, types, NullLogger.Instance);
        var destination = new FlowLoadDestination("dst", executor, "load", Unit(rule, sample), null, new Dictionary<string, object?>(), Now, TimeSpan.Zero);
        var graph = Graph([FromRows("src", Customer("A", "Ann"), Customer("B", "Bo"), Customer("C", "Cy")), destination], [Link("src", "dst")]);
        var reporter = new CommitReporter();
        var context = new FlowStepContext(new FlowVariables(), new FlowExecutor(), new FlowRunOptions { LinkCapacity = 2 }, null, null, reporter, null);
        var ran = await new FlowPipeline("run", [new FlowStageStep("flow", graph)]).Run(context, TestContext.Current.CancellationToken);
        return (destination, connection, reporter, ran);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task UnderFailABatchWithAnUnmatchedKeyRollsBackAndTheRunFails()
    {
        var (_, connection, _, ran) = await Load("Fail", 5);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("matched no row");
        ran.CurrentMessage.ShouldContain("Code=A");
        _events.ShouldContain("rollback:db");
        _events.ShouldNotContain("commit:db");
        connection.Commands.OfType<IReportsUnmatchedKeys>().ShouldAllBe(command => command.UnmatchedKeySample == 5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task UnderWarnEveryBatchCommitsAndTheRecordsCarryTheTotalAndASample()
    {
        var (destination, _, reporter, ran) = await Load("Warn", 1);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        _events.Count(e => e == "commit:db").ShouldBe(2);
        destination.RowsLoaded.ShouldBe(2);
        var last = reporter.Commits[^1];
        last.UnmatchedKeys.ShouldBe(2L);
        last.UnmatchedSample.ShouldNotBeNull().Count.ShouldBe(1);
        last.UnmatchedSample![0]["Code"].ShouldBe("A");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASampleSizeOfZeroRecordsTheCountAndNoKey()
    {
        var (_, _, reporter, ran) = await Load("Warn", 0);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        reporter.Commits[^1].UnmatchedKeys.ShouldBe(2L);
        reporter.Commits[^1].UnmatchedSample.ShouldNotBeNull().ShouldBeEmpty();
    }
}
