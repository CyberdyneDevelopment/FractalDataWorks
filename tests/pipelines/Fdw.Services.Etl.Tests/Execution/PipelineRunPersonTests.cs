using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// A run started by a person acts as that person (FDW-6137): its work reads with the person's own context, narrowed to the run's roles, and a person
/// who no longer holds the membership or the roles is refused. A run with no person acts as its tenant run, as before.
/// </summary>
[Collection(EtlOperationalConnectionCollection.Name)]
public sealed class PipelineRunPersonTests
{
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Person = Guid.Parse("66666666-0000-0000-0000-000000000006");

    private readonly Mock<IRoleGrants> _roles = new();
    private readonly Mock<IUserPermissions> _permissions = new();
    private readonly Mock<IIssuanceEligibility> _eligibility = new();
    private readonly Mock<ITenantProvider> _tenants = new();
    private readonly Mock<ITenant> _tenant = new();

    public PipelineRunPersonTests()
    {
        _roles.Setup(r => r.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(["datasets:read", "pipelines:execute"]));
        PersonIsEligible(true, "the account is active");
        _tenants.Setup(t => t.GetTenant(Tenant, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<ITenant>.Success(_tenant.Object));
    }

    private void PersonIsEligible(bool permitted, string reason) =>
        _eligibility
            .Setup(e => e.MayBeIssued(It.Is<Principal>(p => p.Id == Person && p.TenantId == Tenant), _tenants.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Decision>.Success(new Decision { Permitted = permitted, Reason = reason }));

    private static PipelineExecutionRequest Request(Guid? person) => new()
    {
        ExecutionId = Guid.NewGuid(),
        PipelineId = Guid.NewGuid(),
        PipelineName = "NightlyLoad",
        TriggerSource = "Api",
        RunRoles = ["DataWriter"],
        TenantId = Tenant,
        PersonId = person
    };

    private ServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_roles.Object);
        services.AddSingleton(_permissions.Object);
        services.AddSingleton(_eligibility.Object);
        services.AddSingleton(_tenants.Object);
        return services.BuildServiceProvider();
    }

