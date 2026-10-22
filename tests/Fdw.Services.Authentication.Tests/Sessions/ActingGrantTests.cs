using Fdw.Services.Authentication.Sessions;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Sessions;

/// <summary>
/// The narrowing rule for acting with a role (FDW-2284): a role is allowed only when the caller already holds everything it carries.
/// </summary>
public sealed class ActingGrantTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ARoleNoWiderThanTheCallerIsAllowed()
    {
        ActingGrant.NotCovered(["projects:read", "reports:*"], ["projects:read", "reports:write"]).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ARoleWiderThanTheCallerIsRefusedForWhatIsMissing()
    {
        string.Join(",", ActingGrant.NotCovered(["projects:read"], ["projects:read", "projects:write", "scripts:run"]))
            .ShouldBe("projects:write,scripts:run");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void TheSuperGrantCoversNothingSoItDoesNotAllowARole()
    {
        string.Join(",", ActingGrant.NotCovered(["*:*"], ["projects:read"])).ShouldBe("projects:read");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ARoleWithNoPermissionsIsAllowedAndEachMissingPermissionIsNamedOnce()
    {
        ActingGrant.NotCovered(["projects:read"], []).ShouldBeEmpty();
        string.Join(",", ActingGrant.NotCovered([], ["a:b", "a:b", "c:d"])).ShouldBe("a:b,c:d");
    }
}
