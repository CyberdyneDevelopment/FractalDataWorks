using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for <see cref="OrganizationScope"/> (FDW-1593, FDW-2840, FDW-2855): a caller may administer an organization that exists when they
/// hold the right at the organization's scope, whichever organization they act in; a Finance administrator cannot touch Legal; the tenant
/// is judged by the scope authority and not a second time here.
/// </summary>
public sealed class OrganizationScopeTests
{
    private const string Right = OrganizationRights.Write;

    private static readonly Guid Tenant = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = new("11111111-0000-0000-0000-000000000002");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = new("44444444-0000-0000-0000-000000000002");

    private static IAuthenticationContext Caller(Guid activeTenant, bool system = false)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.IsAuthenticated).Returns(true);
        caller.SetupGet(c => c.IsSystemContext).Returns(system);
        caller.SetupGet(c => c.ActiveTenantId).Returns(activeTenant);
        return caller.Object;
    }

    private static Mock<IOrganizationProvider> Organizations(Guid organizationId, Guid tenantId)
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations.Setup(o => o.Get(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(new OrganizationConfiguration { Id = organizationId, TenantId = tenantId }));
        return organizations;
    }

    // The right is held at the scope of exactly these organizations of the tenant, and nowhere else.
    private static Mock<IScopeAuthority> HoldsAt(params Guid[] organizations)
    {
        var authority = new Mock<IScopeAuthority>();
        authority.Setup(a => a.May(It.IsAny<string>(), It.IsAny<ThingScope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new GenericMessage("the right is not held at that scope")));
        foreach (var organization in organizations)
        {
            authority.Setup(a => a.May(Right, It.Is<ThingScope>(s => s.TenantId == Tenant && s.OrganizationId == organization), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
        }

        return authority;
    }

    private static OrganizationScope Scope(IAuthenticationContext? caller, Mock<IOrganizationProvider> organizations, Mock<IScopeAuthority> authority)
        => new(new AuthenticationContextAccessor { Current = caller }, organizations.Object, authority.Object, NullLogger<OrganizationScope>.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerHoldingTheRightAtTheOrganizationsScopeMayAdministerIt()
    {
        var result = await Scope(Caller(Tenant), Organizations(Finance, Tenant), HoldsAt(Finance)).TenantOf(Right, Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Tenant);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheOrganizationTheCallerActsInDecidesNothing()
    {
        // The caller's token acts in Finance or in none; they hold the right at Legal's scope, so Legal is theirs to administer.
        var authority = HoldsAt(Legal);

        var result = await Scope(Caller(Tenant), Organizations(Legal, Tenant), authority).TenantOf(Right, Legal, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Tenant);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AFinanceAdministratorIsStillRefusedLegal()
    {
        var result = await Scope(Caller(Tenant), Organizations(Legal, Tenant), HoldsAt(Finance)).TenantOf(Right, Legal, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheRightIsAskedAtTheOrganizationsScopeInItsOwnTenant()
    {
        var authority = HoldsAt(Finance);

        await Scope(Caller(Tenant), Organizations(Finance, Tenant), authority).TenantOf(Right, Finance, TestContext.Current.CancellationToken);

        authority.Verify(a => a.May(Right, It.Is<ThingScope>(s => s.TenantId == Tenant && s.OrganizationId == Finance), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationOfAnotherTenantIsLeftToTheScopeAuthorityWhichRefusesIt()
    {
        var authority = HoldsAt(Finance);

        var result = await Scope(Caller(Tenant), Organizations(Finance, OtherTenant), authority).TenantOf(Right, Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        authority.Verify(a => a.May(Right, It.Is<ThingScope>(s => s.TenantId == OtherTenant && s.OrganizationId == Finance), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationOfAnotherTenantIsServedWhenTheScopeAuthorityAdmitsTheCaller()
    {
        // What the scope authority does for a platform administrator acting in the global tenant (FDW-2852): the organization scope does
        // not judge the tenant a second time and refuse before it is asked.
        var authority = new Mock<IScopeAuthority>();
        authority.Setup(a => a.May(Right, It.Is<ThingScope>(s => s.TenantId == OtherTenant && s.OrganizationId == Finance), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());

        var result = await Scope(Caller(Tenant), Organizations(Finance, OtherTenant), authority).TenantOf(Right, Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(OtherTenant);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task NobodySignedInIsRefusedAndNeitherTheStoreNorTheRightIsAsked()
    {
        var organizations = new Mock<IOrganizationProvider>(MockBehavior.Strict);
        var authority = new Mock<IScopeAuthority>(MockBehavior.Strict);

        var result = await Scope(null, organizations, authority).TenantOf(Right, Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationThatDoesNotExistIsNotFoundAndTheRightIsNotAsked()
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations.Setup(o => o.Get(Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("not found")));
        var authority = new Mock<IScopeAuthority>(MockBehavior.Strict);

        var result = await Scope(Caller(Tenant), organizations, authority).TenantOf(Right, Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task TheSystemContextMayAdministerAnyOrganization()
    {
        var result = await Scope(Caller(OtherTenant, system: true), Organizations(Legal, Tenant), HoldsAt(Legal)).TenantOf(Right, Legal, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Tenant);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task TheOverloadWithoutARightNamesTheTenantAndAsksNoRight()
    {
        var authority = new Mock<IScopeAuthority>(MockBehavior.Strict);

        var result = await Scope(Caller(Tenant), Organizations(Finance, Tenant), authority).TenantOf(Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Tenant);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task TheOverloadWithoutARightNamesAnotherTenantsOrganizationAndAsksNothing()
    {
        // The caller of this overload asks the scope authority itself, which judges the tenant and the right.
        var authority = new Mock<IScopeAuthority>(MockBehavior.Strict);

        var result = await Scope(Caller(Tenant), Organizations(Finance, OtherTenant), authority).TenantOf(Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(OtherTenant);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationOfAnotherTenantIsAnsweredExactlyAsAMissingOne()
    {
        // FDW-2887: the caller does not act in the organization's tenant and is refused, so nothing may say the organization exists.
        var missing = new Mock<IOrganizationProvider>();
        missing.Setup(o => o.Get(Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("not found")));

        var ofAnotherTenant = await Scope(Caller(Tenant), Organizations(Finance, OtherTenant), HoldsAt()).TenantOf(Right, Finance, TestContext.Current.CancellationToken);
        var absent = await Scope(Caller(Tenant), missing, HoldsAt()).TenantOf(Right, Finance, TestContext.Current.CancellationToken);

        ofAnotherTenant.IsFailure.ShouldBeTrue();
        ofAnotherTenant.Messages.Select(m => m.Code).ShouldBe(absent.Messages.Select(m => m.Code));
        ofAnotherTenant.CurrentMessage.ShouldBe(absent.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerWhoActsInTheTenantAndLacksTheRightIsStillRefusedForTheRight()
    {
        var result = await Scope(Caller(Tenant), Organizations(Legal, Tenant), HoldsAt(Finance)).TenantOf(Right, Legal, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldBe("the right is not held at that scope");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ADeletedOrganizationIsNotFoundAndTheRightIsNotAsked()
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations.Setup(o => o.Get(Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(new OrganizationConfiguration { Id = Finance, TenantId = Tenant, IsDeleted = true }));
        var authority = new Mock<IScopeAuthority>(MockBehavior.Strict);

        var result = await Scope(Caller(Tenant), organizations, authority).TenantOf(Right, Finance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }
}
