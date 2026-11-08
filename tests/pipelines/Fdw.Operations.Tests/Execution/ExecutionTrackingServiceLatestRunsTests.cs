using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations.Data;
using Fdw.Operations.Execution;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Tests.Execution;

/// <summary>
/// The latest run of each of a set of pipelines is read in a fixed number of requests per piece of names, inside the caller's tenants (FDW-4350).
/// </summary>
public sealed class ExecutionTrackingServiceLatestRunsTests
{
    private static readonly Guid TenantA = Guid.Parse("0a000000-0000-0000-0000-00000000000a");
    private static readonly DateTimeOffset Older = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Newer = new(2026, 11, 7, 11, 0, 0, TimeSpan.Zero);

    private sealed class StubGatewayProvider(IDataGateway gateway) : IDataGatewayProvider
    {
        public IDataGateway ByName(string name) => gateway;
    }

    // One run row of a pipeline: when it was written, and the execution item with the same id.
    private sealed record Run(string Pipeline, DateTimeOffset At, Guid Id, string State);

    private readonly Mock<IDataGateway> _gateway = new();
    private readonly List<IDataCommand> _commands = [];
    private readonly List<Run> _runs = [];

    // Changes a row the way a store that hands back the wrong type would (FDW-4350): applied to the grouped read and to the row read alike.
    private Action<Dictionary<string, object?>>? _corrupt;

    private void Seed(params Run[] runs) => _runs.AddRange(runs);

