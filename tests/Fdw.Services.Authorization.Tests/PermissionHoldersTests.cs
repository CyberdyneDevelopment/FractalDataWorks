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
/// FDW-2144: <see cref="PermissionHolders"/> says who holds a permission in a tenant and on the platform, by the rule <see cref="UserPermissions"/> applies
/// when it bakes a token: a current, unexpired assignment, in that tenant, of a role that carries the permission.
/// </summary>
public sealed class PermissionHoldersTests
{
    private const string Permission = "ui-feedback:read";
    private static readonly Guid TenantA = new("11111111-0000-0000-0000-00000000000a");
    private static readonly Guid TenantB = new("11111111-0000-0000-0000-00000000000b");
    private static readonly Guid Global = new("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid ReadPermissionId = new("55555555-0000-0000-0000-000000000001");
    private static readonly Guid OtherPermissionId = new("55555555-0000-0000-0000-000000000002");
    private static readonly Guid DeveloperA = new("77777777-0000-0000-0000-00000000000a");
    private static readonly Guid ViewerA = new("77777777-0000-0000-0000-00000000000b");
    private static readonly Guid DeveloperB = new("77777777-0000-0000-0000-00000000000c");
    private static readonly Guid AdminGlobal = new("77777777-0000-0000-0000-00000000000d");
    private static readonly Guid Ana = new("22222222-0000-0000-0000-000000000001");
    private static readonly Guid Ben = new("22222222-0000-0000-0000-000000000002");
    private static readonly Guid Cy = new("22222222-0000-0000-0000-000000000003");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ThePeopleWhoseRoleInTheTenantCarriesThePermissionAreTheHolders()
    {
        var holders = Build([Held(Ana, DeveloperA, TenantA), Held(Ben, ViewerA, TenantA)]);

        var result = await holders.Get(Permission, TenantA, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([Ana]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ARoleHeldInAnotherTenantDoesNotCount()
    {
        var holders = Build([Held(Ana, DeveloperA, TenantA), Held(Ben, DeveloperB, TenantB)]);

        var inA = await holders.Get(Permission, TenantA, Ct);
        var inB = await holders.Get(Permission, TenantB, Ct);

        inA.Value.ShouldBe([Ana]);
        inB.Value.ShouldBe([Ben]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AnExpiredOrRevokedAssignmentDoesNotCount()
    {
        var holders = Build([
            Held(Ana, DeveloperA, TenantA),
            Held(Ben, DeveloperA, TenantA, expiresAt: DateTimeOffset.UtcNow.AddDays(-1)),
            Held(Cy, DeveloperA, TenantA, isCurrent: false),
            Held(Guid.NewGuid(), DeveloperA, TenantA, isDeleted: true)]);

        var result = await holders.Get(Permission, TenantA, Ct);

        result.Value.ShouldBe([Ana]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task AServiceIdentityIsNotAPersonToTell()
    {
        var holders = Build([Held(Ana, DeveloperA, TenantA), Held("fdw.scheduler", DeveloperA, TenantA)]);

        var result = await holders.Get(Permission, TenantA, Ct);

        result.Value.ShouldBe([Ana]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task APersonWhoHoldsTheRoleTwiceIsListedOnce()
    {
        var holders = Build([Held(Ana, DeveloperA, TenantA), Held(Ana.ToString("D").ToUpperInvariant(), DeveloperA, TenantA)]);

        var result = await holders.Get(Permission, TenantA, Ct);

        result.Value.ShouldBe([Ana]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ThePlatformsHoldersAreThoseOfTheGlobalTenant()
    {
        var holders = Build([Held(Ana, DeveloperA, TenantA), Held(Ben, AdminGlobal, Global)]);

        var result = await holders.GetOnPlatform(Permission, Ct);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe([Ben]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task APermissionThatDoesNotExistIsAFailureAndNotAnEmptyList()
    {
        var holders = Build([Held(Ana, DeveloperA, TenantA)]);

        var result = await holders.Get("ui-feedback:nothing", TenantA, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "AUTHORIZATION-32730");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(0)]
    [InlineData(2)]
    public async Task ThePlatformCannotBeFoundUnlessExactlyOneTenantIsGlobal(int marked)
    {
        var holders = Build([Held(Ana, DeveloperA, TenantA)], globalTenants: marked);

        var result = await holders.GetOnPlatform(Permission, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "AUTHORIZATION-62730");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task UnreadableTenantsAreAFailureOnThePlatform()
    {
        var holders = Build([Held(Ana, DeveloperA, TenantA)], tenantsReadable: false);

        var result = await holders.GetOnPlatform(Permission, Ct);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldContain(m => m.Code == "AUTHORIZATION-72730");
    }

    private static UserRoleImplementationConfiguration Held(Guid user, Guid roleId, Guid tenant, DateTimeOffset? expiresAt = null, bool isCurrent = true, bool isDeleted = false)
        => Held(user.ToString("D"), roleId, tenant, expiresAt, isCurrent, isDeleted);

    private static UserRoleImplementationConfiguration Held(string user, Guid roleId, Guid tenant, DateTimeOffset? expiresAt = null, bool isCurrent = true, bool isDeleted = false)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = user,
            RoleId = roleId,
            TenantId = tenant,
            ExpiresAt = expiresAt,
            IsCurrent = isCurrent,
            IsDeleted = isDeleted,
        };

    private static PermissionHolders Build(
        IReadOnlyList<UserRoleImplementationConfiguration> assignments, int globalTenants = 1, bool tenantsReadable = true)
    {
        var roles = new List<IRoleImplementationConfiguration>
        {
            new RoleImplementationConfiguration { Id = DeveloperA, Name = "Developer", TenantId = TenantA },
            new RoleImplementationConfiguration { Id = ViewerA, Name = "Viewer", TenantId = TenantA },
            new RoleImplementationConfiguration { Id = DeveloperB, Name = "Developer", TenantId = TenantB },
            new RoleImplementationConfiguration { Id = AdminGlobal, Name = "Admin", TenantId = Global },
        };
        var grants = new List<IRolePermissionImplementationConfiguration>
        {
            new RolePermissionImplementationConfiguration { RoleId = DeveloperA, PermissionId = ReadPermissionId, TenantId = TenantA },
            new RolePermissionImplementationConfiguration { RoleId = ViewerA, PermissionId = OtherPermissionId, TenantId = TenantA },
            new RolePermissionImplementationConfiguration { RoleId = DeveloperB, PermissionId = ReadPermissionId, TenantId = TenantB },
            new RolePermissionImplementationConfiguration { RoleId = AdminGlobal, PermissionId = ReadPermissionId, TenantId = Global },
        };
        var permissions = new List<IPermissionImplementationConfiguration>
        {
            new PermissionImplementationConfiguration { Id = ReadPermissionId, Name = Permission },
            new PermissionImplementationConfiguration { Id = OtherPermissionId, Name = "ui-feedback:write" },
        };

        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(t => t.GetAllTenants(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenantsReadable
                ? GenericResult<IEnumerable<ITenant>>.Success(Tenants(globalTenants))
                : GenericResult<IEnumerable<ITenant>>.Failure(new GenericMessage("the tenants are unreadable")));

        return new PermissionHolders(
            ConfigurationCatalog.Roles(roles),
            ConfigurationCatalog.Permissions(permissions),
            ConfigurationCatalog.RolePermissions(grants),
            ConfigurationCatalog.UserRoles(assignments),
            tenants.Object,
            new AuthenticationContextAccessor(),
            NullLogger<PermissionHolders>.Instance);
    }

    private static IEnumerable<ITenant> Tenants(int globalTenants)
    {
        var all = new List<ITenant> { Tenant(TenantA, isGlobal: false), Tenant(TenantB, isGlobal: false) };
        all.AddRange(Enumerable.Range(0, globalTenants).Select(i => Tenant(i == 0 ? Global : Guid.NewGuid(), isGlobal: true)));
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
