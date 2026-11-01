using Fdw.Web.RestEndpoints.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.Tests.Security;

/// <summary>
/// Tests for SecurityEndpointLog MessageLogging methods.
/// </summary>
public sealed class SecurityEndpointLogTests
{
    private readonly ILogger _logger = NullLogger.Instance;

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EndpointConfiguredReturnsMessage()
    {
        // Arrange
        var endpointName = "TestEndpoint";
        var securityTier = "Public";
        var route = "/test";

        // Act
        var result = SecurityEndpointLog.EndpointConfigured(_logger, endpointName, securityTier, route);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("RESTENDPOINTS-11015");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RateLimitPolicyAppliedReturnsMessage()
    {
        // Arrange
        var endpointName = "TestEndpoint";
        var policyName = "Standard";

        // Act
        var result = SecurityEndpointLog.RateLimitPolicyApplied(_logger, endpointName, policyName);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("RESTENDPOINTS-11016");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AuthorizationPolicyAppliedReturnsMessage()
    {
        // Arrange
        var endpointName = "TestEndpoint";
        var policyName = "fdw:connections:read";

        // Act
        var result = SecurityEndpointLog.AuthorizationPolicyApplied(_logger, endpointName, policyName);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("RESTENDPOINTS-11017");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnonymousAccessConfiguredReturnsMessage()
    {
        // Arrange
        var endpointName = "TestEndpoint";
        var route = "/test";

        // Act
        var result = SecurityEndpointLog.AnonymousAccessConfigured(_logger, endpointName, route);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("RESTENDPOINTS-11018");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DevelopmentModeBypassActiveReturnsMessage()
    {
        // Arrange
        var endpointName = "TestEndpoint";

        // Act
        var result = SecurityEndpointLog.DevelopmentModeBypassActive(_logger, endpointName);

        // Assert
        result.ShouldNotBeNull();
        result.Code.ShouldBe("RESTENDPOINTS-11019");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllLogMethodsAcceptNullLogger()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act & Assert - should not throw
        SecurityEndpointLog.EndpointConfigured(logger, "Test", "Public", "/test");
        SecurityEndpointLog.RateLimitPolicyApplied(logger, "Test", "Standard");
        SecurityEndpointLog.AuthorizationPolicyApplied(logger, "Test", "policy");
        SecurityEndpointLog.AnonymousAccessConfigured(logger, "Test", "/test");
        SecurityEndpointLog.DevelopmentModeBypassActive(logger, "Test");
    }
}
