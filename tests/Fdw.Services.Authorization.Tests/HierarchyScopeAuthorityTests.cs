using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// Tests for <see cref="HierarchyScopeAuthority"/>: a right held at the thing's scope or above counts, and the session must hold it too (FDW-2327).
/// </summary>
public sealed class HierarchyScopeAuthorityTests
{
    private static readonly Guid Tenant = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = new("11111111-0000-0000-0000-000000000002");
    private static readonly Guid OrganizationA = new("22222222-0000-0000-0000-00000000000a");
    private static readonly Guid OrganizationB = new("22222222-0000-0000-0000-00000000000b");

    private const string Right = "data-views:write";

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task A_right_held_in_the_tenant_is_held_in_every_organization_below_it()
    {
        var rule = Build(Session([Right]), (OrganizationA, [Right]), (OrganizationB, [Right]), (null, [Right]));

        (await rule.May(Right, new ThingScope(Tenant, null), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await rule.May(Right, new ThingScope(Tenant, OrganizationA), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await rule.May(Right, new ThingScope(Tenant, OrganizationB), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task A_right_an_organization_grants_counts_inside_that_organization_only()
    {
        var rule = Build(Session([Right]), (OrganizationA, [Right]), (OrganizationB, []), (null, []));

        (await rule.May(Right, new ThingScope(Tenant, OrganizationA), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await rule.May(Right, new ThingScope(Tenant, OrganizationB), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeFalse();
        (await rule.May(Right, new ThingScope(Tenant, null), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task A_thing_in_a_tenant_the_caller_does_not_act_in_is_refused()
    {
        var rule = Build(Session([Right]), (null, [Right]));

        var result = await rule.May(Right, new ThingScope(OtherTenant, null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(OtherTenant.ToString("D"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task A_session_narrower_than_the_person_is_not_widened_by_what_the_person_holds()
    {
        var rule = Build(Session(["data-views:read"]), (null, [Right]));

        var result = await rule.May(Right, new ThingScope(Tenant, null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("session");
    }

    [Theory]
    [InlineData("data-views:*")]
    [InlineData("*:*")]
    public async Task A_held_wildcard_covers_the_right(string held)
    {
        var rule = Build(Session([held]), (null, [held]));

        (await rule.May(Right, new ThingScope(Tenant, null), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task A_refusal_names_the_right_and_the_scope()
    {
        var rule = Build(Session([Right]), (OrganizationA, []));

        var result = await rule.May(Right, new ThingScope(Tenant, OrganizationA), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(Right);
        result.CurrentMessage.ShouldContain(OrganizationA.ToString("D"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task A_scope_with_no_tenant_is_refused_instead_of_assuming_one()
    {
        var rule = Build(Session([Right]), (null, [Right]));

        (await rule.May(Right, new ThingScope(Guid.Empty, null), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task Nobody_signed_in_is_refused()
    {
        var rule = Build(null, (null, [Right]));

        (await rule.May(Right, new ThingScope(Tenant, null), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task The_system_context_passes_without_reading_any_rights()
    {
        var system = new Mock<IAuthenticationContext>();
        system.SetupGet(c => c.IsAuthenticated).Returns(true);
        system.SetupGet(c => c.IsSystemContext).Returns(true);
        var permissions = new Mock<IUserPermissions>(MockBehavior.Strict);

        var rule = new HierarchyScopeAuthority(Accessor(system.Object), permissions.Object, NullLogger<HierarchyScopeAuthority>.Instance);

        (await rule.May(Right, new ThingScope(Tenant, null), TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task A_read_of_the_rights_that_fails_is_returned_as_it_is()
    {
        var permissions = new Mock<IUserPermissions>();
        permissions
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<EffectiveAuthorization>.Failure(new GenericMessage("roles unavailable")));
        var rule = new HierarchyScopeAuthority(Accessor(Session([Right])), permissions.Object, NullLogger<HierarchyScopeAuthority>.Instance);

        var result = await rule.May(Right, new ThingScope(Tenant, null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("roles unavailable");
    }

    // The session: what the token the caller signed in with carries, which is all a narrower acting session holds.
    private static IAuthenticationContext Session(IReadOnlyList<string> permissions)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.IsAuthenticated).Returns(true);
        caller.SetupGet(c => c.IsSystemContext).Returns(false);
        caller.SetupGet(c => c.UserId).Returns("33333333-0000-0000-0000-000000000001");
        caller.SetupGet(c => c.ActiveTenantId).Returns(Tenant);
        caller.SetupGet(c => c.Permissions).Returns(permissions);
        return caller.Object;
    }

    private static AuthenticationContextAccessor Accessor(IAuthenticationContext? caller)
        => new AuthenticationContextAccessor { Current = caller };

    // held: the rights the person holds when asked at each organization (null is the tenant alone).
    private static HierarchyScopeAuthority Build(IAuthenticationContext? caller, params (Guid? Organization, string[] Rights)[] held)
    {
        var permissions = new Mock<IUserPermissions>();
        foreach (var (organization, rights) in held)
        {
            permissions
                .Setup(p => p.Get(It.IsAny<string>(), Tenant, organization, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<EffectiveAuthorization>.Success(new EffectiveAuthorization(rights, [], null)));
        }

        return new HierarchyScopeAuthority(Accessor(caller), permissions.Object, NullLogger<HierarchyScopeAuthority>.Instance);
    }
}
