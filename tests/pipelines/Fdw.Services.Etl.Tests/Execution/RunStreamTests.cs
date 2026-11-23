using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// FDW-6140: the pieces a run gives its person are kept in order with a sequence the client resumes from. A piece is written for the person the run acts as and for nobody else (a run that acts as no person
/// writes none); a read after a sequence answers the pieces that follow it; only the person the pieces are addressed to reads them, or whoever the host's audience names; the two settings are required.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class RunStreamTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Person = Guid.Parse("66666666-0000-0000-0000-000000000006");
    private static readonly Guid Other = Guid.Parse("77777777-0000-0000-0000-000000000007");
    private static readonly Guid First = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Second = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    private readonly string? _before = PipelineTypes.OperationalConnection;
    private readonly List<IDataCommand> _commands = [];
    private readonly List<EtlPipelineRunStreamRecord> _stored = [];
    private readonly Mock<IEffectiveSettingsProvider> _settings = new();
    private readonly List<IRunAudience> _audiences = [];
    private readonly Mock<IPipelineStatusBroadcaster> _broadcaster = new();
    private bool _push;
    private readonly FixedTime _time = new(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));

    public RunStreamTests()
    {
        PipelineTypes.OperationalConnection = "OpsDb";
        SettingIs(SettingDefinitions.RunStreamRetentionSeconds, 3600);
        SettingIs(SettingDefinitions.RunStreamPageSize, 2);
    }

    public void Dispose() => PipelineTypes.OperationalConnection = _before;

    private void SettingIs(string name, int value)
    {
        var read = value;
        _settings.Setup(s => s.TryGetEffectiveValue(name, out read, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(true);
    }

    private void SettingMissing(string name)
    {
        var none = 0;
        _settings.Setup(s => s.TryGetEffectiveValue(name, out none, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(false);
    }

    private RunStream Sut()
    {
        var gateway = new Mock<IDataGateway>();
        gateway.Setup(g => g.Execute<int>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken __) =>
            {
                _commands.Add(command);
                if (command is InsertCommand<EtlPipelineRunStreamRecord> insert)
                {
                    foreach (var row in insert.Data)
                    {
                        row.RowId = _stored.Count + 1;
                        _stored.Add(row);
                    }
                }

                return GenericResult<int>.Success(1);
            });
        gateway.Setup(g => g.Execute<IEnumerable<EtlPipelineRunStreamRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<IEnumerable<EtlPipelineRunStreamRecord>>.Success(_stored.OrderBy(row => row.RowId).Where(row => row.RowId > _after).Take(_take).ToList()));
        var gateways = new Mock<IDataGatewayProvider>();
        gateways.Setup(p => p.ByName(It.IsAny<string>())).Returns(gateway.Object);
        return new RunStream(gateways.Object, _settings.Object, _audiences, _time, NullLogger<RunStream>.Instance, _push ? _broadcaster.Object : null);
    }

    // The fake store answers what the query asked for, which the test states: the rows after this sequence, at most this many.
    private long _after;
    private int _take = int.MaxValue;

    private static FlowRunIdentity Run(Guid execution, Guid? person, Guid? continues = null) =>
        new(execution, Tenant, ["etl-runner"]) { PersonId = person, ContinuesRun = continues };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APieceIsWrittenForThePersonTheRunActsAsUnderTheFirstRunOfItsChain()
    {
        var written = await Sut().Write(Run(Second, Person, continues: First), RunStreamKinds.Text, "answer", "Hello", TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        var row = _stored.ShouldHaveSingleItem();
        row.PersonId.ShouldBe(Person);
        row.ClaimRootId.ShouldBe(First);
        row.ExecutionId.ShouldBe(Second);
        row.Text.ShouldBe("Hello");
        row.CreatedAt.ShouldBe(_time.GetUtcNow());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AWrittenPieceIsPushedToItsPersonWithTheSequenceTheTableGaveIt()
    {
        _push = true;
        PipelineRunPieceUpdate? pushed = null;
        _broadcaster.Setup(b => b.BroadcastRunPiece(It.IsAny<PipelineRunPieceUpdate>())).Callback<PipelineRunPieceUpdate>(piece => pushed = piece).Returns(Task.CompletedTask);

        var written = await Sut().Write(Run(Second, Person, continues: First), RunStreamKinds.StepEnded, "send", "Succeeded", TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
        var update = pushed.ShouldNotBeNull();
        update.PersonId.ShouldBe(Person);
        update.ClaimRootId.ShouldBe(First);
        update.Sequence.ShouldBe(_stored.Single().RowId);
        update.Kind.ShouldBe(RunStreamKinds.StepEnded);
        update.Text.ShouldBe("Succeeded");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARunThatActsAsNoPersonPushesNothingEither()
    {
        _push = true;

        await Sut().Write(Run(First, null), RunStreamKinds.Text, null, "Hello", TestContext.Current.CancellationToken);

        _broadcaster.Verify(b => b.BroadcastRunPiece(It.IsAny<PipelineRunPieceUpdate>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARunThatActsAsNoPersonWritesNothingBecauseNobodyFollowsIt()
    {
        var written = await Sut().Write(Run(First, null), RunStreamKinds.Text, "answer", "Hello", TestContext.Current.CancellationToken);

        written.IsSuccess.ShouldBeTrue();
        _commands.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task APieceIsNotWrittenWhileTheRetentionIsNotSet()
    {
        SettingMissing(SettingDefinitions.RunStreamRetentionSeconds);

        var written = await Sut().Write(Run(First, Person), RunStreamKinds.Text, null, "Hello", TestContext.Current.CancellationToken);

        written.IsFailure.ShouldBeTrue();
        written.CurrentMessage.ShouldNotBeNull().ShouldContain(SettingDefinitions.RunStreamRetentionSeconds);
        _commands.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepStartingSweepsThePiecesPastTheRetentionBeforeItWrites()
    {
        await Sut().Write(Run(First, Person), RunStreamKinds.StepStarted, "step", null, TestContext.Current.CancellationToken);

        _commands.Count.ShouldBe(2);
        _commands[0].ShouldBeOfType<DeleteCommand>();
        _commands[1].ShouldBeOfType<InsertCommand<EtlPipelineRunStreamRecord>>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AReadAfterASequenceAnswersThePiecesThatFollowItInOrderAndAtMostThePageSize()
    {
        var sut = Sut();
        foreach (var text in new[] { "a", "b", "c" })
        {
            await sut.Write(Run(First, Person), RunStreamKinds.Text, null, text, TestContext.Current.CancellationToken);
        }

        _after = 0;
        _take = 2;
        var firstPage = await sut.ReadAfter(Tenant, Person, First, 0, null, TestContext.Current.CancellationToken);
        _after = firstPage.Value!.Last().Sequence;
        _take = 2;
        var secondPage = await sut.ReadAfter(Tenant, Person, First, firstPage.Value!.Last().Sequence, null, TestContext.Current.CancellationToken);

        firstPage.Value!.Select(piece => piece.Text).ShouldBe(["a", "b"]);
        secondPage.Value!.Select(piece => piece.Text).ShouldBe(["c"], "nothing repeated, nothing lost");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnotherPersonIsRefusedThePiecesAndGetsNone()
    {
        var sut = Sut();
        await sut.Write(Run(First, Person), RunStreamKinds.Text, null, "private", TestContext.Current.CancellationToken);

        var read = await sut.ReadAfter(Tenant, Other, First, 0, null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.Messages.ShouldHaveSingleItem().Code.ShouldBe("PIPERUNSTREAM-40850");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonTheHostsAudienceNamesMayFollowAndAnAudienceThatCannotAnswerIsNotAYes()
    {
        var audience = new Mock<IRunAudience>();
        audience.Setup(a => a.MayFollow(Other, Tenant, First, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<bool>.Success(true));
        var broken = new Mock<IRunAudience>();
        broken.Setup(a => a.MayFollow(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Failure(new GenericMessage("the audience store is down")));
        _audiences.Add(broken.Object);
        var withoutOne = Sut();
        await withoutOne.Write(Run(First, Person), RunStreamKinds.Text, null, "shared", TestContext.Current.CancellationToken);
        (await withoutOne.ReadAfter(Tenant, Other, First, 0, null, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();

        // The stream takes the host's audiences when it is made, so the one that says yes is there only for a stream made after it was added.
        _audiences.Add(audience.Object);
        var read = await Sut().ReadAfter(Tenant, Other, First, 0, null, TestContext.Current.CancellationToken);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value.ShouldHaveSingleItem().Text.ShouldBe("shared");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheStreamIsNotReadWhileThePageSizeIsNotSet()
    {
        SettingMissing(SettingDefinitions.RunStreamPageSize);

        var read = await Sut().ReadAfter(Tenant, Person, First, 0, null, TestContext.Current.CancellationToken);

        read.IsFailure.ShouldBeTrue();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain(SettingDefinitions.RunStreamPageSize);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
