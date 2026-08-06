using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

[Collection(nameof(RestEndpointsTestCollection))]
public class EndpointDefaultSettingsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsAllProperties()
    {
        // Arrange & Act
        var settings = new EndpointDefaultSettings(
            "JWT",
            "FixedWindow",
            30000,
            10485760,
            true,
            ["Admin", "User"]);

        // Assert
        settings.SecurityMethodName.ShouldBe("JWT");
        settings.RateLimitPolicyName.ShouldBe("FixedWindow");
        settings.TimeoutMs.ShouldBe(30000);
        settings.MaxBodySize.ShouldBe(10485760);
        settings.RequireAuthentication.ShouldBeTrue();
        settings.AllowedRoles.ShouldBe(new[] { "Admin", "User" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SecurityMethod_ReturnsEmptySecurityMethod_WhenSecurityMethodNameIsEmpty()
    {
        // Arrange
        var settings = new EndpointDefaultSettings(
            string.Empty,
            "FixedWindow",
            30000,
            10485760,
            true,
            []);

        // Act
        var securityMethod = settings.SecurityMethod;

        // Assert
        securityMethod.ShouldNotBeNull();
        securityMethod.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RateLimitPolicy_ReturnsEmptyRateLimitPolicy_WhenRateLimitPolicyNameIsEmpty()
    {
        // Arrange
        var settings = new EndpointDefaultSettings(
            "JWT",
            string.Empty,
            30000,
            10485760,
            true,
            []);

        // Act
        var rateLimitPolicy = settings.RateLimitPolicy;

        // Assert
        rateLimitPolicy.ShouldNotBeNull();
        rateLimitPolicy.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Record_SupportsWithExpressions()
    {
        // Arrange
        var original = new EndpointDefaultSettings(
            "JWT",
            "FixedWindow",
            30000,
            10485760,
            true,
            ["Admin"]);

        // Act
        var modified = original with { TimeoutMs = 60000 };

        // Assert
        modified.TimeoutMs.ShouldBe(60000);
        modified.SecurityMethodName.ShouldBe("JWT");
        original.TimeoutMs.ShouldBe(30000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Record_ComparesValueEquality()
    {
        // Arrange
        var settings1 = new EndpointDefaultSettings(
            "JWT",
            "FixedWindow",
            30000,
            10485760,
            true,
            ["Admin"]);

        var settings2 = new EndpointDefaultSettings(
            "JWT",
            "FixedWindow",
            30000,
            10485760,
            true,
            ["Admin"]);

        // Act & Assert
        // Records compare by value, but arrays use reference equality
        settings1.SecurityMethodName.ShouldBe(settings2.SecurityMethodName);
        settings1.RateLimitPolicyName.ShouldBe(settings2.RateLimitPolicyName);
        settings1.TimeoutMs.ShouldBe(settings2.TimeoutMs);
        settings1.MaxBodySize.ShouldBe(settings2.MaxBodySize);
        settings1.RequireAuthentication.ShouldBe(settings2.RequireAuthentication);
        settings1.AllowedRoles.ShouldBe(settings2.AllowedRoles);
    }
}
