using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for <see cref="PlatformAdministratorGuard"/>: the platform is never left with no administrator (FDW-1577).
/// </summary>
public sealed class PlatformAdministratorGuardTests
{
    private const string AdminName = "Admin";
    private static readonly Guid GlobalTenant = new("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid OtherTenant = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid AdminRoleId = new("77777777-0000-0000-0000-000000000001");
    private static readonly Guid FirstAdmin = new("22222222-0000-0000-0000-000000000001");
    private static readonly Guid SecondAdmin = new("22222222-0000-0000-0000-000000000002");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanRemove_refuses_taking_Admin_from_the_last_holder_in_the_global_tenant()
    {
        var guard = Build([Held(FirstAdmin)]);

        var result = await guard.CanRemove(FirstAdmin, AdminName, GlobalTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CanRemove_allows_taking_Admin_from_one_of_two_holders()
    {
        var guard = Build([Held(FirstAdmin), Held(SecondAdmin)]);

        var result = await guard.CanRemove(FirstAdmin, AdminName, GlobalTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CanRemove_does_not_count_an_expired_or_revoked_holder_as_remaining()
    {
        var guard = Build([
            Held(FirstAdmin),
            Held(SecondAdmin, expiresAt: DateTimeOffset.UtcNow.AddDays(-1)),
            Held(Guid.NewGuid(), isCurrent: false),
            Held(Guid.NewGuid(), isDeleted: true),
        ]);

        var result = await guard.CanRemove(FirstAdmin, AdminName, GlobalTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CanRemove_counts_a_service_identity_as_a_remaining_administrator()
    {
        var guard = Build([Held(FirstAdmin), Held("fdw.scheduler")]);

        var result = await guard.CanRemove(FirstAdmin, AdminName, GlobalTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CanRemove_matches_the_holder_by_their_guid_in_any_spelling()
    {
        var guard = Build([Held(FirstAdmin.ToString("D").ToUpperInvariant())]);

        var result = await guard.CanRemove(FirstAdmin, AdminName, GlobalTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CanRemove_allows_a_person_who_does_not_hold_Admin()
    {
        var guard = Build([Held(SecondAdmin)]);

        var result = await guard.CanRemove(FirstAdmin, AdminName, GlobalTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CanRemove_allows_Admin_to_go_from_the_last_holder_in_another_tenant()
    {
        var guard = Build([Held(FirstAdmin, OtherTenant)]);

        var result = await guard.CanRemove(FirstAdmin, AdminName, OtherTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CanRemove_allows_any_role_other_than_Admin()
    {
        var guard = Build([Held(FirstAdmin)]);

        var result = await guard.CanRemove(FirstAdmin, "Viewer", GlobalTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CanRemove_matches_the_role_name_without_regard_to_case()
    {
        var guard = Build([Held(FirstAdmin)]);

        var result = await guard.CanRemove(FirstAdmin, "admin", GlobalTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task CanRemove_fails_when_the_number_of_global_tenants_is_not_one(int marked)
    {
        var guard = Build([Held(FirstAdmin)], globalTenants: marked);

        var result = await guard.CanRemove(FirstAdmin, AdminName, GlobalTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CanRemove_fails_when_the_tenants_cannot_be_read()
    {
        var guard = Build([Held(FirstAdmin)], tenantsReadable: false);

        var result = await guard.CanRemove(FirstAdmin, AdminName, GlobalTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CanDeleteRole_refuses_deleting_Admin_while_it_is_held_in_the_global_tenant()
    {
        var guard = Build([Held(FirstAdmin)]);

        var result = await guard.CanDeleteRole(AdminName, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CanDeleteRole_allows_deleting_Admin_when_nobody_holds_it()
    {
        var guard = Build([]);

        var result = await guard.CanDeleteRole(AdminName, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CanDeleteRole_allows_any_role_other_than_Admin()
    {
        var guard = Build([Held(FirstAdmin)]);

        var result = await guard.CanDeleteRole("Viewer", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    private static UserRoleImplementationConfiguration Held(Guid user, Guid? tenant = null, DateTimeOffset? expiresAt = null,
        bool isCurrent = true, bool isDeleted = false)
        => Held(user.ToString(), tenant, expiresAt, isCurrent, isDeleted);

    private static UserRoleImplementationConfiguration Held(string user, Guid? tenant = null, DateTimeOffset? expiresAt = null,
        bool isCurrent = true, bool isDeleted = false)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = user,
            RoleId = AdminRoleId,
            TenantId = tenant ?? GlobalTenant,
            ExpiresAt = expiresAt,
            IsCurrent = isCurrent,
            IsDeleted = isDeleted,
        };

    private static PlatformAdministratorGuard Build(IReadOnlyList<UserRoleImplementationConfiguration> assignments,
        int globalTenants = 1, bool tenantsReadable = true)
    {
        var systemRoles = new Mock<ISystemRoleConfiguration>();
        systemRoles.SetupGet(s => s.AdminRoleName).Returns(AdminName);

        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(t => t.GetAllTenants(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantsReadable
                ? GenericResult<IEnumerable<ITenant>>.Success(Tenants(globalTenants))
                : GenericResult<IEnumerable<ITenant>>.Failure(new GenericMessage("the tenants are unreadable")));

        return new PlatformAdministratorGuard(
            systemRoles.Object,
            tenants.Object,
            ConfigurationCatalog.Roles([new RoleImplementationConfiguration { Id = AdminRoleId, Name = AdminName, TenantId = GlobalTenant }]),
            ConfigurationCatalog.UserRoles(assignments),
            NullLogger<PlatformAdministratorGuard>.Instance);
    }

    private static List<ITenant> Tenants(int globalTenants)
    {
        var all = new List<ITenant>
        {
            Tenant(OtherTenant, isGlobal: false),
        };
        all.AddRange(Enumerable.Range(0, globalTenants).Select(i => Tenant(i == 0 ? GlobalTenant : Guid.NewGuid(), isGlobal: true)));
        return all;
    }

    private static ITenant Tenant(Guid id, bool isGlobal)
    {
        var tenant = new Mock<ITenant>();
        tenant.SetupGet(t => t.Id).Returns(id);
        tenant.SetupGet(t => t.IsGlobal).Returns(isGlobal);
        return tenant.Object;
    }
}
