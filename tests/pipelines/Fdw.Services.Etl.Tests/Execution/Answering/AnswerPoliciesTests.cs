using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.AnswerRequests;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution.Answering;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution.Answering;

/// <summary>
/// FDW-6142: the answer policy says which calls need a request and which a stored grant covers. The four things a call can be judged (it goes on, it is refused, it needs the person, a grant covers it), a grant
/// that has ended, was revoked or does not allow the permission, a call or a policy that is not there and a row that states something that cannot be: each is answered, and none is assumed.
/// </summary>
public sealed class AnswerPoliciesTests
{
    private static readonly Guid PolicyId = Guid.Parse("11111111-0000-0000-0000-000000000042");
    private static readonly Guid GrantId = Guid.Parse("22222222-0000-0000-0000-000000000042");
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly List<AnswerPolicyRecord> _policies = [new() { Id = PolicyId, Name = "tools", ExpiresAfterSeconds = 900 }];
    private readonly List<AnswerPolicyCallRecord> _calls =
    [
        new() { PolicyId = PolicyId, CallName = "read_inbox", RequirementName = "Automatic" },
        new() { PolicyId = PolicyId, CallName = "delete_account", RequirementName = "Refused" },
        new() { PolicyId = PolicyId, CallName = "send_email", RequirementName = "GrantOrPerson", Permission = "email:send" },
        new() { PolicyId = PolicyId, CallName = "transfer", RequirementName = "NeedsPerson" },
    ];

