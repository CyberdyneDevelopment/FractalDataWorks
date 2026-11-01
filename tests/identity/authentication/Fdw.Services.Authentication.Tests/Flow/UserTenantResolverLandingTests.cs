using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Binding;
using Fdw.Services.Users.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// FDW-2938: a sign-in lands in the tenant the person marked as their default and in the department of it they marked, if they marked one.
/// A person with no default tenant has nowhere to land and is refused, not placed by guesswork.
/// </summary>
public sealed class UserTenantResolverLandingTests
{
    private static readonly Guid Person = Guid.Parse("0a1b2c3d-0000-0000-0000-00000000000a");
    private static readonly Guid Acme = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Globex = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid Finance = Guid.Parse("44444444-0000-0000-0000-000000000001");

    private static UserTenantResolver ResolverOver(params HeldTenant[] held)
    {
        var memberships = new Mock<IUserTenantsHeld>();
        IReadOnlyList<HeldTenant> rows = held;
        memberships.Setup(p => p.Get(Person, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<HeldTenant>>.Success(rows));
        return new UserTenantResolver(memberships.Object, new AuthenticationContextAccessor());
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASignInLandsInTheDepartmentOfTheDefaultTenantThePersonMarked()
    {
        var landing = await ResolverOver(new HeldTenant(Globex, false, Finance), new HeldTenant(Acme, true, Finance))
            .LandingFor(Person, TestContext.Current.CancellationToken);

        landing.IsSuccess.ShouldBeTrue(landing.CurrentMessage);
        landing.Value.ShouldBe(new Landing(Acme, Finance));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ASignInWithNoDepartmentMarkedLandsInTheTenantWithNoDepartment()
    {
        var landing = await ResolverOver(new HeldTenant(Acme, true, null))
            .LandingFor(Person, TestContext.Current.CancellationToken);

        landing.IsSuccess.ShouldBeTrue(landing.CurrentMessage);
        landing.Value.ShouldBe(new Landing(Acme, null));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ADepartmentMarkedOnATenantThatIsNotTheDefaultIsNotLandedIn()
    {
        var landing = await ResolverOver(new HeldTenant(Acme, true, null), new HeldTenant(Globex, false, Finance))
            .LandingFor(Person, TestContext.Current.CancellationToken);

        landing.Value.ShouldBe(new Landing(Acme, null));
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task APersonWithNoDefaultTenantHasNowhereToLandAndIsRefused()
    {
        var landing = await ResolverOver(new HeldTenant(Acme, false, Finance), new HeldTenant(Globex, false, null))
            .LandingFor(Person, TestContext.Current.CancellationToken);

        landing.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task TheTenantAnswerIsTheTenantOfTheLanding()
    {
        var tenant = await ResolverOver(new HeldTenant(Acme, true, Finance))
            .TenantFor(Person, TestContext.Current.CancellationToken);

        tenant.IsSuccess.ShouldBeTrue(tenant.CurrentMessage);
        tenant.Value.ShouldBe(Acme);
    }
}