    private void PersonHolds(params string[] permissions) =>
        _permissions.Setup(p => p.Get(Person.ToString(), Tenant, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<EffectiveAuthorization>.Success(new EffectiveAuthorization(permissions, ["Analyst"], null)));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonStartedRunActsAsThePersonNarrowedToTheRunsRoles()
    {
        PersonHolds("datasets:read", "pipelines:execute", "connections:write");
        using var services = Services();

        var result = await PipelineRequestProcessor.ResolveRunIdentity(
            services, Request(Person), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var context = result.Value.ShouldNotBeNull();
        context.ShouldNotBeOfType<TenantRunAuthenticationContext>();
        context.UserId.ShouldBe(Person.ToString());
        context.IsAuthenticated.ShouldBeTrue();
        context.IsSystemContext.ShouldBeFalse();
        context.ActiveTenantId.ShouldBe(Tenant);
        context.Permissions.OrderBy(p => p, StringComparer.Ordinal).ShouldBe(["datasets:read", "pipelines:execute"]);
        context.Roles.ShouldBe(["DataWriter"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AToolTheRunCallsSeesTheRunsPersonWhileItRunsAndTheBookkeepingContextIsBackAfter()
    {
        PersonHolds("datasets:read", "pipelines:execute");
        var accessor = new AuthenticationContextAccessor();
        var bookkeeping = new SystemAuthenticationContext(Tenant);
        accessor.Current = bookkeeping;
        var services = new ServiceCollection();
        services.AddSingleton(_roles.Object);
        services.AddSingleton(_permissions.Object);
        services.AddSingleton(_eligibility.Object);
        services.AddSingleton(_tenants.Object);
        services.AddSingleton<IAuthenticationContextAccessor>(accessor);
        using var provider = services.BuildServiceProvider();
        var runAs = await PipelineRequestProcessor.ResolveRunIdentity(provider, Request(Person), NullLogger.Instance, TestContext.Current.CancellationToken);
        runAs.IsSuccess.ShouldBeTrue(runAs.CurrentMessage);

        // The stand-in for a tool endpoint the run calls: whatever reads the caller from the accessor while the run executes.
        IAuthenticationContext? seenByTheTool = null;
        var pipeline = new Mock<IEtlPipeline>();
        pipeline.Setup(p => p.Execute(It.IsAny<PipelineExecutionOptions>(), It.IsAny<CancellationToken>()))
            .Callback<PipelineExecutionOptions, CancellationToken>((_, _) => seenByTheTool = accessor.Current)
            .ReturnsAsync(GenericResult<IEtlPipelineExecutionResult>.Failure(new GenericMessage("the probe ends the run")));

        await PipelineRequestProcessor.ExecuteAs(provider, runAs.Value!, pipeline.Object, PipelineExecutionOptions.Production, TestContext.Current.CancellationToken);

        seenByTheTool.ShouldNotBeNull().UserId.ShouldBe(Person.ToString());
        seenByTheTool.ActiveTenantId.ShouldBe(Tenant);
        accessor.Current.ShouldBeSameAs(bookkeeping);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWhoseMembershipCannotBeReadIsRefusedAsRunPersonNotPermitted()
    {
        _permissions.Setup(p => p.Get(Person.ToString(), Tenant, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<EffectiveAuthorization>.Failure(new GenericMessage("not a member of the tenant")));
        using var services = Services();

        var result = await PipelineRequestProcessor.ResolveRunIdentity(
            services, Request(Person), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("RUNPERSON-100831");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADisabledPersonIsRefusedAsRunPersonIneligibleThoughTheirGrantsStillRead()
    {
        PersonHolds("datasets:read", "pipelines:execute");
        PersonIsEligible(false, "the account is disabled");
        using var services = Services();

        var result = await PipelineRequestProcessor.ResolveRunIdentity(
            services, Request(Person), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("RUNPERSON-100833");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("the account is disabled");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWhoseEligibilityCannotBeReadIsRefusedWithItsOwnCode()
    {
        PersonHolds("datasets:read", "pipelines:execute");
        _eligibility
            .Setup(e => e.MayBeIssued(It.IsAny<Principal>(), _tenants.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Decision>.Failure(new GenericMessage("the user store is unreachable")));
        using var services = Services();

        var result = await PipelineRequestProcessor.ResolveRunIdentity(
            services, Request(Person), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("RUNPERSON-70836");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("the user store is unreachable");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonRunWhoseTenantCannotBeReadIsRefusedWithItsOwnCode()
    {
        PersonHolds("datasets:read", "pipelines:execute");
        _tenants.Setup(t => t.GetTenant(Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITenant>.Failure(new GenericMessage("the tenant store is unreachable")));
        using var services = Services();

        var result = await PipelineRequestProcessor.ResolveRunIdentity(
            services, Request(Person), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("RUNPERSON-70837");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonRunInTheGlobalTenantCarriesTheGlobalTenantClaimAndOneElsewhereDoesNot()
    {
        PersonHolds("datasets:read", "pipelines:execute");
        using var services = Services();

        foreach (var global in new[] { true, false })
        {
            _tenant.SetupGet(t => t.IsGlobal).Returns(global);

            var result = await PipelineRequestProcessor.ResolveRunIdentity(
                services, Request(Person), NullLogger.Instance, TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
            result.Value.ShouldNotBeNull().Claims.ContainsKey(ClaimDefinitions.globalTenant.Name).ShouldBe(global);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AHostWithNoEligibilityCheckRefusesAPersonsRunRatherThanRunningIt()
    {
        PersonHolds("datasets:read", "pipelines:execute");
        var bare = new ServiceCollection();
        bare.AddSingleton(_roles.Object);
        bare.AddSingleton(_permissions.Object);
        using var services = bare.BuildServiceProvider();

        var result = await PipelineRequestProcessor.ResolveRunIdentity(
            services, Request(Person), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("RUNPERSON-60836");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWhoNoLongerHoldsWhatTheRunsRolesCarryIsRefused()
    {
        PersonHolds("datasets:read");
        using var services = Services();

        var result = await PipelineRequestProcessor.ResolveRunIdentity(
            services, Request(Person), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("RUNPERSON-100832");
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("pipelines:execute");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARunWithNoPersonActsAsItsTenantRunAsBefore()
    {
        using var services = Services();
        var request = Request(null);

        var result = await PipelineRequestProcessor.ResolveRunIdentity(
            services, request, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var run = result.Value.ShouldBeOfType<TenantRunAuthenticationContext>();
        run.PrincipalId.ShouldBe(request.PipelineId);
        _permissions.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AHeldRunWhoseRequesterLostAPermissionWhileHeldIsRefusedWhenReleased()
    {
        var held = Request(Person);
        var kept = PipelineRunRequestJson.Write(held, NullLogger.Instance);
        var released = PipelineRunRequestJson.Read(held.ExecutionId, kept.Value.ShouldNotBeNull(), null, NullLogger.Instance);
        PersonHolds("datasets:read");
        using var services = Services();

        var result = await PipelineRequestProcessor.ResolveRunIdentity(
            services, released.Value.ShouldNotBeNull(), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.FirstFailureMessage().ShouldNotBeNull().Code.ShouldBe("RUNPERSON-100832");
        _permissions.Verify(p => p.Get(Person.ToString(), Tenant, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ACancelledTokenStopsResolvingTheRunsIdentityWithTheCancelledCodeAndReadsNothing(bool hasPerson)
    {
        PersonHolds("datasets:read", "pipelines:execute");
        using var services = Services();
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var result = await PipelineRequestProcessor.ResolveRunIdentity(
            services, Request(hasPerson ? Person : null), NullLogger.Instance, cancelled.Token);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("RUNPERSON-10837");
        _eligibility.Verify(e => e.MayBeIssued(It.IsAny<Principal>(), It.IsAny<ITenantProvider>(), It.IsAny<CancellationToken>()), Times.Never);
        _permissions.Verify(p => p.Get(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        _roles.Verify(r => r.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AStoredRequestKeepsItsPersonAndOneWithNoneReadsBackWithNone()
    {
        foreach (var person in new Guid?[] { Person, null })
        {
            var request = Request(person);
            var text = PipelineRunRequestJson.Write(request, NullLogger.Instance);
            text.IsSuccess.ShouldBeTrue(text.CurrentMessage);

            var read = PipelineRunRequestJson.Read(request.ExecutionId, text.Value.ShouldNotBeNull(), null, NullLogger.Instance);

            read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
            read.Value.ShouldNotBeNull().PersonId.ShouldBe(person);
        }
    }
}
