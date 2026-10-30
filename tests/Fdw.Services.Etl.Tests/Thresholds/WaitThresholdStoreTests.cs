using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Thresholds;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for writing the level of thresholds on waiting runs an owner states (FDW-3161): the old thresholds, their rules and the old level are retired one row at a time and
/// the new level, thresholds and rules are saved in order; and a level whose owner, notification rule or counted pipeline does not exist is refused with nothing written.
/// </summary>
public sealed class WaitThresholdStoreTests
{
    private readonly List<WaitThresholdLevelRecord> _levels = [];
    private readonly List<WaitThresholdRecord> _thresholds = [];
    private readonly List<WaitThresholdRuleRecord> _rules = [];
    private readonly List<ScheduleNameRecord> _schedules = [];
    private readonly List<(IDataCommand Command, string Container)> _writes = [];
    private readonly List<GraphPipelineConfiguration> _pipelines = [new() { Name = "AfcPlayersLoad" }];
    private readonly Mock<IConfigurationGateway> _gateway = new();
    private readonly Mock<IRuleNotifier> _notifier = new();
    private string? _failingSaveOf;

    private void Rows<T>(List<T> rows)
        where T : class
    {
        _gateway.Setup(g => g.Execute<IEnumerable<T>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<T>>.Success(rows));
    }

