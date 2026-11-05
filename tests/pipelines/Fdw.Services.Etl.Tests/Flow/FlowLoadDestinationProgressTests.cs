using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
/// Tests for what a load destination tells the run's records while it loads (FDW-3785): the target emptied, and the rows committed after each batch, so a run that is
/// stopped or lost still says how far its load got.
/// </summary>
public sealed class FlowLoadDestinationProgressTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 9, 10, 11, TimeSpan.Zero);

    private readonly Guid _exports = Guid.NewGuid();
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

    private static LoadUnit Unit(string kind)
    {
        var step = new PipelineLoadStepConfiguration
        {
            Name = "load",
            Kind = kind,
            IsEnabled = true,
            Columns = kind == "Insert"
                ? [new PipelineLoadColumnConfiguration { Name = "Team", Role = "Write", ColumnName = "Team", Ordinal = 0 }, new PipelineLoadColumnConfiguration { Name = "Name", Role = "Write", ColumnName = "Name", Ordinal = 1 }]
                : [],
        };
        return LoadUnit.ForContainer("graph destination 'players'", LoadStepKinds.ByName(kind), step, "Exports", "exports", "Players");
    }

    private PipelineLoadExecutor Executor()
    {
        var connection = new RecordingConnection("exports", _events);
        var dataStores = DataStoreProviderOf(("Exports", "exports", "Players", Container("Players", _exports)));
        var types = new TypeResolver(new Dictionary<Guid, IConnectionType> { [_exports] = ConnectionType(AllCapabilities) });
        return new PipelineLoadExecutor(new LoadTargetResolver(null, dataStores.Object), ConnectionProviderOf((_exports, connection.Connection)).Object, types, NullLogger.Instance);
    }

    private static IDictionary<string, object?> Player(string team, string name) => Fields(("Team", team), ("Name", name));

    private async Task<(FlowLoadDestination Destination, CommitReporter Reporter, IGenericResult Result)> Load(bool replace, bool tracked = true)
    {
        var destination = new FlowLoadDestination("dst", Executor(), "export", Unit("Insert"), replace ? Unit("Truncate") : null, new Dictionary<string, object?>(), Now);
        var graph = Graph([FromRows("src", Player("Chiefs", "Mahomes"), Player("Broncos", "Nix"), Player("Chiefs", "Kelce")), destination], [Link("src", "dst")]);
        var reporter = new CommitReporter();
        var context = new FlowStepContext(new FlowVariables(), new FlowExecutor(), new FlowRunOptions { LinkCapacity = 2 }, null, null, tracked ? reporter : null, null);

        var ran = await new FlowPipeline("run", [new FlowStageStep("flow", graph)]).Run(context, TestContext.Current.CancellationToken);
        return (destination, reporter, ran);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task EveryCommittedBatchIsReportedWithTheTotalSoFar()
    {
        var (destination, reporter, ran) = await Load(replace: false);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        reporter.Commits.Count.ShouldBe(2);
        reporter.Commits.ShouldAllBe(commit => commit.Destination == "dst" && !commit.TargetEmptied && !commit.WholeSet);

        // Why an Insert load that does not empty its target does not repeat to the same result: a second run adds the rows again (FDW-3840).
        reporter.Commits.ShouldAllBe(commit => !commit.RepeatsToSameResult);
        reporter.Commits[0].RowsCommitted.ShouldBeLessThan(reporter.Commits[1].RowsCommitted);
        reporter.Commits[1].RowsCommitted.ShouldBe(destination.RowsLoaded);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AReplaceLoadReportsTheTargetEmptiedBeforeAnyRowAndKeepsSayingSo()
    {
        var (_, reporter, ran) = await Load(replace: true);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        reporter.Commits.Count.ShouldBe(3);
        reporter.Commits[0].TargetEmptied.ShouldBeTrue();
        reporter.Commits[0].RowsCommitted.ShouldBe(0L);
        reporter.Commits.ShouldAllBe(commit => commit.TargetEmptied && commit.RepeatsToSameResult);
        _events.IndexOf("exec:exports:Delete").ShouldBeLessThan(_events.IndexOf("exec:exports:Insert"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatIsNotTrackedLoadsAsBeforeAndReportsNothing()
    {
        var (destination, reporter, ran) = await Load(replace: false, tracked: false);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        reporter.Commits.ShouldBeEmpty();
        destination.RowsLoaded.ShouldBeGreaterThan(0);
    }
}
