using System;
using Fdw.Web.RestEndpoints.Configuration;

namespace Fdw.Web.RestEndpoints.Tests.Configuration;

[Collection(nameof(RestEndpointsTestCollection))]
public class EndpointDefaultsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultSecurityMethod_IsNotNull()
    {
        // Arrange & Act
        var securityMethod = EndpointDefaults.DefaultSecurityMethod;

        // Assert
        securityMethod.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultRateLimitPolicy_IsNotNull()
    {
        // Arrange & Act
        var rateLimitPolicy = EndpointDefaults.DefaultRateLimitPolicy;

        // Assert
        rateLimitPolicy.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultRequestTimeoutMs_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultRequestTimeoutMs.ShouldBe(30000);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultMaxRequestBodySize_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultMaxRequestBodySize.ShouldBe(10485760);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultRateLimitWindowSeconds_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultRateLimitWindowSeconds.ShouldBe(60);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultRateLimitMaxRequests_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultRateLimitMaxRequests.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultCacheDurationSeconds_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultCacheDurationSeconds.ShouldBe(300);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultContentType_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultContentType.ShouldBe("application/json");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultApiVersion_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultApiVersion.ShouldBe("v1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultOperationTimeout_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultOperationTimeout.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultSuccessStatusCode_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultSuccessStatusCode.ShouldBe(200);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultGenericValidationErrorStatusCode_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultGenericValidationErrorStatusCode.ShouldBe(400);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultAuthenticationErrorStatusCode_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultAuthenticationErrorStatusCode.ShouldBe(401);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultAuthorizationErrorStatusCode_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultAuthorizationErrorStatusCode.ShouldBe(403);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultNotFoundStatusCode_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultNotFoundStatusCode.ShouldBe(404);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultRateLimitStatusCode_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultRateLimitStatusCode.ShouldBe(429);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultServerErrorStatusCode_IsCorrect()
    {
        // Arrange & Act & Assert
        EndpointDefaults.DefaultServerErrorStatusCode.ShouldBe(500);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultResponseHeaders_ContainsExpectedHeaders()
    {
        // Arrange & Act
        var headers = EndpointDefaults.DefaultResponseHeaders;

        // Assert
        headers.ShouldNotBeNull();
        headers.ShouldContain("X-Content-Type-Options: nosniff");
        headers.ShouldContain("X-Frame-Options: DENY");
        headers.ShouldContain("X-XSS-Protection: 1; mode=block");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultCorsHeaders_ContainsExpectedHeaders()
    {
        // Arrange & Act
        var headers = EndpointDefaults.DefaultCorsHeaders;

        // Assert
        headers.ShouldNotBeNull();
        headers.ShouldContain("Content-Type");
        headers.ShouldContain("Authorization");
        headers.ShouldContain("X-API-Key");
        headers.ShouldContain("X-Requested-With");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetDefaultsForType_ReturnsNonNullSettings()
    {
        // Arrange
        var mock = new Mock<Fdw.Web.Http.Abstractions.EndPoints.IEndpointType>();
        mock.Setup(e => e.TimeoutMs).Returns(EndpointDefaults.DefaultRequestTimeoutMs);
        mock.Setup(e => e.MaxBodySize).Returns(EndpointDefaults.DefaultMaxRequestBodySize);
        mock.Setup(e => e.RequiresAuthentication).Returns(true);
        mock.Setup(e => e.SecurityMethodName).Returns("None");
        mock.Setup(e => e.RateLimitPolicyName).Returns("None");
        mock.Setup(e => e.AllowedRoles).Returns(Array.Empty<string>());

        // Act
        var settings = EndpointDefaults.GetDefaultsForType(mock.Object);

        // Assert
        settings.ShouldNotBeNull();
        settings.TimeoutMs.ShouldBe(EndpointDefaults.DefaultRequestTimeoutMs);
        settings.MaxBodySize.ShouldBe(EndpointDefaults.DefaultMaxRequestBodySize);
        settings.RequireAuthentication.ShouldBeTrue();
    }
}
