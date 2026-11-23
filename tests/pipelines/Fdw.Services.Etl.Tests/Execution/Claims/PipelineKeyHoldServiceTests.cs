using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution.Claims;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Claims;

/// <summary>
/// FDW-6939 (slice 3 of FDW-6141): the hold on a key (one conversation) is two OpsDb procedures called through the data gateway, etl.PlaceKeyHold and etl.ReleaseKeyHold, and nothing else. The key is the
/// stated key the claim builds (the tenant's id, a colon, the key), so the hold and the claim meet on one text. A place on a key that is held is a coded refusal naming who holds it; a release of a key that
/// is not held is a coded refusal; a hold with no reason is refused before the procedure is called.
/// </summary>
public sealed class PipelineKeyHoldServiceTests
{
    private const string Connection = "OpsDb";
    private static readonly Guid Tenant = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Person = Guid.Parse("55555555-0000-0000-0000-000000000005");
    private static readonly Guid Other = Guid.Parse("66666666-0000-0000-0000-000000000006");
    private static readonly Guid Run = Guid.Parse("33333333-0000-0000-0000-000000000003");

    private readonly Mock<IDataGateway> _gateway = new(MockBehavior.Strict);
    private IDataCommand? _sent;
    private DataStoreTarget? _target;

    private PipelineKeyHoldService Sut(string? connection = Connection) =>
        new(_gateway.Object, connection, new PipelineRunClaimSettings(60, 20, 5, 30000), NullLogger<PipelineKeyHoldService>.Instance);

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

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task APlaceCallsThePlaceProcedureWithTheTenantPrefixedKeyThePersonAndTheReason()
    {
        var at = DateTimeOffset.Parse("2026-11-19T12:00:00+00:00");
        Answers(Row(("Placed", true), ("HeldBy", Person), ("HeldAt", at), ("Reason", "waiting for the person to answer")));

        var placed = await Sut().Place(Tenant, "chat-9", Person, "waiting for the person to answer", TestContext.Current.CancellationToken);

        placed.IsSuccess.ShouldBeTrue(placed.CurrentMessage);
        placed.Value.ShouldBe(new PipelineKeyHold(Person, at, "waiting for the person to answer"));
        _target.ShouldBe(new DataStoreTarget(Connection, "etl", "PlaceKeyHold"));
        var call = _sent.ShouldBeAssignableTo<IExecuteProcedureCommand>().ShouldNotBeNull();
        call.Parameters["TenantId"].ShouldBe(Tenant);
        call.Parameters["ClaimKey"].ShouldBe($"{Tenant:D}:chat-9");
        call.Parameters["HeldBy"].ShouldBe(Person);
        call.Parameters["Reason"].ShouldBe("waiting for the person to answer");
        call.Parameters["LockTimeoutMilliseconds"].ShouldBe(30000);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task APlaceOnAKeyThatIsHeldIsACodedRefusalNamingWhoHoldsIt()
    {
        Answers(Row(("Placed", false), ("HeldBy", Other), ("HeldAt", DateTimeOffset.UtcNow), ("Reason", "first reason")));

        var placed = await Sut().Place(Tenant, "chat-9", Person, "second reason", TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        placed.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-40874");
        placed.CurrentMessage.ShouldNotBeNull().ShouldContain(Other.ToString("D"));
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task APlaceWithNoReasonIsRefusedBeforeTheProcedureIsCalled(string reason)
    {
        var placed = await Sut().Place(Tenant, "chat-9", Person, reason, TestContext.Current.CancellationToken);

        placed.IsFailure.ShouldBeTrue();
        placed.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-20876");
        _gateway.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AReleaseCallsTheReleaseProcedureAndListsTheRunsItPromoted()
    {
        Answers(Row(("Released", true), ("HeldBy", Person)), Row(("ExecutionId", Run), ("ExecutingHost", "FdwReference.Etl.Server"), ("ExecutingSlot", "a")));

        var released = await Sut().Release(Tenant, "chat-9", TestContext.Current.CancellationToken);

        released.IsSuccess.ShouldBeTrue(released.CurrentMessage);
        released.Value.ShouldNotBeNull().HeldBy.ShouldBe(Person);
        released.Value.Promoted.ShouldBe([new PipelinePromotedRun(Run, "FdwReference.Etl.Server", "a")]);
        _target.ShouldBe(new DataStoreTarget(Connection, "etl", "ReleaseKeyHold"));
        var call = _sent.ShouldBeAssignableTo<IExecuteProcedureCommand>().ShouldNotBeNull();
        call.Parameters["ClaimKey"].ShouldBe($"{Tenant:D}:chat-9");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task AReleaseOfAKeyThatIsNotHeldIsACodedRefusal()
    {
        Answers(Row(("Released", false), ("HeldBy", null)));

        var released = await Sut().Release(Tenant, "chat-9", TestContext.Current.CancellationToken);

        released.IsFailure.ShouldBeTrue();
        released.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-30875");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public async Task AHostThatNamesNoOperationalConnectionCannotHoldOrReleaseAndNothingIsCalled()
    {
        var placed = await Sut(connection: null).Place(Tenant, "chat-9", Person, "a reason", TestContext.Current.CancellationToken);
        var released = await Sut(connection: null).Release(Tenant, "chat-9", TestContext.Current.CancellationToken);

        placed.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60865");
        released.Messages.Select(message => message.Code).ShouldContain("PIPELINERUNCLAIM-60865");
        _gateway.VerifyNoOtherCalls();
    }
}
