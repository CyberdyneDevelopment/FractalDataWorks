using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.DataConfigurationRules;
using Fdw.Services.Data.Logging;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Workspaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.DataConfigurationRules;

/// <summary>FDW-6346: the names a rule shows are looked up once for each distinct user or owner, and one that cannot be found fails with its own code and is never empty.</summary>
public sealed class DataConfigurationRuleNamesTests
{
    private static readonly Guid Dana = Guid.Parse("0193a100-aaaa-7bbb-8ccc-0123456789ab");
    private static readonly Guid Lee = Guid.Parse("0193a100-bbbb-7bbb-8ccc-0123456789ab");
    private static readonly Guid Org = Guid.Parse("0193a3e0-1b2c-7d4e-8f50-6a7b8c9d0e1f");

    private readonly Mock<IUserNames> _users = new(MockBehavior.Strict);
    private readonly Mock<IOrganizationProvider> _organizations = new(MockBehavior.Strict);

    private readonly Mock<IWorkspaceConfigurationProvider> _workspaces = new(MockBehavior.Strict);

    private readonly Mock<IPermissionHolders> _holders = new(MockBehavior.Strict);
    private readonly Mock<ITenantContext> _tenant = new(MockBehavior.Strict);

    private DataConfigurationRuleNames Names() => new(
        NullLogger<DataConfigurationRuleNames>.Instance, _users.Object, _organizations.Object, _workspaces.Object, _holders.Object, _tenant.Object);

    private void Workspace(Guid id, string? name) =>
        _workspaces.Setup(w => w.Get(id, It.IsAny<CancellationToken>())).ReturnsAsync(name is null
            ? GenericResult<IWorkspaceImplementationConfiguration>.Failure(DataOptionRulesLog.OwnerNotFound(NullLogger.Instance, "Workspace", id.ToString("D")))
            : GenericResult<IWorkspaceImplementationConfiguration>.Success(new WorkspaceImplementationConfiguration { Id = id, Name = name }));

    private void User(Guid id, string? name) =>
        _users.Setup(u => u.Get(id, It.IsAny<CancellationToken>())).ReturnsAsync(name is null
            ? GenericResult<string>.Failure(DataOptionRulesLog.UserNotFound(NullLogger.Instance, id.ToString("D")))
            : GenericResult<string>.Success(name));

