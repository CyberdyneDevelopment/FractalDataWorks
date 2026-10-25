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
/// Tests for <see cref="GrantRule"/>: a caller can give or define only what they hold themselves, and only in the tenant
/// they act in (FDW-1577).
/// </summary>
public sealed class GrantRuleTests
{
    private static readonly Guid Tenant = new("11111111-0000-0000-0000-000000000001");
    private static readonly Guid OtherTenant = new("11111111-0000-0000-0000-000000000002");
    private static readonly Guid Item = new("0f0f0f0f-0000-0000-0000-000000000001");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGive_allows_a_role_whose_permissions_the_caller_holds()
    {
        var rule = Build(Caller(["pipelines:read", "pipelines:execute", "pipelines:delegate"]), ["pipelines:read"]);

        var result = await rule.CanGive(["Viewer"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGive_refuses_a_role_that_carries_a_permission_the_caller_lacks_and_names_it()
    {
        var rule = Build(Caller(["pipelines:read"]), ["pipelines:read", "users:write"]);

        var result = await rule.CanGive(["Operator"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("users:write");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGive_refuses_a_caller_giving_Admin_when_they_only_hold_users_write()
    {
        var rule = Build(Caller(["users:write"]), ["*:*"]);

        var result = await rule.CanGive(["Admin"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Theory]
    [InlineData("pipelines:*")]
    [InlineData("*:*")]
    [InlineData("PIPELINES:READ")]
    public async Task CanGive_counts_a_held_wildcard_or_other_spelling_for_what_it_covers(string held)
    {
        var rule = Build(Caller([held, "pipelines:delegate"]), ["pipelines:read"]);

        var result = await rule.CanGive(["Viewer"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("datasets")]
    [InlineData("workspaces")]
    public async Task CanGive_lets_the_resource_wide_rights_cover_a_per_resource_permission(string resource)
    {
        var rule = Build(Caller([$"{resource}:read", $"{resource}:delegate"]), [$"{resource}/{Item:D}:read"]);

        var result = await rule.CanGive(["Reader"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGive_does_not_let_one_item_right_cover_the_whole_resource()
    {
        var rule = Build(Caller([$"datasets/{Item:D}:read", $"datasets/{Item:D}:delegate"]), ["datasets:read"]);

        var result = await rule.CanGive(["Reader"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGive_lets_a_held_everything_wildcard_give_a_role_that_carries_it()
    {
        var rule = Build(Caller(["*:*"]), ["*:*"]);

        var result = await rule.CanGive(["Admin"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CanGive_does_not_let_a_resource_wildcard_cover_another_resource()
    {
        var rule = Build(Caller(["pipelines:*"]), ["users:read"]);

        var result = await rule.CanGive(["Viewer"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGive_refuses_a_tenant_the_caller_does_not_act_in()
    {
        var rule = Build(Caller(["*:*"]), ["pipelines:read"]);

        var result = await rule.CanGive(["Viewer"], OtherTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CanGive_refuses_when_nobody_is_signed_in()
    {
        var rule = Build(null, ["pipelines:read"]);

        var result = await rule.CanGive(["Viewer"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CanGive_allows_the_system_context_in_any_tenant()
    {
        var system = new Mock<IAuthenticationContext>();
        system.SetupGet(c => c.IsAuthenticated).Returns(true);
        system.SetupGet(c => c.IsSystemContext).Returns(true);
        var rule = Build(system.Object, ["*:*"]);

        var result = await rule.CanGive(["Admin"], OtherTenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task CanGive_refuses_when_no_role_is_named()
    {
        var rule = Build(Caller(["*:*"]), []);

        var result = await rule.CanGive([], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task CanGive_passes_on_the_failure_when_the_roles_cannot_be_expanded()
    {
        var grants = new Mock<IRoleGrants>();
        grants.Setup(g => g.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Failure(new GenericMessage("the roles are unreadable")));
        var rule = new GrantRule(grants.Object, Accessor(Caller(["*:*"])), NullLogger<GrantRule>.Instance);

        var result = await rule.CanGive(["Viewer"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CanDefine_refuses_a_permission_the_caller_lacks()
    {
        var rule = Build(Caller(["settings/role:write"]), []);

        var result = rule.CanDefine(["settings/role:write", "users:delete"], Tenant);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("users:delete");
    }

    [Fact]
    public void CanDefine_allows_permissions_the_caller_holds()
    {
        var rule = Build(Caller(["users:*"]), []);

        rule.CanDefine(["users:read", "users:write"], Tenant).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void CanDefine_refuses_a_tenant_the_caller_does_not_act_in()
    {
        var rule = Build(Caller(["*:*"]), []);

        rule.CanDefine(["users:read"], OtherTenant).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void CanDefine_refuses_when_no_permission_is_named()
    {
        var rule = Build(Caller(["*:*"]), []);

        rule.CanDefine([], Tenant).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGiveInNewTenant_allows_a_role_the_caller_holds_in_a_tenant_the_caller_does_not_act_in()
    {
        var rule = Build(Caller(["*:*"]), ["pipelines:read", "users:write"]);

        var result = await rule.CanGiveInNewTenant(["Admin"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGiveInNewTenant_still_refuses_a_role_that_carries_what_the_caller_lacks()
    {
        var rule = Build(Caller(["pipelines:read"]), ["pipelines:read", "users:write"]);

        var result = await rule.CanGiveInNewTenant(["Admin"], TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("users:write");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGiveInNewTenant_refuses_when_nobody_is_signed_in_or_no_role_is_named()
    {
        (await Build(null, ["pipelines:read"]).CanGiveInNewTenant(["Admin"], TestContext.Current.CancellationToken)).IsSuccess.ShouldBeFalse();
        (await Build(Caller(["*:*"]), []).CanGiveInNewTenant([], TestContext.Current.CancellationToken)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGive_refuses_a_caller_who_holds_the_permission_but_not_the_right_to_modify_it_and_names_the_right()
    {
        var rule = Build(Caller(["pipelines:read"]), ["pipelines:read"]);

        var result = await rule.CanGive(["Viewer"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("pipelines:delegate");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGive_refuses_a_caller_who_has_the_right_to_modify_but_does_not_hold_the_permission()
    {
        var rule = Build(Caller(["pipelines:delegate"]), ["pipelines:read"]);

        var result = await rule.CanGive(["Viewer"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("pipelines:read");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGive_needs_the_right_to_modify_for_every_resource_the_role_reaches()
    {
        var rule = Build(Caller(["pipelines:read", "pipelines:delegate", "users:read"]), ["pipelines:read", "users:read"]);

        var result = await rule.CanGive(["Mixed"], Tenant, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("users:delegate");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task CanGive_a_carried_wildcard_of_everything_needs_the_wildcard_held()
    {
        var rule = Build(Caller(["users:*", "pipelines:*"]), ["*:*"]);

        (await rule.CanGive(["Admin"], Tenant, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CanDefine_needs_the_right_to_modify_as_well_as_the_permission()
    {
        var rule = Build(Caller(["users:read"]), []);

        var result = rule.CanDefine(["users:read"], Tenant);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("users:delegate");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void CanDefine_allows_a_permission_the_caller_holds_with_the_right_to_modify_it()
    {
        var rule = Build(Caller(["users:read", "users:delegate"]), []);

        rule.CanDefine(["users:read"], Tenant).IsSuccess.ShouldBeTrue();
    }

    private static IAuthenticationContext Caller(IReadOnlyList<string> permissions)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.IsAuthenticated).Returns(true);
        caller.SetupGet(c => c.IsSystemContext).Returns(false);
        caller.SetupGet(c => c.ActiveTenantId).Returns(Tenant);
        caller.SetupGet(c => c.Permissions).Returns(permissions);
        return caller.Object;
    }

    private static AuthenticationContextAccessor Accessor(IAuthenticationContext? caller)
        => new AuthenticationContextAccessor { Current = caller };

    private static GrantRule Build(IAuthenticationContext? caller, IReadOnlyCollection<string> carried)
    {
        var grants = new Mock<IRoleGrants>();
        grants.Setup(g => g.Get(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyCollection<string>>.Success(carried));
        return new GrantRule(grants.Object, Accessor(caller), NullLogger<GrantRule>.Instance);
    }
}
