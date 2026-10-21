using System.Linq;
using Fdw.Services.Authentication.Abstractions.Security;
using Shouldly;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// The one reading of an endpoint policy name (FDW-2246): the policy provider and the permission pre-processor both depend on it, so a name read one way by one
/// and another way by the other would refuse a caller the policy admits.
/// </summary>
public sealed class PermissionPolicyNameTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASinglePermissionIsOneResourceAndOneAction()
    {
        PermissionPolicyName.TryParse("users:read", out var permissions).ShouldBeTrue();

        permissions.ShouldHaveSingleItem().ShouldBe(("users", "read"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void PermissionsJoinedByABarAreReadInTheOrderNamed()
    {
        PermissionPolicyName.TryParse("ui-feedback:read|ui-feedback:write", out var permissions).ShouldBeTrue();

        permissions.ToList().ShouldBe([("ui-feedback", "read"), ("ui-feedback", "write")]);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("")]
    [InlineData("authenticated")]
    [InlineData(":read")]
    [InlineData("users:")]
    [InlineData("users:read|")]
    [InlineData("|users:read")]
    [InlineData("users:read|write")]
    [InlineData("users:read||users:write")]
    public void ANameInWhichAnyPartIsNotAPermissionIsNotAPermissionPolicy(string name)
    {
        PermissionPolicyName.TryParse(name, out var permissions).ShouldBeFalse();

        permissions.ShouldBeEmpty();
    }
}
