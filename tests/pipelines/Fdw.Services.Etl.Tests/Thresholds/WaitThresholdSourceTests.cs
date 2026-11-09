using System.Collections.Generic;
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
/// Tests for the source of the level of thresholds on waiting runs (FDW-3161, FDW-3975): the level the server, each pipeline and each schedule states is read from
/// configuration; an owner with no level row inherits; and a server that states no level, a mode that does not exist, an owner that does not exist, or rows that cannot be
/// read fail the read naming it, with nothing assumed.
/// </summary>
public sealed class WaitThresholdSourceTests
{
    private readonly List<WaitThresholdLevelRecord> _levels = [];
    private readonly List<ScheduleNameRecord> _schedules = [];
    private readonly List<GraphPipelineConfiguration> _pipelines = [];
    private bool _levelsUnreadable;

    private static WaitThresholdLevelRecord Level(string kind, string? owner, string mode) => new() { Id = Guid.NewGuid(), OwnerKind = kind, OwnerName = owner, Mode = mode };

    private WaitThresholdSource Sut(bool serverLevel = true)
    {
        if (serverLevel)
        {
            _levels.Add(Level("Server", null, "Own"));
        }

        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<WaitThresholdLevelRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_levelsUnreadable
                ? GenericResult<IEnumerable<WaitThresholdLevelRecord>>.Failure(new GenericMessage("the table is not there"))
                : GenericResult<IEnumerable<WaitThresholdLevelRecord>>.Success(_levels));
        gateway.Setup(g => g.Execute<IEnumerable<ScheduleNameRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<ScheduleNameRecord>>.Success(_schedules));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
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
    public async Task ItReadsEachOwnersModeAndAnOwnerWithNoLevelRowInherits()
    {
        _pipelines.AddRange([Pipeline("AfcPlayersLoad"), Pipeline("NflGames")]);
        _schedules.AddRange([new ScheduleNameRecord { Name = "Nightly" }, new ScheduleNameRecord { Name = "Hourly" }]);
        _levels.Add(Level("Pipeline", "AfcPlayersLoad", "None"));
        _levels.Add(Level("Schedule", "Nightly", "Own"));

        var read = await Sut().Read(TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.Server.Mode.Name.ShouldBe("Own");
        read.Value.Pipelines["AfcPlayersLoad"].Mode.Name.ShouldBe("None");
        read.Value.Pipelines["nflgames"].Mode.Name.ShouldBe("Inherit");
        read.Value.Schedules["Nightly"].Mode.Name.ShouldBe("Own");
        read.Value.Schedules["Hourly"].Mode.Name.ShouldBe("Inherit");
        read.Value.Pipelines["AfcPlayersLoad"].Owner.ShouldBe("Pipeline:AfcPlayersLoad");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AServerThatStatesNoLevelOrInheritsFailsTheRead()
    {
        var missing = await Sut(serverLevel: false).Read(TestContext.Current.CancellationToken);
        _levels.Clear();
        _levels.Add(Level("Server", null, "Inherit"));
        var inherits = await Sut(serverLevel: false).Read(TestContext.Current.CancellationToken);

        missing.IsFailure.ShouldBeTrue();
        inherits.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AModeThatIsNotAnOptionOrAnOwnerThatDoesNotExistFailsTheReadNamingIt()
    {
        _pipelines.Add(Pipeline("AfcPlayersLoad"));
        _levels.Add(Level("Pipeline", "AfcPlayersLoad", "Sometimes"));
        var mode = await Sut().Read(TestContext.Current.CancellationToken);

        // Sut() appended the server level after the pipeline's, so the last level is not the one with the bad mode: remove that one by its owner.
        _levels.RemoveAll(level => string.Equals(level.OwnerName, "AfcPlayersLoad", StringComparison.Ordinal));
        _levels.Add(Level("Pipeline", "Ghost", "None"));
        var owner = await Sut(serverLevel: false).Read(TestContext.Current.CancellationToken);

        mode.IsFailure.ShouldBeTrue();
        mode.CurrentMessage.ShouldNotBeNull().ShouldContain("Sometimes");
        owner.IsFailure.ShouldBeTrue();
        owner.CurrentMessage.ShouldNotBeNull().ShouldContain("Ghost");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task LevelsThatCannotBeReadFailTheRead()
    {
        _levelsUnreadable = true;

        var read = await Sut().Read(TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
    }
}