    [Fact]
    public async Task EachDistinctUserIsReadOnceAndNamed()
    {
        User(Dana, "Dana");
        User(Lee, "Lee");

        var names = await Names().Users([Dana, Lee, Dana, Dana], TestContext.Current.CancellationToken);

        names.IsSuccess.ShouldBeTrue(names.CurrentMessage);
        names.Value![Dana].ShouldBe("Dana");
        names.Value[Lee].ShouldBe("Lee");
        _users.Verify(u => u.Get(Dana, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AUserWithNoNameFailsWithItsCodeAndIsNeverAnEmptyName()
    {
        User(Dana, "Dana");
        User(Lee, null);

        var names = await Names().Users([Dana, Lee], TestContext.Current.CancellationToken);

        names.Messages.Select(m => m.Code).ShouldContain("DATA-31691");
    }

    [Fact]
    public async Task AUserWhoseNameIsBlankIsNotFound()
    {
        User(Dana, "  ");

        var names = await Names().Users([Dana], TestContext.Current.CancellationToken);

        names.Messages.Select(m => m.Code).ShouldContain("DATA-31691");
    }

    [Fact]
    public async Task AnOrganizationOwnerIsNamedFromTheOrganizationProvider()
    {
        _organizations
            .Setup(o => o.Get(Org, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(new OrganizationConfiguration { Id = Org, Name = "Sports" }));

        var names = await Names().Owners("Organization", [Org, Org], TestContext.Current.CancellationToken);

        names.IsSuccess.ShouldBeTrue(names.CurrentMessage);
        names.Value![Org].ShouldBe("Sports");
        _organizations.Verify(o => o.Get(Org, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnOrganizationWithNoNameIsNotFoundAndNotShownEmpty()
    {
        _organizations
            .Setup(o => o.Get(Org, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<OrganizationConfiguration>.Success(new OrganizationConfiguration { Id = Org }));

        var names = await Names().Owners("Organization", [Org], TestContext.Current.CancellationToken);

        names.Messages.Select(m => m.Code).ShouldContain("DATA-31688");
    }

    // Release-Supervisor (2026-11-17): a workspace's name comes from the workspaces domain's own provider, never a made-up or empty owner name.
    [Fact]
    public async Task AWorkspaceOwnerIsNamedFromTheWorkspaceProviderOncePerDistinctId()
    {
        Workspace(Org, "Scouting");

        var names = await Names().Owners("Workspace", [Org, Org], TestContext.Current.CancellationToken);

        names.IsSuccess.ShouldBeTrue(names.CurrentMessage);
        names.Value![Org].ShouldBe("Scouting");
        _workspaces.Verify(w => w.Get(Org, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AWorkspaceTheProviderDoesNotFindIsNotFound()
    {
        Workspace(Org, null);

        var names = await Names().Owners("Workspace", [Org], TestContext.Current.CancellationToken);

        names.Messages.Select(m => m.Code).ShouldContain("DATA-31688");
    }

    [Fact]
    public async Task AWorkspaceWithABlankNameIsNotFoundAndNotShownEmpty()
    {
        Workspace(Org, "  ");

        var names = await Names().Owners("Workspace", [Org], TestContext.Current.CancellationToken);

        names.Messages.Select(m => m.Code).ShouldContain("DATA-31688");
    }

    [Fact]
    public async Task AnOwnerKindThatIsNotOneFails()
    {
        var names = await Names().Owners("Planet", [Org], TestContext.Current.CancellationToken);

        names.Messages.Select(m => m.Code).ShouldContain("DATA-31687");
    }

    private static readonly Guid Tenant = Guid.Parse("0193a3e0-9999-7d4e-8f50-6a7b8c9d0e1f");

    [Fact]
    public async Task TheCustodianPickerListsTheTenantsHoldersOfDatastoresWriteWithTheirNames()
    {
        _tenant.Setup(t => t.TenantId).Returns(Tenant);
        _holders
            .Setup(h => h.Get("datastores:write", Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<Guid>>.Success([Dana, Lee]));
        User(Dana, "Dana");
        User(Lee, "Lee");

        var custodians = await Names().Custodians(TestContext.Current.CancellationToken);

        custodians.IsSuccess.ShouldBeTrue(custodians.CurrentMessage);
        custodians.Value![Dana].ShouldBe("Dana");
        custodians.Value[Lee].ShouldBe("Lee");
    }

    // A caller with no tenant is answered by the platform's holders, the other scope the holders directory declares.
    [Fact]
    public async Task ACallerWithNoTenantIsListedThePlatformsHolders()
    {
        _tenant.Setup(t => t.TenantId).Returns((Guid?)null);
        _holders
            .Setup(h => h.GetOnPlatform("datastores:write", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<Guid>>.Success([Dana]));
        User(Dana, "Dana");

        var custodians = await Names().Custodians(TestContext.Current.CancellationToken);

        custodians.IsSuccess.ShouldBeTrue(custodians.CurrentMessage);
        custodians.Value.ShouldNotBeNull().Keys.ShouldBe([Dana]);
    }

    [Fact]
    public async Task HoldersThatCannotBeReadFailTheListAndAreNeverAnEmptyList()
    {
        _tenant.Setup(t => t.TenantId).Returns(Tenant);
        _holders
            .Setup(h => h.Get("datastores:write", Tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<Guid>>.Failure(DataOptionRulesLog.UserNotFound(NullLogger.Instance, "holders")));

        var custodians = await Names().Custodians(TestContext.Current.CancellationToken);

        custodians.IsFailure.ShouldBeTrue();
    }
}
