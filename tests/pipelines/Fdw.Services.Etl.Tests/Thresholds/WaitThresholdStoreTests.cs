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
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.Services.Etl.Tests.Thresholds;

/// <summary>
/// Tests for reading and writing the level an owner states (FDW-3161, FDW-3975): a level is read as its row's mode, or Inherit when the owner has no row; a write saves the
/// level over the old row, or retires the row for Inherit; and a mode the owner cannot state, an owner that does not exist, or a save that fails is refused with nothing assumed.
/// </summary>
public sealed class WaitThresholdStoreTests
{
    private readonly List<WaitThresholdLevelRecord> _levels = [];
    private readonly List<ScheduleNameRecord> _schedules = [];
    private readonly List<(IDataCommand Command, string Container)> _writes = [];
    private readonly List<GraphPipelineConfiguration> _pipelines = [new() { Name = "AfcPlayersLoad" }];
    private readonly Mock<IConfigurationGateway> _gateway = new();
    private bool _saveFails;

    private void Rows<T>(List<T> rows)
        where T : class
    {
        _gateway.Setup(g => g.Execute<IEnumerable<T>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<T>>.Success(rows));
    }

    private WaitThresholdStore Sut()
    {
        Rows(_levels);
        Rows(_schedules);
        _gateway.Setup(g => g.Execute<ConfigurationWriteResult>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget target, CancellationToken _) =>
            {
                _writes.Add((command, target.Container));
                return _saveFails && command is not ConfigurationRetireCommand
                    ? GenericResult<ConfigurationWriteResult>.Failure(new GenericMessage("the table is not there"))
                    : GenericResult<ConfigurationWriteResult>.Success(new ConfigurationWriteResult([], null));
            });
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(_gateway.Object));
        var pipelines = new Mock<IPipelineConfigurationProvider>();
        pipelines.Setup(p => p.Get(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success([.. _pipelines]));
        return new WaitThresholdStore(gateways.Object, pipelines.Object, NullLogger<WaitThresholdStore>.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task WritingAnOwnLevelSavesItOverTheOldRowKeepingItsId()
    {
        var old = new WaitThresholdLevelRecord { Id = Guid.NewGuid(), OwnerKind = "Server", Mode = "None" };
        _levels.Add(old);

        var written = await Sut().Write("Server", null, new WaitThresholdLevelPayload { Mode = "Own" }, TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        _writes.Count.ShouldBe(1);
        _writes[0].Container.ShouldBe("WaitThresholdLevel");
        var level = _writes[0].Command.ShouldBeOfType<ConfigurationSaveCommand<WaitThresholdLevelRecord>>().Data;
        level.Id.ShouldBe(old.Id);
        level.OwnerKind.ShouldBe("Server");
        level.Mode.ShouldBe("Own");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task WritingInheritRetiresTheOwnersRowAndWritesNothingWhenThereIsNone()
    {
        var old = new WaitThresholdLevelRecord { Id = Guid.NewGuid(), OwnerKind = "Pipeline", OwnerName = "afcplayersload", Mode = "Own" };
        _levels.Add(old);
        var sut = Sut();

        await sut.Write("Pipeline", "AfcPlayersLoad", new WaitThresholdLevelPayload { Mode = "Inherit" }, TestContext.Current.CancellationToken);
        _writes.Single().Command.ShouldBeOfType<ConfigurationRetireCommand>();

        _levels.Clear();
        _writes.Clear();
        await sut.Write("Pipeline", "AfcPlayersLoad", new WaitThresholdLevelPayload { Mode = "Inherit" }, TestContext.Current.CancellationToken);
        _writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APipelineOrScheduleThatDoesNotExistCannotStateALevelAndNothingIsWritten()
    {
        var sut = Sut();

        var pipeline = await sut.Write("Pipeline", "Missing", new WaitThresholdLevelPayload { Mode = "None" }, TestContext.Current.CancellationToken);
        var schedule = await sut.Write("Schedule", "Gone", new WaitThresholdLevelPayload { Mode = "None" }, TestContext.Current.CancellationToken);

        pipeline.IsFailure.ShouldBeTrue();
        pipeline.CurrentMessage.ShouldNotBeNull().ShouldContain("Missing");
        schedule.IsFailure.ShouldBeTrue();
        schedule.CurrentMessage.ShouldNotBeNull().ShouldContain("Gone");
        _writes.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AModeTheOwnerCannotStateIsRefusedAndAFailedSaveIsReportedNamingTheOwner()
    {
        var refused = await Sut().Write("Server", null, new WaitThresholdLevelPayload { Mode = "Inherit" }, TestContext.Current.CancellationToken);
        _saveFails = true;
        var failed = await Sut().Write("Server", null, new WaitThresholdLevelPayload { Mode = "Own" }, TestContext.Current.CancellationToken);

        refused.IsFailure.ShouldBeTrue();
        failed.IsFailure.ShouldBeTrue();
        failed.CurrentMessage.ShouldNotBeNull().ShouldContain("Server");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnOwnerIsReadAsItsRowsModeOrInheritWhenItHasNoRow()
    {
        _levels.Add(new WaitThresholdLevelRecord { Id = Guid.NewGuid(), OwnerKind = "Pipeline", OwnerName = "AfcPlayersLoad", Mode = "None" });
        var sut = Sut();

        var stated = await sut.Read("Pipeline", "afcplayersload", TestContext.Current.CancellationToken);
        _schedules.Add(new ScheduleNameRecord { Name = "Nightly" });
        var silent = await sut.Read("Schedule", "Nightly", TestContext.Current.CancellationToken);

        stated.Value!.Mode.ShouldBe("None");
        silent.Value!.Mode.ShouldBe("Inherit");
    }
}
