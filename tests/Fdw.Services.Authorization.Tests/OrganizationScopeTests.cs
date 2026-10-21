using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for <see cref="OrganizationScope"/> (FDW-1593): a caller may administer the organization they act in, in the tenant they act in,
/// and a Finance administrator cannot touch Legal.
/// </summary>
public sealed class OrganizationScopeTests
{
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

    // The organization the request resolved: the orgId claim, else the tenant's default.
    private static IOrgContext ActingIn(Guid? organizationId)
    {
        var context = new Mock<IOrgContext>();
        context.SetupGet(c => c.OrgId).Returns(organizationId);
        return context.Object;
    }

    private static Mock<IOrganizationProvider> Organizations(Guid organizationId, Guid tenantId)
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations.Setup(o => o.Get(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(new OrganizationConfiguration { Id = organizationId, TenantId = tenantId }));
        return organizations;
    }

    private static OrganizationScope Scope(IAuthenticationContext? caller, IOrgContext acting, Mock<IOrganizationProvider> organizations)
        => new(new AuthenticationContextAccessor { Current = caller }, acting, organizations.Object, NullLogger<OrganizationScope>.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerActingInTheOrganizationAndTenantMayAdministerIt()
    {
        var result = await Scope(Caller(Tenant), ActingIn(Finance), Organizations(Finance, Tenant)).TenantOf(Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Tenant);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AFinanceAdministratorCannotTouchLegalAndTheStoreIsNotAsked()
    {
        var organizations = new Mock<IOrganizationProvider>(MockBehavior.Strict);

        var result = await Scope(Caller(Tenant), ActingIn(Finance), organizations).TenantOf(Legal, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationOfAnotherTenantIsRefused()
    {
        var result = await Scope(Caller(Tenant), ActingIn(Finance), Organizations(Finance, OtherTenant)).TenantOf(Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ACallerActingInNoOrganizationIsRefused()
    {
        var result = await Scope(Caller(Tenant), ActingIn(null), Organizations(Finance, Tenant)).TenantOf(Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task NobodySignedInIsRefused()
    {
        var result = await Scope(null, ActingIn(Finance), Organizations(Finance, Tenant)).TenantOf(Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnOrganizationThatDoesNotExistIsNotFound()
    {
        var organizations = new Mock<IOrganizationProvider>();
        organizations.Setup(o => o.Get(Finance, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Failure(new GenericMessage("not found")));

        var result = await Scope(Caller(Tenant), ActingIn(Finance), organizations).TenantOf(Finance, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task TheSystemContextMayAdministerAnyOrganization()
    {
        var result = await Scope(Caller(OtherTenant, system: true), ActingIn(null), Organizations(Legal, Tenant)).TenantOf(Legal, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(Tenant);
    }
}
