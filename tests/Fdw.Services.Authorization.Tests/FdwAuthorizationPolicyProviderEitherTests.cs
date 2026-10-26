using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// A policy name that lists several permissions with a bar builds one requirement that any of them satisfies (FDW-2246); a malformed list builds none, so
/// the endpoint fails loudly instead of opening.
/// </summary>
public sealed class FdwAuthorizationPolicyProviderEitherTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task ABarBetweenTwoPermissionsBuildsOneRequirementThatEitherSatisfies()
    {
        var policy = await Provider().GetPolicyAsync("ui-feedback:read|ui-feedback:write");

        var requirement = policy.ShouldNotBeNull().Requirements.OfType<FdwPermissionRequirement>().ShouldHaveSingleItem();
        requirement.Resource.ShouldBe("ui-feedback");
        requirement.Action.ShouldBe("read");
        requirement.AlsoAccepted.ShouldHaveSingleItem().ShouldBe(("ui-feedback", "write"));
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("ui-feedback:read|")]
    [InlineData("|ui-feedback:read")]
    [InlineData("ui-feedback:read|write")]
    [InlineData("ui-feedback:read|:write")]
    public async Task AMalformedListBuildsNoPolicy(string name)
    {
        var policy = await Provider().GetPolicyAsync(name);

        policy.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task ASinglePermissionStillBuildsTheSingleRequirement()
    {
        var policy = await Provider().GetPolicyAsync("ui-feedback:read");

        policy.ShouldNotBeNull().Requirements.OfType<FdwPermissionRequirement>().ShouldHaveSingleItem().AlsoAccepted.ShouldBeEmpty();
    }

    private static FdwAuthorizationPolicyProvider Provider() =>
        new(Options.Create(new Microsoft.AspNetCore.Authorization.AuthorizationOptions()), Mock.Of<ISystemRoleConfiguration>(), NullLogger<FdwAuthorizationPolicyProvider>.Instance);
}
