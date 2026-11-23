using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution.Claims;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Claims;

/// <summary>
/// FDW-6629: the claim service calls the three OpsDb procedures through the data gateway and nothing else: a command carrying scalar parameters (the keys as ONE json text), against the stored
/// procedure container of the operational connection. It answers what the procedure answered and fails loudly, with a code, when the procedure answers nothing it can read.
/// </summary>
public sealed class PipelineRunClaimServiceTests
{
    private const string Connection = "OpsDb";
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Pipeline = Guid.Parse("44444444-0000-0000-0000-000000000004");
    private static readonly Guid Execution = Guid.Parse("33333333-0000-0000-0000-000000000003");

    private readonly Mock<IDataGateway> _gateway = new(MockBehavior.Strict);
    private IDataCommand? _sent;
    private DataStoreTarget? _target;

    private PipelineRunClaimService Sut(string? connection = Connection) =>
        new(_gateway.Object, connection, new PipelineRunClaimSettings(60, 20, 5, 30000), NullLogger<PipelineRunClaimService>.Instance);

    private static Dictionary<string, object?> Row(params (string Column, object? Value)[] values) =>
        values.ToDictionary(v => v.Column, v => v.Value, StringComparer.OrdinalIgnoreCase);

    private void Answers(params Dictionary<string, object?>[] rows) =>
        _gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .Callback<IDataCommand, DataStoreTarget, CancellationToken>((command, target, _) =>
            {
                _sent = command;
                _target = target;
            })
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));

    private static PipelineRunClaimRequest Request(params PipelineRunClaimKey[] keys) =>
        new(Tenant, Execution, Pipeline, keys, "FdwReference.Etl.Server", "a");

    private static PipelineRunClaimKey Write(string container) =>
        PipelineRunClaimKeys.Write("NflDb", "dbo", container, NullLogger.Instance).Value.ShouldNotBeNull();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AClaimCallsTheClaimProcedureOfTheOperationalConnectionWithTheKeysAsOneJsonTextAndEveryValueAParameter()
    {
        Answers(Row(("ExecutionId", Execution), ("State", "Claimed"), ("Ahead", 0)));

        var outcome = await Sut().Claim(Request(Write("Teams"), PipelineRunClaimKeys.PipelineCap(Pipeline, 2, NullLogger.Instance).Value!), TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        _target.ShouldBe(new DataStoreTarget(Connection, "etl", "ClaimPipelineRun"));
        var call = _sent.ShouldBeAssignableTo<IExecuteProcedureCommand>().ShouldNotBeNull();
        call.Parameters["TenantId"].ShouldBe(Tenant);
        call.Parameters["ExecutionId"].ShouldBe(Execution);
        call.Parameters["PipelineId"].ShouldBe(Pipeline);
        call.Parameters["ExecutingHost"].ShouldBe("FdwReference.Etl.Server");
        call.Parameters["ExecutingSlot"].ShouldBe("a");
        call.Parameters["LeaseSeconds"].ShouldBe(60);
        call.Parameters["LockTimeoutMilliseconds"].ShouldBe(30000);

        using var keys = JsonDocument.Parse((string)call.Parameters["Keys"]!);
        keys.RootElement.GetArrayLength().ShouldBe(2);
        keys.RootElement[0].GetProperty("keyType").GetString().ShouldBe("WriteKey");
        keys.RootElement[0].GetProperty("claimKey").GetString().ShouldBe("NflDb/dbo/Teams");
        keys.RootElement[1].GetProperty("keyType").GetString().ShouldBe("PipelineCap");
        keys.RootElement[1].GetProperty("cap").GetInt32().ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task ARunThatWaitsIsAnsweredWaitingAndHowManyRunsAreAheadOfIt()
    {
        Answers(Row(("ExecutionId", Execution), ("State", "Waiting"), ("Ahead", 3)));

        var outcome = await Sut().Claim(Request(Write("Teams")), TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        outcome.Value.ShouldNotBeNull().State.Name.ShouldBe("Waiting");
        outcome.Value.Ahead.ShouldBe(3);
    }

    // What the recovery of half-done runs is handed (FDW-7403): a sweep renews no run, so its first result set is empty and the lapsed rows are all the procedure answers.
    private sealed class RecordingLapsedClaims : IPipelineLapsedClaims
    {
        public List<PipelineRunLapsedClaim> Handed { get; } = [];

        public void Seen(IReadOnlyList<PipelineRunLapsedClaim> lapsed) => Handed.AddRange(lapsed);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task ASweepThatRenewsNoRunHandsTheLapsedRowsOfItsAnswerToTheRecovery()
    {
        var lapsed = Guid.Parse("55555555-0000-0000-0000-000000000005");
        Answers(Row(("ExecutionId", lapsed), ("TenantId", Tenant), ("PipelineId", Pipeline), ("ExecutingHost", "FdwReference.Etl.Server"), ("ExecutingSlot", null), ("State", "Claimed")));
        var recovery = new RecordingLapsedClaims();
        var sut = new PipelineRunClaimService(_gateway.Object, Connection, new PipelineRunClaimSettings(60, 20, 5, 30000), NullLogger<PipelineRunClaimService>.Instance, recovery);

        var swept = await sut.Renew([], "FdwReference.Etl.Server", null, TestContext.Current.CancellationToken);

        swept.IsSuccess.ShouldBeTrue(swept.CurrentMessage);
        swept.Value.ShouldNotBeNull().Renewals.ShouldBeEmpty();
        var handed = recovery.Handed.ShouldHaveSingleItem();
        handed.ExecutionId.ShouldBe(lapsed);
        handed.TenantId.ShouldBe(Tenant);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TheLapsedClaimsOfOtherRunsAreReturnedWithTheAnswerSoTheEngineCanResumeThem()
    {
        var lapsed = Guid.Parse("55555555-0000-0000-0000-000000000005");
        Answers(
            Row(("ExecutionId", Execution), ("State", "Claimed"), ("Ahead", 0)),
            Row(("ExecutionId", lapsed), ("TenantId", Tenant), ("PipelineId", Pipeline), ("ExecutingHost", "FdwReference.Etl.Server"), ("ExecutingSlot", "b"), ("State", "Claimed")));

        var outcome = await Sut().Claim(Request(Write("Teams")), TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        var returned = outcome.Value.ShouldNotBeNull().Lapsed.ShouldHaveSingleItem();
        returned.ExecutionId.ShouldBe(lapsed);
        returned.ExecutingSlot.ShouldBe("b");
        returned.State.ShouldBe("Claimed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task ARunThatClaimsNothingIsClaimedAtOnceAndTheGatewayIsNotCalled()
    {
        var outcome = await Sut().Claim(Request(), TestContext.Current.CancellationToken);

        outcome.IsSuccess.ShouldBeTrue(outcome.CurrentMessage);
        outcome.Value.ShouldNotBeNull().State.Name.ShouldBe("Claimed");
        _gateway.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AFailureOfTheProcedureIsReturnedAndNeverSwallowed()
    {
        _gateway
            .Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(new GenericMessage("the claim lock was not taken")));

        var outcome = await Sut().Claim(Request(Write("Teams")), TestContext.Current.CancellationToken);

        outcome.IsFailure.ShouldBeTrue();
        outcome.CurrentMessage.ShouldNotBeNull().ShouldContain("the claim lock was not taken");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AnAnswerWithNoStateRowIsRefusedWithItsCodeAndTheRunIsNotAssumedClaimed()
    {
        Answers();

        var outcome = await Sut().Claim(Request(Write("Teams")), TestContext.Current.CancellationToken);

        outcome.IsFailure.ShouldBeTrue();
        outcome.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60866");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    [InlineData("None")]
    [InlineData("Held")]
    public async Task AStateThatIsNeitherClaimedNorWaitingIsRefusedWithItsCode(string state)
    {
        Answers(Row(("ExecutionId", Execution), ("State", state), ("Ahead", 0)));

        var outcome = await Sut().Claim(Request(Write("Teams")), TestContext.Current.CancellationToken);

        outcome.IsFailure.ShouldBeTrue();
        outcome.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60867");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AHostThatNamesNoOperationalConnectionIsRefusedWithItsCode()
    {
        var outcome = await Sut(connection: null).Claim(Request(Write("Teams")), TestContext.Current.CancellationToken);

        outcome.IsFailure.ShouldBeTrue();
        outcome.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60865");
        _gateway.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task ARenewalSendsTheRunIdsAsOneJsonTextAndAnswersWhichRunsStillHoldTheirClaim()
    {
        var kept = Guid.Parse("55555555-0000-0000-0000-000000000005");
        var lost = Guid.Parse("66666666-0000-0000-0000-000000000006");
        Answers(Row(("ExecutionId", kept), ("Renewed", 1)), Row(("ExecutionId", lost), ("Renewed", 0)));

        var renewed = await Sut().Renew([kept, lost], "FdwReference.Etl.Server", "a", TestContext.Current.CancellationToken);

        renewed.IsSuccess.ShouldBeTrue(renewed.CurrentMessage);
        _target.ShouldBe(new DataStoreTarget(Connection, "etl", "RenewPipelineRunClaims"));
        var call = _sent.ShouldBeAssignableTo<IExecuteProcedureCommand>().ShouldNotBeNull();
        JsonSerializer.Deserialize<Guid[]>((string)call.Parameters["ExecutionIds"]!).ShouldBe([kept, lost]);
        call.Parameters["LeaseSeconds"].ShouldBe(60);
        renewed.Value.ShouldNotBeNull().Renewals.ShouldBe([new PipelineRunRenewal(kept, true), new PipelineRunRenewal(lost, false)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AReleaseAnswersTheRunsItPromotedSoTheirHostsCanStartThem()
    {
        var promoted = Guid.Parse("55555555-0000-0000-0000-000000000005");
        Answers(Row(("ExecutionId", promoted), ("ExecutingHost", "FdwReference.Etl.Server"), ("ExecutingSlot", null)));

        var released = await Sut().Release(Execution, TestContext.Current.CancellationToken);

        released.IsSuccess.ShouldBeTrue(released.CurrentMessage);
        _target.ShouldBe(new DataStoreTarget(Connection, "etl", "ReleasePipelineRunClaims"));
        var call = _sent.ShouldBeAssignableTo<IExecuteProcedureCommand>().ShouldNotBeNull();
        call.Parameters["ExecutionId"].ShouldBe(Execution);
        released.Value.ShouldNotBeNull().Promoted.ShouldHaveSingleItem().ExecutionId.ShouldBe(promoted);
    }
}
