using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Thresholds;
using Moq;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for removing the level of thresholds on waiting runs of a pipeline or schedule that is being deleted (FDW-3271): the owner's rule rows, threshold rows and level row are
/// retired one by one; a pipeline that a threshold of another owner counts is refused, naming the threshold and its owner, with nothing removed; an owner with no rows costs no write.
/// </summary>
public sealed class WaitThresholdOwnerReleaseTests
{
    private readonly List<WaitThresholdLevelRecord> _levels = [];
    private readonly List<WaitThresholdRecord> _thresholds = [];
    private readonly List<WaitThresholdRuleRecord> _rules = [];
    private readonly List<string> _retired = [];
    private bool _levelsUnreadable;
    private string? _failingRetireOf;

    private static void Rows<T>(Mock<IConfigurationGateway> gateway, List<T> rows, bool unreadable = false)
        where T : class
    {
        gateway.Setup(g => g.Execute<IEnumerable<T>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(unreadable
                ? GenericResult<IEnumerable<T>>.Failure(new GenericMessage("the table is not there"))
                : GenericResult<IEnumerable<T>>.Success(rows));
    }

    private WaitThresholdOwnerRelease Sut()
    {
        var gateway = new Mock<IConfigurationGateway>();
        Rows(gateway, _levels, _levelsUnreadable);
        Rows(gateway, _thresholds);
        Rows(gateway, _rules);
        gateway.Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget target, CancellationToken _) =>
            {
                _retired.Add(target.Container);
                return command is ConfigurationRetireCommand && _failingRetireOf == target.Container
                    ? GenericResult<ConfigurationWriteResult>.Failure(new GenericMessage("the table is not there"))
                    : GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], null));
            });
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return new WaitThresholdOwnerRelease(gateways.Object);
    }

    private WaitThresholdRecord Threshold(string ownerKind, string? ownerName, string name, string? counted = null)
    {
        var row = new WaitThresholdRecord { Id = Guid.NewGuid(), OwnerKind = ownerKind, OwnerName = ownerName, Name = name, CountedPipeline = counted, Comparison = "Equal", Severity = "Info" };
        _thresholds.Add(row);
        return row;
    }

    private void Level(string ownerKind, string? ownerName) =>
        _levels.Add(new WaitThresholdLevelRecord { Id = Guid.NewGuid(), OwnerKind = ownerKind, OwnerName = ownerName, Mode = "Own" });

    private void Rule(WaitThresholdRecord threshold, string name, int ordinal) =>
        _rules.Add(new WaitThresholdRuleRecord { Id = Guid.NewGuid(), WaitThresholdId = threshold.Id, RuleName = name, Ordinal = ordinal });

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineWithALevelHasItsRulesThresholdsAndLevelRetiredOneByOneRulesFirst()
    {
        Level("Pipeline", "AfcPlayersLoad");
        var first = Threshold("Pipeline", "afcplayersload", "Backlog");
        var second = Threshold("Pipeline", "AfcPlayersLoad", "Stuck", counted: "AfcPlayersLoad");
        Rule(first, "Ops email", 0);
        Rule(first, "Ops chat", 1);
        Rule(second, "Ops email", 0);

        var released = await Sut().ReleasePipeline("AfcPlayersLoad", TestContext.Current.CancellationToken);

        released.IsSuccess.ShouldBeTrue(released.CurrentMessage);
        _retired.ShouldBe(["WaitThresholdRule", "WaitThresholdRule", "WaitThresholdRule", "WaitThreshold", "WaitThreshold", "WaitThresholdLevel"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineThatAThresholdOfAnotherOwnerCountsIsRefusedNamingTheThresholdAndItsOwnerAndNothingIsRemoved()
    {
        Level("Pipeline", "AfcPlayersLoad");
        Threshold("Pipeline", "AfcPlayersLoad", "Own backlog");
        Threshold("Server", null, "AfcPlayersLoad backlog", counted: "afcplayersload");

        var released = await Sut().ReleasePipeline("AfcPlayersLoad", TestContext.Current.CancellationToken);

        released.IsFailure.ShouldBeTrue();
        released.CurrentMessage.ShouldNotBeNull().ShouldContain("AfcPlayersLoad backlog");
        released.CurrentMessage.ShouldContain("Server");
        _retired.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ThePipelinesOwnThresholdThatCountsItselfDoesNotRefuseTheDelete()
    {
        Threshold("Pipeline", "AfcPlayersLoad", "Itself", counted: "AfcPlayersLoad");

        var released = await Sut().ReleasePipeline("AfcPlayersLoad", TestContext.Current.CancellationToken);

        released.IsSuccess.ShouldBeTrue(released.CurrentMessage);
        _retired.ShouldBe(["WaitThreshold"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AThresholdOfASchedulePointingAtThePipelineNamesTheScheduleAsItsOwner()
    {
        Threshold("Schedule", "Nightly", "Pile-up", counted: "AfcPlayersLoad");

        var released = await Sut().ReleasePipeline("AfcPlayersLoad", TestContext.Current.CancellationToken);

        released.IsFailure.ShouldBeTrue();
        released.CurrentMessage.ShouldNotBeNull().ShouldContain("Schedule 'Nightly'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AScheduleHasItsLevelAndThresholdsRetiredAndIsNotRefusedByAPipelineOfTheSameNameThatIsCounted()
    {
        Level("Schedule", "AfcPlayersLoad");
        Threshold("Schedule", "AfcPlayersLoad", "Late");
        Threshold("Server", null, "Counts the pipeline of that name", counted: "AfcPlayersLoad");

        var released = await Sut().ReleaseSchedule("AfcPlayersLoad", TestContext.Current.CancellationToken);

        released.IsSuccess.ShouldBeTrue(released.CurrentMessage);
        _retired.ShouldBe(["WaitThreshold", "WaitThresholdLevel"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnOwnerThatStatesNothingCostsNoWriteAndTheRowsOfOtherOwnersAreLeftAlone()
    {
        Level("Pipeline", "Other");
        Threshold("Pipeline", "Other", "Not mine");

        var released = await Sut().ReleasePipeline("AfcPlayersLoad", TestContext.Current.CancellationToken);

        released.IsSuccess.ShouldBeTrue(released.CurrentMessage);
        _retired.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RowsThatCannotBeReadFailTheReleaseAndRemoveNothing()
    {
        _levelsUnreadable = true;

        var released = await Sut().ReleasePipeline("AfcPlayersLoad", TestContext.Current.CancellationToken);

        released.IsFailure.ShouldBeTrue();
        _retired.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARetireThatFailsFailsTheReleaseNamingTheOwnerAndStopsThere()
    {
        Level("Schedule", "Nightly");
        Threshold("Schedule", "Nightly", "Late");
        _failingRetireOf = "WaitThreshold";

        var released = await Sut().ReleaseSchedule("Nightly", TestContext.Current.CancellationToken);

        released.IsFailure.ShouldBeTrue();
        released.CurrentMessage.ShouldNotBeNull().ShouldContain("Schedule:Nightly");
        _retired.Last().ShouldBe("WaitThreshold");
        _retired.ShouldNotContain("WaitThresholdLevel");
    }
}
