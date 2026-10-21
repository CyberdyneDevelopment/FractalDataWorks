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
/// Tests for <see cref="TenantFounding"/> (FDW-1578): a tenant is made with its first administrator, nothing is made when the person
/// is missing or the caller may not give the role, and what was made is removed again, newest first, when the administrator cannot be set.
/// </summary>
public sealed class TenantFoundingTests
{
    private static readonly Guid Administrator = new("22222222-0000-0000-0000-000000000001");
    private static readonly Guid NewTenantId = new("11111111-0000-0000-0000-000000000009");
    private static readonly NewTenant Beta = new("Beta", "beta", null, null);

    private static IGenericResult Failed(string reason) => GenericResult.Failure(new GenericMessage(reason));

    private sealed class Rig
    {
        public Mock<ITenantWriter> Tenants { get; } = new();
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
            Memberships.Setup(m => m.Save(It.IsAny<UserTenantImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult.Success());
            Memberships.Setup(m => m.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
            Roles.Setup(r => r.Assign(Administrator, "Admin", NewTenantId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        }

        public TenantFounding Founding() =>
            new(Tenants.Object, Users.Object, Memberships.Object, GrantRule.Object, Roles.Object, NullLogger<TenantFounding>.Instance);

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
    public async Task AMembershipThatCannotBeSavedRemovesTheTenantAndReportsThatFailure()
    {
        var rig = new Rig();
        rig.Memberships.Setup(m => m.Save(It.IsAny<UserTenantImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Failed("membership refused"));

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("membership refused");
        rig.Tenants.Verify(t => t.Delete(NewTenantId, It.IsAny<CancellationToken>()), Times.Once);
        rig.Memberships.Verify(m => m.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        rig.Roles.Verify(r => r.Assign(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ARoleThatCannotBeGivenRemovesTheMembershipThenTheTenant()
    {
        var rig = new Rig();
        rig.Roles.Setup(r => r.Assign(Administrator, "Admin", NewTenantId, It.IsAny<CancellationToken>())).ReturnsAsync(Failed("role refused"));
        var order = new System.Collections.Generic.List<string>();
        rig.Memberships.Setup(m => m.Delete(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("membership")).ReturnsAsync(GenericResult.Success());
        rig.Tenants.Setup(t => t.Delete(NewTenantId, It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("tenant")).ReturnsAsync(GenericResult.Success());

        var result = await rig.Founding().Found(Beta, Administrator, "Admin", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("role refused");
        order.ShouldBe(["membership", "tenant"]);
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