    private readonly Mock<IRunGrants> _grants = new();
    private bool _readFails;

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private AnswerPolicies Sut()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway.Setup(g => g.Execute<IEnumerable<AnswerPolicyRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken __) => _readFails
                ? GenericResult<IEnumerable<AnswerPolicyRecord>>.Failure(new GenericMessage("the store is down"))
                : GenericResult<IEnumerable<AnswerPolicyRecord>>.Success(InMemoryOperationalStore.RowsMatching(_policies, command)));
        gateway.Setup(g => g.Execute<IEnumerable<AnswerPolicyCallRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDataCommand command, DataStoreTarget _, CancellationToken __) => GenericResult<IEnumerable<AnswerPolicyCallRecord>>.Success(InMemoryOperationalStore.RowsMatching(_calls, command)));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(provider => provider.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return new AnswerPolicies(gateways.Object, _grants.Object, new Clock(), NullLogger<AnswerPolicies>.Instance);
    }

    private void Grant(DateTimeOffset expiresAt, bool revoked, params string[] permissions) =>
        _grants.Setup(g => g.Read(GrantId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<RunGrant>.Success(
            new RunGrant { Id = GrantId, PersonId = Guid.NewGuid(), ExpiresAt = expiresAt, Permissions = permissions, IsRevoked = revoked }));

    [Theory]
    [InlineData("read_inbox", "Automatic")]
    [InlineData("delete_account", "WouldBeRefused")]
    [InlineData("transfer", "NeedsPerson")]
    [InlineData("send_email", "NeedsPerson")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ACallIsJudgedByTheRequirementItsRowStatesWhenNoGrantIsStated(string call, string outcome)
    {
        var judged = await Sut().Judge("tools", call, null, TestContext.Current.CancellationToken);

        judged.IsSuccess.ShouldBeTrue(judged.CurrentMessage);
        judged.Value.ShouldNotBeNull().Outcome.Name.ShouldBe(outcome);
        judged.Value.ExpiresAfterSeconds.ShouldBe(900);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AGrantThatHasNotEndedAndAllowsThePermissionCoversAGrantOrPersonCall()
    {
        Grant(Now.AddDays(1), revoked: false, "email:*");

        var judged = await Sut().Judge("tools", "send_email", GrantId, TestContext.Current.CancellationToken);

        judged.IsSuccess.ShouldBeTrue(judged.CurrentMessage);
        judged.Value.ShouldNotBeNull().Outcome.Name.ShouldBe("Granted");
        judged.Value.Outcome.CallGoesOn.ShouldBeTrue();
        judged.Value.Outcome.KeepsARequest.ShouldBeFalse();
    }

    [Theory]
    [InlineData(-1, false, "email:send")]
    [InlineData(0, false, "email:send")]
    [InlineData(1440, true, "email:send")]
    [InlineData(1440, false, "calendar:read")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AGrantThatEndedWasRevokedOrDoesNotAllowThePermissionLeavesTheCallToThePerson(int minutesFromNow, bool revoked, string permission)
    {
        Grant(Now.AddMinutes(minutesFromNow), revoked, permission);

        var judged = await Sut().Judge("tools", "send_email", GrantId, TestContext.Current.CancellationToken);

        judged.IsSuccess.ShouldBeTrue(judged.CurrentMessage);
        judged.Value.ShouldNotBeNull().Outcome.Name.ShouldBe("NeedsPerson");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AGrantNoOtherRequirementTakesIsNotReadAndAGrantStatedButNotReadableIsARefusal()
    {
        (await Sut().Judge("tools", "transfer", GrantId, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull().Outcome.Name.ShouldBe("NeedsPerson");
        _grants.Verify(g => g.Read(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        _grants.Setup(g => g.Read(GrantId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<RunGrant>.Failure(new GenericMessage("no such grant")));
        (await Sut().Judge("tools", "send_email", GrantId, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APolicyOrACallThatIsNotThereIsRefusedAndNoRequirementIsAssumed()
    {
        var noPolicy = await Sut().Judge("nobody", "read_inbox", null, TestContext.Current.CancellationToken);
        var noCall = await Sut().Judge("tools", "format_disk", null, TestContext.Current.CancellationToken);

        noPolicy.IsFailure.ShouldBeTrue();
        noPolicy.CurrentMessage.ShouldNotBeNull().ShouldContain("not a policy");
        noCall.IsFailure.ShouldBeTrue();
        noCall.CurrentMessage.ShouldNotBeNull().ShouldContain("does not list call");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARowThatStatesWhatCannotBeIsRefusedWhateverIsAskedOfIt()
    {
        _calls.Add(new AnswerPolicyCallRecord { PolicyId = PolicyId, CallName = "odd", RequirementName = "Sometimes" });
        _calls.Add(new AnswerPolicyCallRecord { PolicyId = PolicyId, CallName = "no_permission", RequirementName = "GrantOrPerson" });
        _calls.Add(new AnswerPolicyCallRecord { PolicyId = PolicyId, CallName = "needless", RequirementName = "Automatic", Permission = "a:b" });

        foreach (var call in new[] { "odd", "no_permission", "needless" })
        {
            (await Sut().Judge("tools", call, null, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue(call);
        }

        _policies[0].ExpiresAfterSeconds = 0;
        (await Sut().Judge("tools", "read_inbox", null, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStoreThatCannotBeReadIsAFailureAndNotANeedForThePerson()
    {
        _readFails = true;

        (await Sut().Judge("tools", "read_inbox", null, TestContext.Current.CancellationToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EachRequirementJudgesByItsOwnDataAndEveryOutcomeSaysWhatItMeans()
    {
        AnswerPolicyRequirements.ByName("Automatic").Judge(true).Name.ShouldBe("Automatic");
        AnswerPolicyRequirements.ByName("Refused").Judge(true).RefusesTheCall.ShouldBeTrue();
        AnswerPolicyRequirements.ByName("NeedsPerson").Judge(true).KeepsARequest.ShouldBeTrue();
        AnswerPolicyRequirements.ByName("GrantOrPerson").Judge(true).Name.ShouldBe("Granted");
        AnswerPolicyRequirements.ByName("GrantOrPerson").Judge(false).Name.ShouldBe("NeedsPerson");
        AnswerPolicyRequirements.ByName("GrantOrPerson").NeedsPermission.ShouldBeTrue();
        AnswerPolicyRequirements.All().Where(requirement => requirement.NeedsPermission).Select(requirement => requirement.Name).ShouldBe(["GrantOrPerson"]);
        AnswerPolicyOutcomes.All().Where(outcome => outcome.CallGoesOn).Select(outcome => outcome.Name).OrderBy(name => name, StringComparer.Ordinal).ShouldBe(["Automatic", "Granted"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheStatesAnswerRequestsPassThroughSayWhoMayAnswerAndWhatIsOwed()
    {
        AnswerRequestStates.All().Where(state => state.AcceptsAnAnswer).Select(state => state.Name).ShouldBe(["Open"]);
        AnswerRequestStates.All().Where(state => state.OwesTheStart).Select(state => state.Name).ShouldBe(["Answered"]);
        AnswerRequestStates.All().Where(state => !state.IsReadable).Select(state => state.Name).ShouldBe(["Keeping"]);
        AnswerRequestKinds.All().Where(kind => kind.AnswerMustBeAnOption).Select(kind => kind.Name).ShouldBe(["Choice"]);
    }
}
