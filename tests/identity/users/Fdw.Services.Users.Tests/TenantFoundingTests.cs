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
    private static readonly Guid MadeWorkspaceId = new("66666666-0000-0000-0000-000000000009");
    private static readonly NewTenant Beta = new("Beta", "beta", null, null, true, "main");

    private static IGenericResult Failed(string reason) => GenericResult.Failure(new GenericMessage(reason));

    private sealed class Rig
    {
        public Mock<ITenantWriter> Tenants { get; } = new();
        public Mock<IOrganizationWriter> Organizations { get; } = new();
        public Mock<IWorkspaceFoundingPolicy> Policy { get; } = new();
        public NewOrganization? OrganizationMade { get; private set; }
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
            Policy.Setup(p => p.FoundingMakesDefaultWorkspace()).Returns(GenericResult<bool>.Success(true));
            Organizations.Setup(o => o.Create(It.IsAny<NewOrganization>(), It.IsAny<CancellationToken>()))
                .Callback<NewOrganization, CancellationToken>((made, _) => OrganizationMade = made)
                .ReturnsAsync(GenericResult<Guid>.Success(DefaultOrganizationId));
            DefaultWorkspaces.Setup(w => w.Found(It.IsAny<NewDefaultWorkspace>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(MadeWorkspaceId));
            DefaultWorkspaces.Setup(w => w.Remove(It.IsAny<NewDefaultWorkspace>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Memberships.Setup(m => m.Save(It.IsAny<UserTenantImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            Memberships.Setup(m => m.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Roles.Setup(r => r.Assign(Administrator, "Admin", NewTenantId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        }

        public TenantFounding Founding() =>
            new(Tenants.Object, Organizations.Object, DefaultWorkspaces.Object, Policy.Object, Users.Object, Memberships.Object, GrantRule.Object,
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
            It.Is<NewOrganization>(n => n.TenantId == NewTenantId && n.Name == "Beta" && n.Slug == "main" && n.IsDefault),
            It.IsAny<CancellationToken>()), Times.Once);
        var stated = rig.OrganizationMade.ShouldNotBeNull().DefaultWorkspaceId.ShouldNotBeNull();
        rig.DefaultWorkspaces.Verify(w => w.Found(
            It.Is<NewDefaultWorkspace>(n => n.WorkspaceId == stated && n.TenantId == NewTenantId && n.OwnerName == "Beta"
                && n.OrganizationId == DefaultOrganizationId && n.OwnerUserId == Administrator),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task WithTheSettingOffATenantIsMadeWithAnOrganizationNamingNoWorkspaceAndNoWorkspaceIsMade()
    {
        var rig = new Rig();
        rig.Policy.Setup(p => p.FoundingMakesDefaultWorkspace()).Returns(GenericResult<bool>.Success(false));

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        rig.OrganizationMade.ShouldNotBeNull().DefaultWorkspaceId.ShouldBeNull();
        rig.DefaultWorkspaces.Verify(w => w.Found(It.IsAny<NewDefaultWorkspace>(), It.IsAny<CancellationToken>()), Times.Never);
        rig.Roles.Verify(r => r.Assign(Administrator, "Admin", NewTenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task WithTheSettingOffALaterFailureRemovesTheTenantAndRemovesNoWorkspace()
    {
        var rig = new Rig();
        rig.Policy.Setup(p => p.FoundingMakesDefaultWorkspace()).Returns(GenericResult<bool>.Success(false));
        rig.Roles.Setup(r => r.Assign(Administrator, "Admin", NewTenantId, It.IsAny<CancellationToken>())).ReturnsAsync(Failed("role refused"));

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.Tenants.Verify(t => t.Delete(NewTenantId, It.IsAny<CancellationToken>()), Times.Once);
        rig.DefaultWorkspaces.Verify(w => w.Remove(It.IsAny<NewDefaultWorkspace>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASettingThatCannotBeReadMakesNothing()
    {
        var rig = new Rig();
        rig.Policy.Setup(p => p.FoundingMakesDefaultWorkspace()).Returns(GenericResult<bool>.Failure(new GenericMessage("no setting")));

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        rig.NothingMade();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AFounderWhoNamesNoOrganizationSlugMakesNothing(string organizationSlug)
    {
        var rig = new Rig();

        var result = await rig.Founding().Found(
            new NewTenant("Beta", "beta", null, null, true, organizationSlug), Administrator, "Admin", TestContext.Current.CancellationToken);

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
        rig.DefaultWorkspaces.Verify(w => w.Remove(It.Is<NewDefaultWorkspace>(n => n.WorkspaceId == rig.OrganizationMade!.DefaultWorkspaceId), It.IsAny<CancellationToken>()), Times.Once);
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
