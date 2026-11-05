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
/// Tests for retiring the level an owner states when the owner is deleted (FDW-3161, FDW-3975): the owner's level is retired and no other owner's, an owner with no level
/// writes nothing, and a level that cannot be read or retired fails naming the owner.
/// </summary>
public sealed class WaitThresholdOwnerReleaseTests
{
    private readonly List<WaitThresholdLevelRecord> _levels = [];
    private readonly List<(IDataCommand Command, string Container)> _retired = [];
    private bool _levelsUnreadable;
    private bool _retireFails;

    private WaitThresholdOwnerRelease Sut()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<WaitThresholdLevelRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_levelsUnreadable
                ? GenericResult<IEnumerable<WaitThresholdLevelRecord>>.Failure(new GenericMessage("the table is not there"))
                : GenericResult<IEnumerable<WaitThresholdLevelRecord>>.Success(_levels));
        gateway.Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget target, CancellationToken _) =>
            {
                _retired.Add((command, target.Container));
                return _retireFails
                    ? GenericResult<ConfigurationWriteResult>.Failure(new GenericMessage("the table is not there"))
                    : GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], null));
            });
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return new WaitThresholdOwnerRelease(gateways.Object);
    }

    private void Level(string ownerKind, string? ownerName) =>
        _levels.Add(new WaitThresholdLevelRecord { Id = Guid.NewGuid(), OwnerKind = ownerKind, OwnerName = ownerName, Mode = "Own" });

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelinesLevelIsRetiredAndNoOtherOwnersIs()
    {
        Level("Pipeline", "AfcPlayersLoad");
        Level("Pipeline", "Other");
        Level("Server", null);

        var released = await Sut().ReleasePipeline("afcplayersload", TestContext.Current.CancellationToken);

        released.IsSuccess.ShouldBeTrue(released.CurrentMessage);
        _retired.Count.ShouldBe(1);
        _retired[0].Container.ShouldBe("WaitThresholdLevel");
        _retired[0].Command.ShouldBeOfType<ConfigurationRetireCommand>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AScheduleWithNoLevelWritesNothing()
    {
        Level("Pipeline", "AfcPlayersLoad");

        var released = await Sut().ReleaseSchedule("Nightly", TestContext.Current.CancellationToken);

        released.IsSuccess.ShouldBeTrue(released.CurrentMessage);
        _retired.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ALevelThatCannotBeReadOrRetiredFailsAndARetireFailureNamesTheOwner()
    {
        _levelsUnreadable = true;
        var unreadable = await Sut().ReleasePipeline("AfcPlayersLoad", TestContext.Current.CancellationToken);

        _levelsUnreadable = false;
        Level("Pipeline", "AfcPlayersLoad");
        _retireFails = true;
        var failed = await Sut().ReleasePipeline("AfcPlayersLoad", TestContext.Current.CancellationToken);

        unreadable.IsFailure.ShouldBeTrue();
        failed.IsFailure.ShouldBeTrue();
        failed.CurrentMessage.ShouldNotBeNull().ShouldContain("AfcPlayersLoad");
    }
}
