using System;
using System.Collections.Generic;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.MsSql;
using Fdw.Web.Http.Abstractions.Security;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Sql.Tests;

/// <summary>
/// Asserts that the reference scheme's cache partition separates every pair of principals that
/// <c>security.fn_TenantFilter</c> would show different rows to.
/// </summary>
/// <remarks>
/// <para>
/// These are the assertions that make result caching safe above the connection layer. A cache keyed
/// on query shape alone serves one caller's filtered result to the next; the partition is what makes
/// two callers' entries distinct. So every axis the predicate branches on gets its own test, and each
/// one fails independently — a partition that collapsed only the <c>CanReadSecrets</c> axis would
/// still pass the other four while silently leaking restricted rows.
/// </para>
/// <para>
/// The axes are read straight off <c>security.fn_TenantFilter.sql</c>: <c>UserId</c> (Mode 1 bypass
/// when null, and the <c>tenant.TenantOrgAccess</c> join key in Modes 2 and 3), <c>TenantId</c>
/// (Mode 3), <c>CrossTenant</c> (Mode 2) and <c>CanReadSecrets</c> (Mode 4).
/// </para>
/// </remarks>
public sealed class MsSqlSessionContextCachePartitionTests
{
    private static readonly Guid TenantA = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantB = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserA = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid UserB = new("44444444-4444-4444-4444-444444444444");

    private const string ReadSecretsPermission = "connections:read-secrets";

    private static string PartitionFor(IAuthenticationContext? authenticationContext)
        => MsSqlSessionContextTypes.For(authenticationContext).CachePartition(authenticationContext);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DistinguishesTwoUsersInTheSameTenant()
    {
        PartitionFor(Principal(UserA, TenantA))
            .ShouldNotBe(PartitionFor(Principal(UserB, TenantA)));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DistinguishesCanReadSecretsForTheSameUserAndTenant()
    {
        // The highest-value leak, and the one a tenant/org discriminator cannot see at all: Mode 4
        // makes restricted system rows visible ONLY to callers holding connections:read-secrets.
        // Identical principals but for that permission must never share a cache entry.
        PartitionFor(Principal(UserA, TenantA, canReadSecrets: true))
            .ShouldNotBe(PartitionFor(Principal(UserA, TenantA, canReadSecrets: false)));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DistinguishesCrossTenantFromStrictTenantScope()
    {
        // Mode 2 vs Mode 3: the same user in cross-tenant scope sees every tenant they hold a grant
        // in, rather than only the active one. Two different visibility universes, one principal.
        PartitionFor(Principal(UserA, tenantId: null, isCrossTenant: true))
            .ShouldNotBe(PartitionFor(Principal(UserA, TenantA)));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DistinguishesTenantsForTheSameUser()
        => PartitionFor(Principal(UserA, TenantA))
            .ShouldNotBe(PartitionFor(Principal(UserA, TenantB)));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SystemElevationDenyAndRealUserAreThreeDistinctPartitions()
    {
        // System is the FULL-VISIBILITY partition (Mode 1). If it ever collided with the deny
        // partition or a user's, boot-time elevated reads would be served to tenant callers — the
        // worst available failure, and the reason "no session context" must never be a default.
        var partitions = new HashSet<string>(StringComparer.Ordinal)
        {
            PartitionFor(new SystemAuthenticationContext()),
            PartitionFor(null),
            PartitionFor(Principal(UserA, TenantA)),
        };

        partitions.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void IsStableForTheSamePrincipal()
    {
        // A partition that varied per call would make every read a cache miss — correct, but it
        // would hide a broken partition behind an apparently working cache.
        PartitionFor(Principal(UserA, TenantA, canReadSecrets: true))
            .ShouldBe(PartitionFor(Principal(UserA, TenantA, canReadSecrets: true)));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void EqualPlansProduceEqualPartitionsAndUnequalPlansDoNot()
    {
        // The drift property, stated directly: the partition is a function of the plan and nothing
        // else. MsSqlSessionContextBase derives it from Plan() and seals it, so a token can never
        // describe a session other than the one that will actually be applied. This test fails the
        // moment someone reintroduces an independently-computed partition.
        var left = Principal(UserA, TenantA, canReadSecrets: true);
        var right = Principal(UserA, TenantA, canReadSecrets: true);
        var different = Principal(UserA, TenantA, canReadSecrets: false);

        MsSqlSessionContextTypes.For(left).Plan(left).CanReadSecrets
            .ShouldBe(MsSqlSessionContextTypes.For(right).Plan(right).CanReadSecrets);
        PartitionFor(left).ShouldBe(PartitionFor(right));

        MsSqlSessionContextTypes.For(different).Plan(different).CanReadSecrets
            .ShouldNotBe(MsSqlSessionContextTypes.For(left).Plan(left).CanReadSecrets);
        PartitionFor(different).ShouldNotBe(PartitionFor(left));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void BoundsReplayForContextsWhosePredicateBranchJoinsLiveGrantTables()
    {
        // Modes 2, 3 and 4 all join tenant.TenantOrgAccess or security.VisibilityGroup at query time,
        // so revoking a grant changes the next query's answer while this caller's identity — and so
        // its partition — is unchanged. Without a ceiling the revoked user keeps being served the rows
        // they just lost for as long as the entry lives.
        var principal = Principal(UserA, TenantA);

        MsSqlSessionContextTypes.For(principal).MaxCacheDuration(principal)
            .ShouldBeLessThan(TimeSpan.FromMinutes(5));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void DoesNotBoundContextsWhoseBranchJoinsNothing()
    {
        // Mode 1 grants full visibility on a null UserId alone, and the deny principal reaches only
        // the shared-row branch. Neither consults a grant, so neither can be invalidated by an edit to
        // one — bounding them would cost hit rate to protect against a change that cannot happen.
        MsSqlSessionContextTypes.For(new SystemAuthenticationContext())
            .MaxCacheDuration(new SystemAuthenticationContext()).ShouldBe(TimeSpan.MaxValue);

        MsSqlSessionContextTypes.For(null).MaxCacheDuration(null).ShouldBe(TimeSpan.MaxValue);
    }

    private static IAuthenticationContext Principal(
        Guid userId,
        Guid? tenantId = null,
        bool isCrossTenant = false,
        bool canReadSecrets = false)
        => new StubAuthenticationContext(userId.ToString(), tenantId, isCrossTenant, canReadSecrets);

    private sealed class StubAuthenticationContext(
        string userId,
        Guid? activeTenantId,
        bool isCrossTenant,
        bool canReadSecrets) : IAuthenticationContext
    {
        public string UserId { get; } = userId;

        public string Username => UserId;

        public IDictionary<string, object> Claims { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        public IEnumerable<string> Roles { get; } = [];

        public IEnumerable<string> Permissions { get; } = canReadSecrets ? [ReadSecretsPermission] : [];

        public bool IsAuthenticated => true;

        public SecurityMethodBase AuthenticationMethod => (SecurityMethodBase)SecurityMethods.ByName("None");

        public DateTimeOffset? ExpiresAt => null;

        public Guid? ActiveTenantId { get; } = activeTenantId;

        public Guid? ActiveOrgId => null;

        public bool IsCrossTenant { get; } = isCrossTenant;

        public bool IsSystemContext => false;
    }
}
