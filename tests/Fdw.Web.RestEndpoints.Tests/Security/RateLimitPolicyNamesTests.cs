using System.Linq;
using Fdw.Web.RestEndpoints.Security;

namespace Fdw.Web.RestEndpoints.Tests.Security;

/// <summary>
/// Tests for RateLimitPolicyNames constant values.
/// </summary>
public sealed class RateLimitPolicyNamesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void StandardReturnsExpectedValue()
    {
        RateLimitPolicyNames.Standard.ShouldBe("Standard");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AuthenticatedReturnsExpectedValue()
    {
        RateLimitPolicyNames.Authenticated.ShouldBe("Authenticated");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void PremiumReturnsExpectedValue()
    {
        RateLimitPolicyNames.Premium.ShouldBe("Premium");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AdminReturnsExpectedValue()
    {
        RateLimitPolicyNames.Admin.ShouldBe("Admin");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllConstantsAreNonNullAndNonEmpty()
    {
        // Arrange & Act
        var standard = RateLimitPolicyNames.Standard;
        var authenticated = RateLimitPolicyNames.Authenticated;
        var premium = RateLimitPolicyNames.Premium;
        var admin = RateLimitPolicyNames.Admin;

        // Assert
        standard.ShouldNotBeNullOrWhiteSpace();
        authenticated.ShouldNotBeNullOrWhiteSpace();
        premium.ShouldNotBeNullOrWhiteSpace();
        admin.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllConstantsAreUnique()
    {
        // Arrange
        var policies = new[]
        {
            RateLimitPolicyNames.Standard,
            RateLimitPolicyNames.Authenticated,
            RateLimitPolicyNames.Premium,
            RateLimitPolicyNames.Admin
        };

        // Act
        var distinctCount = policies.Distinct().Count();

        // Assert
        distinctCount.ShouldBe(4);
    }
}
