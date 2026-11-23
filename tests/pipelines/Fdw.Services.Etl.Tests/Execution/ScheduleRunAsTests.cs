using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Who a schedule's fire acts as (FDW-6144): the person its stored grant names, and only that person, once the fire is for the schedule it names, the grant has not ended, it allows what the run's roles carry and
/// the person may still act. A schedule with no grant acts as no person, as before.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class ScheduleRunAsTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Person = Guid.Parse("66666666-0000-0000-0000-000000000006");
    private static readonly Guid Pipeline = Guid.Parse("44444444-0000-0000-0000-000000000004");
    private static readonly Guid Schedule = Guid.Parse("55555555-0000-0000-0000-000000000005");
    private static readonly Guid Grant = Guid.Parse("99999999-0000-0000-0000-000000000009");
    private static readonly Guid Run = Guid.Parse("33333333-0000-0000-0000-000000000003");
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly List<ScheduleGrantLinkRecord> _schedules = [];
    private readonly Mock<IRunGrants> _grants = new();
    private readonly Mock<IRoleGrants> _roles = new();
    private readonly Mock<IUserPermissions> _permissions = new();
    private readonly Mock<IIssuanceEligibility> _eligibility = new();
    private readonly Mock<ITenantProvider> _tenants = new();

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    public ScheduleRunAsTests()
    {
        _roles.Setup(r => r.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(["datasets:read", "pipelines:execute"]));
        _eligibility
            .Setup(e => e.MayBeIssued(It.IsAny<Principal>(), _tenants.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Decision>.Success(new Decision { Permitted = true, Reason = "the account is active" }));
        _permissions.Setup(p => p.Get(Person.ToString(), Tenant, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<EffectiveAuthorization>.Success(new EffectiveAuthorization(["datasets:read", "pipelines:execute"], ["Analyst"], null)));
        GrantIs(new RunGrant { Id = Grant, PersonId = Person, ExpiresAt = Now.AddDays(1), Permissions = ["datasets:read", "pipelines:execute"] });
        _schedules.Add(new ScheduleGrantLinkRecord { Id = Schedule, PipelineId = Pipeline, TenantId = Tenant, RunGrantId = Grant });
    }

    private void GrantIs(RunGrant grant) =>
        _grants.Setup(g => g.Read(Grant, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<RunGrant>.Success(grant));

    private ScheduleRunAs Sut()
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<ScheduleGrantLinkRecord>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenericResult<IEnumerable<ScheduleGrantLinkRecord>>.Success(_schedules.ToList()));
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(p => p.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        var services = new ServiceCollection();
        services.AddSingleton(_roles.Object);
        services.AddSingleton(_permissions.Object);
        services.AddSingleton(_eligibility.Object);
        services.AddSingleton(_tenants.Object);
        return new ScheduleRunAs(gateways.Object, _grants.Object, services.BuildServiceProvider(), new FixedClock(Now), NullLogger<ScheduleRunAs>.Instance);
    }

    private static ScheduledFire Fire(Guid? pipeline = null, Guid? tenant = null) =>
        new(Schedule, pipeline ?? Pipeline, "NightlyLoad", tenant ?? Tenant, Run, ["DataWriter"]);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AScheduleWithAGrantRunsAsThePersonTheGrantNames()
    {
        var result = await Sut().Resolve(Fire(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().PersonId.ShouldBe(Person);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AScheduleWithNoGrantRunsAsNoPerson()
    {
        _schedules.Single().RunGrantId = null;

        var result = await Sut().Resolve(Fire(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().PersonId.ShouldBeNull();
        _grants.Verify(g => g.Read(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AFireForAScheduleThatIsNotThereIsRefused()
    {
        _schedules.Clear();

        var result = await Sut().Resolve(Fire(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("SCHEDULERUNAS-30842");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AFireThatIsNotForThePipelineOrTheTenantItsScheduleHasIsRefusedSoNoOneBorrowsAnotherSchedulesGrant()
    {
        var otherPipeline = await Sut().Resolve(Fire(pipeline: Guid.NewGuid()), TestContext.Current.CancellationToken);
        var otherTenant = await Sut().Resolve(Fire(tenant: Guid.NewGuid()), TestContext.Current.CancellationToken);

        otherPipeline.IsFailure.ShouldBeTrue();
        otherPipeline.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("SCHEDULERUNAS-20843");
        otherTenant.IsFailure.ShouldBeTrue();
        otherTenant.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("SCHEDULERUNAS-20843");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AGrantThatHasEndedRefusesTheFireWithItsOwnCode()
    {
        GrantIs(new RunGrant { Id = Grant, PersonId = Person, ExpiresAt = Now, Permissions = ["datasets:read", "pipelines:execute"] });

        var result = await Sut().Resolve(Fire(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("SCHEDULERUNAS-100847");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARevokedGrantRefusesTheFireWithTheSameCode()
    {
        GrantIs(new RunGrant { Id = Grant, PersonId = Person, ExpiresAt = Now.AddDays(1), Permissions = ["datasets:read", "pipelines:execute"], IsRevoked = true });

        var result = await Sut().Resolve(Fire(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("SCHEDULERUNAS-100847");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("revoked");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AGrantThatAllowsLessThanTheRunRolesCarryRefusesTheFire()
    {
        GrantIs(new RunGrant { Id = Grant, PersonId = Person, ExpiresAt = Now.AddDays(1), Permissions = ["datasets:read"] });

        var result = await Sut().Resolve(Fire(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("SCHEDULERUNAS-100848");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("pipelines:execute");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWhoMayNoLongerActRefusesTheFireWithTheCodeARunThatStartsUses()
    {
        _eligibility
            .Setup(e => e.MayBeIssued(It.IsAny<Principal>(), _tenants.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Decision>.Success(new Decision { Permitted = false, Reason = "the account is disabled" }));

        var result = await Sut().Resolve(Fire(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("RUNPERSON-100833");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWhoNoLongerHoldsWhatTheRunRolesCarryRefusesTheFire()
    {
        _permissions.Setup(p => p.Get(Person.ToString(), Tenant, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<EffectiveAuthorization>.Success(new EffectiveAuthorization(["datasets:read"], ["Analyst"], null)));

        var result = await Sut().Resolve(Fire(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("RUNPERSON-100832");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AGrantThatCannotBeReadRefusesTheFireAndNeverRunsAsNobody()
    {
        _grants.Setup(g => g.Read(Grant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<RunGrant>.Failure(new GenericMessage("the store is down")));

        var result = await Sut().Resolve(Fire(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("SCHEDULERUNAS-70846");
    }
}