    private WaitThresholdStore Sut()
    {
        Rows(_levels);
        Rows(_thresholds);
        Rows(_rules);
        Rows(_schedules);
        _gateway.Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget target, CancellationToken _) =>
            {
                _writes.Add((command, target.Container));
                return _failingSaveOf == target.Container && command is not ConfigurationRetireCommand
                    ? GenericResult<ConfigurationWriteResult>.Failure(new GenericMessage("the table is not there"))
                    : GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], null));
            });
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(_gateway.Object));
        var pipelines = new Mock<IPipelineConfigurationProvider>();
        pipelines.Setup(p => p.Get(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success([.. _pipelines]));
        _notifier.Setup(n => n.Exists("Ops email", It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        _notifier.Setup(n => n.Exists(It.Is<string>(name => name != "Ops email"), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Failure(new GenericMessage("no such rule")));
        return new WaitThresholdStore(gateways.Object, pipelines.Object, _notifier.Object, NullLogger<WaitThresholdStore>.Instance);
    }

    private static WaitThresholdPayload Threshold(string name = "Queue full", string[]? rules = null, string? counted = null) => new()
    {
        Name = name, CountedPipeline = counted, Comparison = "GreaterThanOrEqual", Number = 100, Severity = "Error", NotificationRules = rules ?? ["Ops email"], StopsNewRuns = true, IsEnabled = true,
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task WritingAnOwnLevelRetiresTheOldRulesThresholdsOneByOneThenSavesTheLevelTheThresholdAndItsRulesInOrder()
    {
        var oldLevel = new WaitThresholdLevelRecord { Id = Guid.NewGuid(), OwnerKind = "Server", Mode = "Own" };
        var oldThreshold = new WaitThresholdRecord { Id = Guid.NewGuid(), OwnerKind = "Server", Name = "Old", Comparison = "Equal", Severity = "Info" };
        _levels.Add(oldLevel);
        _thresholds.Add(oldThreshold);
        _rules.AddRange([
            new WaitThresholdRuleRecord { Id = Guid.NewGuid(), WaitThresholdId = oldThreshold.Id, RuleName = "A", Ordinal = 0 },
            new WaitThresholdRuleRecord { Id = Guid.NewGuid(), WaitThresholdId = oldThreshold.Id, RuleName = "B", Ordinal = 1 }]);

        var written = await Sut().Write("Server", null, new WaitThresholdLevelPayload { Mode = "Own", Thresholds = [Threshold()] }, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writes.Select(write => write.Container).ShouldBe(
            ["WaitThresholdRule", "WaitThresholdRule", "WaitThreshold", "WaitThresholdLevel", "WaitThreshold", "WaitThresholdRule"]);
        _writes.Take(3).ShouldAllBe(write => write.Command is ConfigurationRetireCommand);
        var level = _writes[3].Command.ShouldBeOfType<ConfigurationSaveCommand<WaitThresholdLevelRecord>>().Data;
        level.Id.ShouldBe(oldLevel.Id);
        level.Mode.ShouldBe("Own");
        var threshold = _writes[4].Command.ShouldBeOfType<ConfigurationSaveCommand<WaitThresholdRecord>>().Data;
        threshold.Name.ShouldBe("Queue full");
        threshold.StopsNewRuns.ShouldBeTrue();
        _writes[5].Command.ShouldBeOfType<ConfigurationSaveCommand<WaitThresholdRuleRecord>>().Data.WaitThresholdId.ShouldBe(threshold.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANotificationRuleThatDoesNotExistIsRefusedNamingItAndNothingIsWritten()
    {
        var level = new WaitThresholdLevelPayload { Mode = "Own", Thresholds = [Threshold(rules: ["Ops fax"])] };

        var written = await Sut().Write("Server", null, level, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("Ops fax");
        _writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineThatDoesNotExistCannotStateALevelAndNothingIsWritten()
    {
        var written = await Sut().Write("Pipeline", "Missing", new WaitThresholdLevelPayload { Mode = "None" }, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("Missing");
        _writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AScheduleThatDoesNotExistCannotStateALevelButOneThatDoesCan()
    {
        _schedules.Add(new ScheduleNameRecord { RowId = 1, Name = "Nightly" });
        var sut = Sut();

        (await sut.Write("Schedule", "Gone", new WaitThresholdLevelPayload { Mode = "None" }, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
        (await sut.Write("Schedule", "nightly", new WaitThresholdLevelPayload { Mode = "None" }, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineThatIsCountedButDoesNotExistIsRefusedNamingItAndNothingIsWritten()
    {
        var level = new WaitThresholdLevelPayload { Mode = "Own", Thresholds = [Threshold(counted: "GhostLoad")] };

        var written = await Sut().Write("Server", null, level, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("GhostLoad");
        _writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task InheritRetiresThePipelinesLevelAndItsThresholdsAndSavesNothing()
    {
        var oldLevel = new WaitThresholdLevelRecord { Id = Guid.NewGuid(), OwnerKind = "Pipeline", OwnerName = "afcplayersload", Mode = "Own" };
        var oldThreshold = new WaitThresholdRecord { Id = Guid.NewGuid(), OwnerKind = "Pipeline", OwnerName = "AfcPlayersLoad", Name = "Old", Comparison = "Equal", Severity = "Info" };
        _levels.Add(oldLevel);
        _thresholds.Add(oldThreshold);

        var written = await Sut().Write("Pipeline", "AfcPlayersLoad", new WaitThresholdLevelPayload { Mode = "Inherit" }, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writes.Select(write => write.Container).ShouldBe(["WaitThreshold", "WaitThresholdLevel"]);
        _writes.ShouldAllBe(write => write.Command is ConfigurationRetireCommand);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASaveThatFailsAfterTheOldRowsWereRetiredFailsNamingTheOwner()
    {
        _failingSaveOf = "WaitThreshold";

        var written = await Sut().Write("Server", null, new WaitThresholdLevelPayload { Mode = "Own", Thresholds = [Threshold()] }, TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain("Server");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ReadingAnOwnerThatStatesNothingAnswersInheritWithNoThresholds()
    {
        var read = await Sut().Read("Pipeline", "AfcPlayersLoad", TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.Mode.ShouldBe("Inherit");
        read.Value.Thresholds.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ReadingALevelAnswersItsModeAndItsThresholdsWithTheRulesInOrder()
    {
        var threshold = new WaitThresholdRecord
        {
            Id = Guid.NewGuid(), OwnerKind = "Server", Name = "Queue full", Comparison = "GreaterThanOrEqual", Number = 100, Severity = "Error", StopsNewRuns = true, IsEnabled = true,
        };
        _levels.Add(new WaitThresholdLevelRecord { Id = Guid.NewGuid(), OwnerKind = "Server", Mode = "Own" });
        _thresholds.Add(threshold);
        _rules.AddRange([
            new WaitThresholdRuleRecord { Id = Guid.NewGuid(), WaitThresholdId = threshold.Id, RuleName = "Second", Ordinal = 1 },
            new WaitThresholdRuleRecord { Id = Guid.NewGuid(), WaitThresholdId = threshold.Id, RuleName = "First", Ordinal = 0 }]);

        var read = await Sut().Read("Server", null, TestContext.Current.CancellationToken);

        read.Value!.Mode.ShouldBe("Own");
        read.Value.Thresholds.Single().NotificationRules.ShouldBe(["First", "Second"]);
        read.Value.Thresholds.Single().StopsNewRuns.ShouldBe(true);
    }
}