    // The gateway answers the way the stores do: the grouped newest time of the names asked, the rows at those times, the items by id.
    private ExecutionTrackingService Tracker(int? chunkSize, bool global = false, bool itemsVisible = true)
    {
        _gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns<IDataCommand, DataStoreTarget, CancellationToken>((command, _, _) =>
            {
                _commands.Add(command);
                var query = (QueryCommand<Dictionary<string, object?>>)command;
                return Task.FromResult(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(query.Aggregation is null ? RowsAsked(query) : NewestAsked(query)));
            });
        _gateway
            .Setup(g => g.Execute<IEnumerable<ExecutionItem>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Returns<IDataCommand, DataStoreTarget, CancellationToken>((command, _, _) =>
            {
                _commands.Add(command);
                return Task.FromResult(GenericResult<IEnumerable<ExecutionItem>>.Success(itemsVisible ? ItemsAsked((QueryCommand<ExecutionItem>)command) : []));
            });

        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupGet(a => a.Current).Returns(Caller(global));
        return new ExecutionTrackingService(
            new StubGatewayProvider(_gateway.Object), NullLoggerFactory.Instance, accessor.Object, "OpsDb", latestRunsChunkSize: chunkSize);
    }

    private static IAuthenticationContext Caller(bool global)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.ActiveTenantId).Returns(TenantA);
        caller.SetupGet(c => c.IsSystemContext).Returns(false);
        caller.SetupGet(c => c.Claims).Returns(global
            ? new Dictionary<string, object>(StringComparer.Ordinal) { ["globalTenant"] = "true" }
            : new Dictionary<string, object>(StringComparer.Ordinal));
        return caller.Object;
    }

    private static IEnumerable<FilterCondition> Conditions(IFilterNode? node) => node switch
    {
        FilterCondition c => [c],
        FilterGroup g => g.Nodes.SelectMany(Conditions),
        _ => []
    };

    private static List<object?> Values(IFilterExpression? filter, string property)
        => ((IEnumerable<object?>)Conditions((filter as FilterExpression)?.Root).Single(c => c.PropertyName == property).Value!).ToList();

    private IEnumerable<Dictionary<string, object?>> NewestAsked(QueryCommand<Dictionary<string, object?>> query)
    {
        var names = Values(query.Filter, "PipelineName");
        return _runs
            .Where(r => names.Contains(r.Pipeline))
            .GroupBy(r => r.Pipeline, StringComparer.OrdinalIgnoreCase)
            .Select(g => Corrupted(new Dictionary<string, object?> { ["PipelineName"] = g.Key, ["NewestAt"] = g.Max(r => r.At) }));
    }

    private IEnumerable<Dictionary<string, object?>> RowsAsked(QueryCommand<Dictionary<string, object?>> query)
    {
        var names = Values(query.Filter, "PipelineName");
        var times = Values(query.Filter, "CreateDate");
        return _runs
            .Where(r => names.Contains(r.Pipeline) && times.Contains(r.At))
            .Select(r => Corrupted(new Dictionary<string, object?> { ["Id"] = r.Id, ["PipelineName"] = r.Pipeline, ["CreateDate"] = r.At }));
    }

    private Dictionary<string, object?> Corrupted(Dictionary<string, object?> row)
    {
        _corrupt?.Invoke(row);
        return row;
    }

    private IEnumerable<ExecutionItem> ItemsAsked(QueryCommand<ExecutionItem> query)
    {
        var ids = Values(query.Filter, "Id");
        return _runs.Where(r => ids.Contains(r.Id)).Select(r => new ExecutionItem
        {
            Id = r.Id, RootExecutionItemId = r.Id, ItemType = "Job", Name = r.Pipeline + "_20261107", State = r.State, TenantId = TenantA,
        });
    }

    [Fact]
    public async Task EachPipelineAnswersItsNewestRun()
    {
        var oldFail = Guid.NewGuid();
        var newDone = Guid.NewGuid();
        var onlyRun = Guid.NewGuid();
        Seed(new Run("Nightly", Older, oldFail, "Failed"), new Run("Nightly", Newer, newDone, "Completed"), new Run("Hourly", Older, onlyRun, "Failed"));

        var result = await Tracker(500).GetLatestRuns(["Nightly", "Hourly"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!["Nightly"].Id.ShouldBe(newDone);
        result.Value["Nightly"].State.Name.ShouldBe("Completed");
        result.Value["Hourly"].Id.ShouldBe(onlyRun);
        result.Value["Hourly"].State.Name.ShouldBe("Failed");
    }

    [Fact]
    public async Task PipelineWithNoRunIsAbsentAndIsNotAFailure()
    {
        Seed(new Run("Nightly", Older, Guid.NewGuid(), "Completed"));

        var result = await Tracker(500).GetLatestRuns(["Nightly", "NeverRun"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ContainsKey("Nightly").ShouldBeTrue();
        result.Value.ContainsKey("NeverRun").ShouldBeFalse();
    }

    [Fact]
    public async Task ThreeRequestsForThreeNamesAndForThirtyNames()
    {
        Seed(Enumerable.Range(0, 30).Select(i => new Run("P" + i, Older, Guid.NewGuid(), "Completed")).ToArray());
        var tracker = Tracker(500);

        await tracker.GetLatestRuns(["P0", "P1", "P2"], TestContext.Current.CancellationToken);
        var forThree = _commands.Count;
        _commands.Clear();
        var result = await tracker.GetLatestRuns(Enumerable.Range(0, 30).Select(i => "P" + i).ToList(), TestContext.Current.CancellationToken);

        forThree.ShouldBe(3);
        _commands.Count.ShouldBe(3);
        result.Value!.Count.ShouldBe(30);
    }

    [Fact]
    public async Task NamesPastTheChunkSizeAreReadInFurtherPiecesOfThreeRequests()
    {
        Seed(Enumerable.Range(0, 25).Select(i => new Run("P" + i, Older, Guid.NewGuid(), "Completed")).ToArray());

        var result = await Tracker(10).GetLatestRuns(Enumerable.Range(0, 25).Select(i => "P" + i).ToList(), TestContext.Current.CancellationToken);

        result.Value!.Count.ShouldBe(25);
        _commands.Count.ShouldBe(9);
    }

    [Fact]
    public async Task EveryReadOfAScopedCallerCarriesItsTenant()
    {
        Seed(new Run("Nightly", Older, Guid.NewGuid(), "Completed"));

        await Tracker(500).GetLatestRuns(["Nightly"], TestContext.Current.CancellationToken);

        _commands.Count.ShouldBe(3);
        foreach (var command in _commands)
        {
            var filter = command switch
            {
                QueryCommand<Dictionary<string, object?>> rows => rows.Filter,
                QueryCommand<ExecutionItem> items => items.Filter,
                _ => null,
            };
            Conditions((filter as FilterExpression)?.Root).ShouldContain(c => c.PropertyName == "TenantId" && Equals(c.Value, TenantA));
        }
    }

    [Fact]
    public async Task ACallerThatReachesEveryTenantReadsWithoutATenantTerm()
    {
        Seed(new Run("Nightly", Older, Guid.NewGuid(), "Completed"));

        await Tracker(500, global: true).GetLatestRuns(["Nightly"], TestContext.Current.CancellationToken);

        foreach (var command in _commands.OfType<QueryCommand<Dictionary<string, object?>>>())
            Conditions((command.Filter as FilterExpression)?.Root).ShouldNotContain(c => c.PropertyName == "TenantId");
    }

    [Fact]
    public async Task TwoRunsWrittenAtTheSameInstantAnswerTheGreaterId()
    {
        var low = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var high = Guid.Parse("00000000-0000-0000-0000-000000000002");
        Seed(new Run("Nightly", Newer, high, "Completed"), new Run("Nightly", Newer, low, "Failed"));

        var result = await Tracker(500).GetLatestRuns(["Nightly"], TestContext.Current.CancellationToken);

        result.Value!["Nightly"].Id.ShouldBe(high);
    }

    [Fact]
    public async Task ARunWhoseItemTheCallerCannotReadLeavesThePipelineWithoutARun()
    {
        Seed(new Run("Nightly", Older, Guid.NewGuid(), "Completed"));

        var result = await Tracker(500, itemsVisible: false).GetLatestRuns(["Nightly"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ContainsKey("Nightly").ShouldBeFalse();
    }

    [Fact]
    public async Task NamesAreMatchedWithoutRegardToCase()
    {
        var id = Guid.NewGuid();
        Seed(new Run("Nightly", Older, id, "Completed"));

        var result = await Tracker(500).GetLatestRuns(["Nightly", "NIGHTLY"], TestContext.Current.CancellationToken);

        result.Value!.Count.ShouldBe(1);
        result.Value["nightly"].Id.ShouldBe(id);
    }

    [Fact]
    public async Task ANewestTimeThatIsNotADateTimeOffsetIsRefusedNamingItsTypeNotReadAsNoTime()
    {
        Seed(new Run("Nightly", Older, Guid.NewGuid(), "Completed"));
        _corrupt = row => { if (row.ContainsKey("NewestAt")) row["NewestAt"] = Older.UtcDateTime; };

        var result = await Tracker(500).GetLatestRuns(["Nightly"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("DateTime");
        result.CurrentMessage.ShouldContain("Nightly");
    }

    [Fact]
    public async Task ARowWhoseCreateDateIsNotADateTimeOffsetIsRefusedNamingItsType()
    {
        Seed(new Run("Nightly", Older, Guid.NewGuid(), "Completed"));
        _corrupt = row => { if (row.ContainsKey("CreateDate")) row["CreateDate"] = "2026-11-07"; };

        var result = await Tracker(500).GetLatestRuns(["Nightly"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("String");
    }

    [Fact]
    public async Task ARowWhoseIdIsNotAGuidIsRefusedNamingItsType()
    {
        Seed(new Run("Nightly", Older, Guid.NewGuid(), "Completed"));
        _corrupt = row => { if (row.ContainsKey("Id")) row["Id"] = "not-a-guid"; };

        var result = await Tracker(500).GetLatestRuns(["Nightly"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("String");
        result.CurrentMessage.ShouldContain("Guid");
    }

    [Fact]
    public async Task ATrackerBuiltWithoutAChunkSizeRefusesAndReadsNothing()
    {
        var result = await Tracker(null).GetLatestRuns(["Nightly"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _commands.ShouldBeEmpty();
    }

    [Fact]
    public async Task ABlankNameIsRefusedAndNothingIsRead()
    {
        var result = await Tracker(500).GetLatestRuns(["Nightly", " "], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        _commands.ShouldBeEmpty();
    }

    [Fact]
    public void TheDeclaredChunkSizeIsAtLeastOne()
    {
        OperationsServiceTypes.LatestRunsChunkSize.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task NoNamesReadNothing()
    {
        var result = await Tracker(500).GetLatestRuns([], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBeEmpty();
        _commands.ShouldBeEmpty();
    }
}
