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
/// one fails independently — a partition that collapsed only the tenant axis would still pass the
/// others while silently serving one tenant's rows to another.
/// </para>
/// <para>
/// The axes are read straight off <c>security.fn_TenantFilter.sql</c>: <c>System</c> (the explicit
/// elevation), <c>UserId</c> (the membership join key), <c>TenantId</c> (the tenant acted in — the
/// global tenant sees all of them). No key admits a user to Restricted rows (FDW-1086).
/// </para>
/// </remarks>
public sealed class MsSqlSessionContextCachePartitionTests
{
    private static readonly Guid TenantA = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantB = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserA = new("33333333-3333-3333-3333-333333333333");
    private static readonly Guid UserB = new("44444444-4444-4444-4444-444444444444");

    private const string RetiredReadSecretsPermission = "connections:read-secrets";

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
    public void APermissionInTheTokenChangesNeitherThePlanNorThePartition()
    {
        // No permission admits a user to Restricted rows (FDW-1086), so a token that still carries the
        // retired connections:read-secrets plans and partitions exactly like one that does not.
        var holder = Principal(UserA, TenantA, carriesRetiredReadSecrets: true);
        var plain = Principal(UserA, TenantA);

        PartitionFor(holder).ShouldBe(PartitionFor(plain));
        MsSqlSessionContextTypes.For(holder).Plan(holder).ShouldBe(MsSqlSessionContextTypes.For(plain).Plan(plain));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void SystemElevationIsNeverAUserPartition_WhateverTenantItWorksFor()
    {
        // A system elevation working for a tenant sees every row, exactly like one working for none;
        // a user acting in that tenant sees one tenant's. They must never share an entry (FDW-825).
        PartitionFor(new SystemAuthenticationContext(TenantA))
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
        // System is the FULL-VISIBILITY partition. If it ever collided with the deny
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
        PartitionFor(Principal(UserA, TenantA))
            .ShouldBe(PartitionFor(Principal(UserA, TenantA)));
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
        var left = Principal(UserA, TenantA);
        var right = Principal(UserA, TenantA);
        var different = Principal(UserA, TenantB);

        MsSqlSessionContextTypes.For(left).Plan(left)
            .ShouldBe(MsSqlSessionContextTypes.For(right).Plan(right));
        PartitionFor(left).ShouldBe(PartitionFor(right));

        MsSqlSessionContextTypes.For(different).Plan(different)
            .ShouldNotBe(MsSqlSessionContextTypes.For(left).Plan(left));
        PartitionFor(different).ShouldNotBe(PartitionFor(left));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void BoundsReplayForContextsWhosePredicateBranchJoinsLiveGrantTables()
    {
        // The user branches join tenant.UserTenantsConfiguration, tenant.TenantOrgAccess or
        // security.VisibilityGroup at query time, so revoking a membership or grant changes the next
        // query's answer while this caller's identity — and so
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
        // The system branch grants full visibility on the System key alone, and the deny principal
        // reaches only the shared-row branch. Neither consults a grant, so neither can be invalidated by an edit to
        // one — bounding them would cost hit rate to protect against a change that cannot happen.
        MsSqlSessionContextTypes.For(new SystemAuthenticationContext())
            .MaxCacheDuration(new SystemAuthenticationContext()).ShouldBe(TimeSpan.MaxValue);

        MsSqlSessionContextTypes.For(null).MaxCacheDuration(null).ShouldBe(TimeSpan.MaxValue);
    }

    private static StubAuthenticationContext Principal(
        Guid userId,
        Guid? tenantId = null,
        bool carriesRetiredReadSecrets = false)
        => new StubAuthenticationContext(userId.ToString(), tenantId, carriesRetiredReadSecrets);

    private sealed class StubAuthenticationContext(
        string userId,
        Guid? activeTenantId,
        bool carriesRetiredReadSecrets) : IAuthenticationContext
    {
        public string UserId { get; } = userId;

        public string Username => UserId;

        public IDictionary<string, object> Claims { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        public IEnumerable<string> Roles { get; } = [];

        public IEnumerable<string> Permissions { get; } = carriesRetiredReadSecrets ? [RetiredReadSecretsPermission] : [];

        public bool IsAuthenticated => true;

        public SecurityMethodBase AuthenticationMethod => (SecurityMethodBase)SecurityMethods.ByName("None");

        public DateTimeOffset? ExpiresAt => null;

        public Guid? ActiveTenantId { get; } = activeTenantId;

        public Guid? ActiveOrgId => null;

        public bool IsSystemContext => false;
    }
}
