using System;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Pipelines.Runs;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests.Runs;

/// <summary>
/// FDW-6939: the tenant a save or a hold is judged against is the caller's own active tenant (its token's <c>tenantId</c> claim), the one every host has. The platform
/// tenant (all F) is a tenant like any other; only a caller whose token states none, or an empty one, acts in no tenant.
/// </summary>
public sealed class RunTenantTests
{
    private static readonly Guid Platform = Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF");

    private static IAuthenticationContext Caller(Guid? tenant)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.ActiveTenantId).Returns(tenant);
        return caller.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ACallerWhoseTokenStatesATenantActsInIt() =>
        RunTenant.Of(Caller(Guid.Parse("22222222-0000-0000-0000-000000000002"))).ShouldBe(Guid.Parse("22222222-0000-0000-0000-000000000002"));

    [Fact]
    [Trait("Priority", "P0")]
    public void ThePlatformTenantIsATenantLikeAnyOther() => RunTenant.Of(Caller(Platform)).ShouldBe(Platform);

    [Fact]
    [Trait("Priority", "P0")]
    public void ACallerWhoseTokenStatesNoTenantActsInNone() => RunTenant.Of(Caller(null)).ShouldBeNull();

    [Fact]
    [Trait("Priority", "P0")]
    public void AnEmptyTenantIsNoTenant() => RunTenant.Of(Caller(Guid.Empty)).ShouldBeNull();

    [Fact]
    [Trait("Priority", "P0")]
    public void NoCallerActsInNoTenant() => RunTenant.Of(null).ShouldBeNull();
}
