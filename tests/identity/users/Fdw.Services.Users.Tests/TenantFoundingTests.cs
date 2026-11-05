using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// Tests for <see cref="TenantFounding"/> (FDW-1578, FDW-3668): a tenant is made with its default organization, that organization's own default
/// workspace and its first administrator, nothing is made when the person is missing or the caller may not give the role, and what was made is
/// removed again, newest first, when a later step fails.
/// </summary>
public sealed class TenantFoundingTests
{
    private static readonly Guid Administrator = new("22222222-0000-0000-0000-000000000001");
    private static readonly Guid NewTenantId = new("11111111-0000-0000-0000-000000000009");
    private static readonly Guid DefaultOrganizationId = new("44444444-0000-0000-0000-000000000009");
    private static readonly Guid DefaultWorkspaceId = new("66666666-0000-0000-0000-000000000009");
    private static readonly NewTenant Beta = new("Beta", "beta", null, null, true, "Main", "main");

    private static IGenericResult Failed(string reason) => GenericResult.Failure(new GenericMessage(reason));

    private sealed class Rig
    {
        public Mock<ITenantWriter> Tenants { get; } = new();
        public Mock<IOrganizationWriter> Organizations { get; } = new();
        public Mock<IOrganizationProvider> OrganizationReader { get; } = new();
        public Mock<IDefaultWorkspaceFounding> DefaultWorkspaces { get; } = new();
        public Mock<IUserConfigurationProvider> Users { get; } = new();
        public Mock<IUserTenantConfigurationProvider> Memberships { get; } = new();
        public Mock<IGrantRule> GrantRule { get; } = new();
        public Mock<IUserRoleAssignment> Roles { get; } = new();

