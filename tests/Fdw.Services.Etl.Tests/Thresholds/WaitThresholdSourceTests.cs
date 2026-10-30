using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Thresholds;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for the source of thresholds on waiting runs (FDW-3161): the level the server, each pipeline and each schedule states and the thresholds each owns are read from
/// configuration; an owner with no level row inherits; and a server that states no level, a mode or option that does not exist, an owner that does not exist, or rows that
/// cannot be read fail the read naming it, with nothing assumed.
/// </summary>
public sealed class WaitThresholdSourceTests
{
    private readonly List<WaitThresholdLevelRecord> _levels = [];
    private readonly List<WaitThresholdRecord> _thresholds = [];
    private readonly List<WaitThresholdRuleRecord> _rules = [];
    private readonly List<ScheduleNameRecord> _schedules = [];
    private readonly List<GraphPipelineConfiguration> _pipelines = [];
    private readonly Mock<IConfigurationGateway> _gateway = new();
    private string? _failingContainerReads;

    private static WaitThresholdLevelRecord Level(string kind, string? owner, string mode) => new() { Id = Guid.NewGuid(), OwnerKind = kind, OwnerName = owner, Mode = mode };

    private static WaitThresholdRecord Row(string name, string kind = "Server", string? owner = null, string comparison = "GreaterThanOrEqual", string severity = "Warning", string? counted = null) => new()
    {
        Id = Guid.NewGuid(), OwnerKind = kind, OwnerName = owner, Name = name, CountedPipeline = counted, Comparison = comparison, Number = 50, Severity = severity,
        StopsNewRuns = true, IsEnabled = true,
    };

    private void Rows<T>(List<T> rows)
        where T : class
    {
        _gateway.Setup(g => g.Execute<IEnumerable<T>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand _, DataStoreTarget target, CancellationToken _) =>
                _failingContainerReads == target.Container
                    ? GenericResult<IEnumerable<T>>.Failure(new GenericMessage("the table is not there"))
                    : GenericResult<IEnumerable<T>>.Success(rows));
    }

    private WaitThresholdSource Sut(bool serverLevel = true)
    {
        if (serverLevel)
        {
            _levels.Add(Level("Server", null, "Own"));
        }

        Rows(_levels);
        Rows(_thresholds);
        Rows(_rules);
        Rows(_schedules);
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(_gateway.Object));
        var pipelines = new Mock<IPipelineConfigurationProvider>();
        pipelines.Setup(p => p.Get(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success([.. _pipelines]));
        var accessor = new Mock<IAuthenticationContextAccessor>();
        accessor.SetupProperty(a => a.Current);
        var scopes = new ServiceCollection().AddSingleton(gateways.Object).AddSingleton(pipelines.Object).AddSingleton(accessor.Object).BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();
        return new WaitThresholdSource(scopes, NullLogger<WaitThresholdSource>.Instance);
    }

    private static GraphPipelineConfiguration Pipeline(string name) => new() { Name = name };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ItReadsEachOwnersLevelAndThresholdsWithTheirRulesInOrderAndAnOwnerWithNoLevelRowInherits()
    {
        var queueLong = Row("Queue getting long");
        var afc = Row("AFC backlog", "Pipeline", "AfcPlayersLoad", counted: "AfcPlayersLoad");
        _thresholds.AddRange([queueLong, afc]);
        _rules.AddRange([
            new WaitThresholdRuleRecord { Id = Guid.NewGuid(), WaitThresholdId = queueLong.Id, RuleName = "Ops Teams", Ordinal = 1 },
            new WaitThresholdRuleRecord { Id = Guid.NewGuid(), WaitThresholdId = queueLong.Id, RuleName = "Ops email", Ordinal = 0 }]);
        _levels.Add(Level("Pipeline", "AfcPlayersLoad", "Own"));
        _levels.Add(Level("Schedule", "Nightly", "None"));
        _pipelines.AddRange([Pipeline("AfcPlayersLoad"), Pipeline("NfcPlayersLoad")]);
        _schedules.AddRange([new ScheduleNameRecord { RowId = 7, Name = "Nightly" }, new ScheduleNameRecord { RowId = 8, Name = "Hourly" }]);

        var read = await Sut().Read(TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        var configuration = read.Value!;
        configuration.Server.Mode.Name.ShouldBe("Own");
        configuration.Server.Thresholds.Single().NotificationRules.ShouldBe(["Ops email", "Ops Teams"]);
        configuration.Pipelines["AfcPlayersLoad"].Mode.Name.ShouldBe("Own");
        configuration.Pipelines["afcplayersload"].Owner.ShouldBe("Pipeline:AfcPlayersLoad");
        configuration.Pipelines["AfcPlayersLoad"].Thresholds.Single().CountedPipeline.ShouldBe("AfcPlayersLoad");
        configuration.Pipelines["NfcPlayersLoad"].Mode.Name.ShouldBe("Inherit");
        configuration.Pipelines["NfcPlayersLoad"].Thresholds.ShouldBeEmpty();
        configuration.Schedules["Nightly"].Mode.Name.ShouldBe("None");
        configuration.Schedules["Hourly"].Mode.Name.ShouldBe("Inherit");
        configuration.All().Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AServerWithNoLevelRowFailsNamingItAndAssumesNone()
    {
        var read = await Sut(serverLevel: false).Read(TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("WaitThresholdLevel");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AServerThatStatesInheritFailsBecauseItHasNoLevelAboveIt()
    {
        var sut = Sut(serverLevel: false);
        _levels.Add(Level("Server", null, "Inherit"));

        var read = await sut.Read(TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("Inherit");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineThatStatesAModeThatDoesNotExistFailsNamingThePipelineAndTheMode()
    {
        _pipelines.Add(Pipeline("AfcPlayersLoad"));
        _levels.Add(Level("Pipeline", "AfcPlayersLoad", "Sometimes"));

        var read = await Sut().Read(TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("AfcPlayersLoad");
        read.CurrentMessage.ShouldContain("Sometimes");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AThresholdThatStatesAComparisonThatDoesNotExistFailsNamingTheThreshold()
    {
        _thresholds.Add(Row("Queue long", comparison: "MuchBigger"));

        var read = await Sut().Read(TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("Queue long");
        read.CurrentMessage.ShouldContain("MuchBigger");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AThresholdOwnedByAPipelineThatDoesNotExistFailsInsteadOfLyingUnused()
    {
        _thresholds.Add(Row("Ghost backlog", "Pipeline", "DeletedPipeline"));
        _pipelines.Add(Pipeline("AfcPlayersLoad"));

        var read = await Sut().Read(TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("DeletedPipeline");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALevelOwnedByAScheduleThatDoesNotExistFailsNamingIt()
    {
        _levels.Add(Level("Schedule", "GoneNightly", "None"));

        var read = await Sut().Read(TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("GoneNightly");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AThresholdOwnedByAKindThatDoesNotExistFailsNamingTheKind()
    {
        _thresholds.Add(Row("Odd", "Department", "Sales"));

        var read = await Sut().Read(TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("Department");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ThresholdRowsThatCannotBeReadFailTheReadNamingTheTable()
    {
        _failingContainerReads = "WaitThreshold";

        var read = await Sut().Read(TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain("WaitThreshold");
    }
}
