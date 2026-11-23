using System.Collections.Generic;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

/// <summary>
/// FDW-6528: the one rule for "may this person act in this tenant" (FDW-825): the tenant exists and is active, and the person is a member of it or of the global
/// tenant. Every provider that answers from stored rows asks this rule, so these cases are the answer for all of them.
/// </summary>
public sealed class TenantAccessRuleTests
{
    private static readonly Guid Global = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid Default = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid Inactive = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid Missing = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly IReadOnlyCollection<TenantFact> Tenants =
    [
        new TenantFact(Global, IsActive: true, IsGlobal: true),
        new TenantFact(Default, IsActive: true, IsGlobal: false),
        new TenantFact(Inactive, IsActive: false, IsGlobal: false),
    ];

    private static HashSet<Guid> Members(params Guid[] tenants) => [.. tenants];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AMemberOfAnActiveTenantMayActInIt()
        => TenantAccessRule.Allows(Default, Tenants, Members(Default)).ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void APersonWithNoMembershipMayNotActInAnyTenant()
    {
        TenantAccessRule.Allows(Default, Tenants, Members()).ShouldBeFalse();
        TenantAccessRule.Allows(Global, Tenants, Members()).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AMemberOfAnotherTenantMayNotActInThisOne()
        => TenantAccessRule.Allows(Default, Tenants, Members(Inactive)).ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AMemberOfTheGlobalTenantMayActInAnyActiveTenant()
    {
        TenantAccessRule.Allows(Default, Tenants, Members(Global)).ShouldBeTrue();
        TenantAccessRule.Allows(Global, Tenants, Members(Global)).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NobodyMayActInAnInactiveTenantNotEvenAMemberOfTheGlobalOne()
    {
        TenantAccessRule.Allows(Inactive, Tenants, Members(Inactive)).ShouldBeFalse();
        TenantAccessRule.Allows(Inactive, Tenants, Members(Global)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NobodyMayActInATenantThatDoesNotExist()
        => TenantAccessRule.Allows(Missing, Tenants, Members(Missing, Global)).ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AGlobalMembershipCountsOnlyWhenTheGlobalTenantExists()
        => TenantAccessRule.Allows(Default, [new TenantFact(Default, IsActive: true, IsGlobal: false)], Members(Global)).ShouldBeFalse();
}