        public Rig()
        {
            Users.Setup(u => u.Get(Administrator, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IUserImplementationConfiguration>.Success(Mock.Of<IUserImplementationConfiguration>()));
            GrantRule.Setup(g => g.CanGiveInNewTenant(It.IsAny<System.Collections.Generic.IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            Tenants.Setup(t => t.Create(It.IsAny<NewTenant>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(NewTenantId));
            Tenants.Setup(t => t.Delete(NewTenantId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Organizations.Setup(o => o.Create(It.IsAny<NewOrganization>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(DefaultOrganizationId));
            OrganizationReader.Setup(o => o.Get(DefaultOrganizationId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(
                new OrganizationConfiguration
                {
                    Id = DefaultOrganizationId, TenantId = NewTenantId, Name = "Default Organization", Slug = "default", IsDefault = true,
                    DefaultWorkspaceId = DefaultWorkspaceId,
                }));
            DefaultWorkspaces.Setup(w => w.Found(It.IsAny<NewDefaultWorkspace>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(DefaultWorkspaceId));
            DefaultWorkspaces.Setup(w => w.Remove(It.IsAny<NewDefaultWorkspace>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Memberships.Setup(m => m.Save(It.IsAny<UserTenantImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            Memberships.Setup(m => m.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Roles.Setup(r => r.Assign(Administrator, "Admin", NewTenantId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        }

        public TenantFounding Founding() =>
            new(Tenants.Object, Organizations.Object, OrganizationReader.Object, DefaultWorkspaces.Object, Users.Object, Memberships.Object, GrantRule.Object,
                Roles.Object, NullLogger<TenantFounding>.Instance);

        public void NothingMade() =>
            Tenants.Verify(t => t.Create(It.IsAny<NewTenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ATenantIsMadeWithItsFirstAdministratorAMemberHoldingTheRole()
    {
        var rig = new Rig();

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(NewTenantId);
        rig.Memberships.Verify(m => m.Save(
            It.Is<UserTenantImplementationConfiguration>(c => c.UserId == Administrator && c.TenantId == NewTenantId && !c.IsDefault),
            "UserTenants", "UserTenants", $"{Administrator}:{NewTenantId}", It.IsAny<CancellationToken>()), Times.Once);
        rig.Roles.Verify(r => r.Assign(Administrator, "Admin", NewTenantId, It.IsAny<CancellationToken>()), Times.Once);
        rig.Tenants.Verify(t => t.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ATenantIsMadeWithItsDefaultOrganizationAndTheOrganizationsOwnDefaultWorkspace()
    {
        var rig = new Rig();

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        rig.Organizations.Verify(o => o.Create(
            It.Is<NewOrganization>(n => n.TenantId == NewTenantId && n.Name == "Main" && n.Slug == "main" && n.IsDefault),
            It.IsAny<CancellationToken>()), Times.Once);
        rig.DefaultWorkspaces.Verify(w => w.Found(
            It.Is<NewDefaultWorkspace>(n => n.WorkspaceId == DefaultWorkspaceId && n.TenantId == NewTenantId && n.TenantSlug == "beta"
                && n.OrganizationId == DefaultOrganizationId && n.OrganizationIsDefault && n.OwnerUserId == Administrator),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", "main")]
    [InlineData("  ", "main")]
    [InlineData("Main", "")]
    [InlineData("Main", "  ")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AFounderWhoNamesNoOrganizationOrNoSlugMakesNothing(string organizationName, string organizationSlug)
    {
        var rig = new Rig();

        var result = await rig.Founding().Found(
            new NewTenant("Beta", "beta", null, null, true, organizationName, organizationSlug), Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.NothingMade();
        rig.Organizations.Verify(o => o.Create(It.IsAny<NewOrganization>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADefaultOrganizationThatCannotBeMadeRemovesTheTenantAndMakesNoWorkspaceOrMembership()
    {
        var rig = new Rig();
        rig.Organizations.Setup(o => o.Create(It.IsAny<NewOrganization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Failure(new GenericMessage("organization refused")));

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("organization refused");
        rig.Tenants.Verify(t => t.Delete(NewTenantId, It.IsAny<CancellationToken>()), Times.Once);
        rig.DefaultWorkspaces.Verify(w => w.Found(It.IsAny<NewDefaultWorkspace>(), It.IsAny<CancellationToken>()), Times.Never);
        rig.Memberships.Verify(m => m.Save(It.IsAny<UserTenantImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ADefaultWorkspaceThatCannotBeMadeRemovesTheTenantAndMakesNoMembership()
    {
        var rig = new Rig();
        rig.DefaultWorkspaces.Setup(w => w.Found(It.IsAny<NewDefaultWorkspace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<Guid>.Failure(new GenericMessage("workspace refused")));

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("workspace refused");
        rig.Tenants.Verify(t => t.Delete(NewTenantId, It.IsAny<CancellationToken>()), Times.Once);
        rig.DefaultWorkspaces.Verify(w => w.Remove(It.IsAny<NewDefaultWorkspace>(), It.IsAny<CancellationToken>()), Times.Never);
        rig.Memberships.Verify(m => m.Save(It.IsAny<UserTenantImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APersonWithNoAccountMakesNothing()
    {
        var rig = new Rig();
        rig.Users.Setup(u => u.Get(Administrator, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IUserImplementationConfiguration>.Failure(new GenericMessage("not found")));

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.NothingMade();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleTheCallerMayNotGiveMakesNothing()
    {
        var rig = new Rig();
        rig.GrantRule.Setup(g => g.CanGiveInNewTenant(It.IsAny<System.Collections.Generic.IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failed("the caller does not hold users:write"));

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.NothingMade();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ASlugAnotherTenantHasMakesNothingAndWritesNoMembership()
    {
        var rig = new Rig();
        rig.Tenants.Setup(t => t.Create(It.IsAny<NewTenant>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Failure(new GenericMessage("slug taken")));

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.Memberships.Verify(m => m.Save(It.IsAny<UserTenantImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AMembershipThatCannotBeSavedRemovesTheDefaultWorkspaceAndTheTenantAndReportsThatFailure()
    {
        var rig = new Rig();
        rig.Memberships.Setup(m => m.Save(It.IsAny<UserTenantImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failed("membership refused"));

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("membership refused");
        rig.Tenants.Verify(t => t.Delete(NewTenantId, It.IsAny<CancellationToken>()), Times.Once);
        rig.DefaultWorkspaces.Verify(w => w.Remove(It.Is<NewDefaultWorkspace>(n => n.WorkspaceId == DefaultWorkspaceId), It.IsAny<CancellationToken>()), Times.Once);
        rig.Memberships.Verify(m => m.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        rig.Roles.Verify(r => r.Assign(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleThatCannotBeGivenRemovesTheMembershipThenTheDefaultWorkspaceThenTheTenant()
    {
        var rig = new Rig();
        rig.Roles.Setup(r => r.Assign(Administrator, "Admin", NewTenantId, It.IsAny<CancellationToken>())).ReturnsAsync(Failed("role refused"));
        var order = new System.Collections.Generic.List<string>();
        rig.Memberships.Setup(m => m.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("membership")).ReturnsAsync(GenericResult.Success());
        rig.DefaultWorkspaces.Setup(w => w.Remove(It.IsAny<NewDefaultWorkspace>(), It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("workspace")).ReturnsAsync(GenericResult.Success());
        rig.Tenants.Setup(t => t.Delete(NewTenantId, It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("tenant")).ReturnsAsync(GenericResult.Success());

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("role refused");
        order.ShouldBe(["membership", "workspace", "tenant"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnUndoThatFailsStillReportsTheOriginalFailureAndTriesTheTenant()
    {
        var rig = new Rig();
        rig.Roles.Setup(r => r.Assign(Administrator, "Admin", NewTenantId, It.IsAny<CancellationToken>())).ReturnsAsync(Failed("role refused"));
        rig.Memberships.Setup(m => m.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Failed("membership stuck"));

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("role refused");
        rig.Tenants.Verify(t => t.Delete(NewTenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
