using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Authorization.Abstractions.Tests;

/// <summary>
/// The rule for naming the roles a run acts with (FDW-1161): at least one, each once, none blank, and only roles the caller
/// holds in the tenant they act in.
/// </summary>
public sealed class RunRoleSelectionTests
{
    private static readonly Guid TenantId = new("11111111-1111-1111-1111-111111111111");

    private static IAuthenticationContext Caller(Guid? tenantId, params string[] roles)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.ActiveTenantId).Returns(tenantId);
        caller.SetupGet(c => c.Roles).Returns(roles);
        return caller.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NoRolesStatedIsNoRolesChosen()
    {
        RunRoleSelection.Chosen(null).ShouldBeEmpty();
        RunRoleSelection.Chosen([]).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ABlankNameIsNotARoleAndANameStatedTwiceIsOneRole()
    {
        RunRoleSelection.Chosen(["Admin", " ", "", "admin", "Operator"]).ShouldBe(["Admin", "Operator"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ARunNamesAtLeastOneRole()
    {
        RunRoleSelection.Refusal(Caller(TenantId, "Admin"), []).ShouldNotBeNull().ShouldContain("at least one role");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ACallerActingInNoTenantMayNameNoRole()
    {
        RunRoleSelection.Refusal(Caller(null, "Admin"), ["Admin"]).ShouldNotBeNull().ShouldContain("no tenant");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ACallerMayNotNameARoleTheyDoNotHold()
    {
        RunRoleSelection.Refusal(Caller(TenantId, "Operator"), ["Operator", "Admin"]).ShouldNotBeNull().ShouldContain("'Admin'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ACallerMayNameRolesTheyHoldWhateverTheCase()
    {
        RunRoleSelection.Refusal(Caller(TenantId, "admin", "Operator"), ["Admin", "operator"]).ShouldBeNull();
    }
}
