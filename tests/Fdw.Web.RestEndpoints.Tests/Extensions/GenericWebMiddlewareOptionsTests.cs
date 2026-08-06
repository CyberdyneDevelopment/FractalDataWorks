using System;
using Fdw.Web.RestEndpoints.Extensions;

namespace Fdw.Web.RestEndpoints.Tests.Extensions;

public class GenericWebMiddlewareOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions();

        // Assert
        options.EnableRequestValidation.ShouldBeTrue();
        options.EnableSecurityHeaders.ShouldBeTrue();
        options.EnableRateLimiting.ShouldBeTrue();
        options.EnableAuthentication.ShouldBeTrue();
        options.EnableAuthorization.ShouldBeTrue();
        options.EnableCors.ShouldBeTrue();
        options.EnableExceptionHandling.ShouldBeTrue();
        options.EnableRequestResponseLogging.ShouldBeFalse();
        options.EnablePerformanceMonitoring.ShouldBeTrue();
        options.EnableHealthChecks.ShouldBeTrue();
        options.MiddlewareOrder.ShouldNotBeNull();
        options.MiddlewareOrder.Length.ShouldBe(11);
        options.CustomMiddleware.ShouldNotBeNull();
        options.CustomMiddleware.ShouldBeEmpty();
        options.CustomMiddlewarePosition.ShouldBe("BeforeEndpoints");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void MiddlewareOrder_HasCorrectDefaultOrder()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions();

        // Assert
        options.MiddlewareOrder[0].ShouldBe("ExceptionHandling");
        options.MiddlewareOrder[1].ShouldBe("SecurityHeaders");
        options.MiddlewareOrder[2].ShouldBe("Cors");
        options.MiddlewareOrder[3].ShouldBe("RequestValidation");
        options.MiddlewareOrder[4].ShouldBe("Authentication");
        options.MiddlewareOrder[5].ShouldBe("Authorization");
        options.MiddlewareOrder[6].ShouldBe("RateLimiting");
        options.MiddlewareOrder[7].ShouldBe("PerformanceMonitoring");
        options.MiddlewareOrder[8].ShouldBe("RequestResponseLogging");
        options.MiddlewareOrder[9].ShouldBe("Endpoints");
        options.MiddlewareOrder[10].ShouldBe("HealthChecks");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EnableRequestValidation_CanBeSet()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions { EnableRequestValidation = false };

        // Assert
        options.EnableRequestValidation.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EnableSecurityHeaders_CanBeSet()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions { EnableSecurityHeaders = false };

        // Assert
        options.EnableSecurityHeaders.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EnableRateLimiting_CanBeSet()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions { EnableRateLimiting = false };

        // Assert
        options.EnableRateLimiting.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EnableAuthentication_CanBeSet()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions { EnableAuthentication = false };

        // Assert
        options.EnableAuthentication.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EnableAuthorization_CanBeSet()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions { EnableAuthorization = false };

        // Assert
        options.EnableAuthorization.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EnableCors_CanBeSet()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions { EnableCors = false };

        // Assert
        options.EnableCors.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EnableExceptionHandling_CanBeSet()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions { EnableExceptionHandling = false };

        // Assert
        options.EnableExceptionHandling.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EnableRequestResponseLogging_CanBeSet()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions { EnableRequestResponseLogging = true };

        // Assert
        options.EnableRequestResponseLogging.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EnablePerformanceMonitoring_CanBeSet()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions { EnablePerformanceMonitoring = false };

        // Assert
        options.EnablePerformanceMonitoring.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EnableHealthChecks_CanBeSet()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions { EnableHealthChecks = false };

        // Assert
        options.EnableHealthChecks.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void MiddlewareOrder_CanBeSet()
    {
        // Arrange
        var customOrder = new[] { "Custom1", "Custom2", "Endpoints" };

        // Act
        var options = new GenericWebMiddlewareOptions { MiddlewareOrder = customOrder };

        // Assert
        options.MiddlewareOrder.ShouldBe(customOrder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CustomMiddleware_CanBeSet()
    {
        // Arrange
        var customTypes = new[] { typeof(string), typeof(int) };

        // Act
        var options = new GenericWebMiddlewareOptions { CustomMiddleware = customTypes };

        // Assert
        options.CustomMiddleware.ShouldBe(customTypes);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CustomMiddlewarePosition_CanBeSet()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions { CustomMiddlewarePosition = "AfterEndpoints" };

        // Assert
        options.CustomMiddlewarePosition.ShouldBe("AfterEndpoints");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange & Act
        var options = new GenericWebMiddlewareOptions
        {
            EnableRequestValidation = false,
            EnableSecurityHeaders = false,
            EnableRateLimiting = false,
            EnableAuthentication = false,
            EnableAuthorization = false,
            EnableCors = false,
            EnableExceptionHandling = false,
            EnableRequestResponseLogging = true,
            EnablePerformanceMonitoring = false,
            EnableHealthChecks = false,
            MiddlewareOrder = ["Test"],
            CustomMiddleware = [typeof(string)],
            CustomMiddlewarePosition = "Custom"
        };

        // Assert
        options.EnableRequestValidation.ShouldBeFalse();
        options.EnableSecurityHeaders.ShouldBeFalse();
        options.EnableRateLimiting.ShouldBeFalse();
        options.EnableAuthentication.ShouldBeFalse();
        options.EnableAuthorization.ShouldBeFalse();
        options.EnableCors.ShouldBeFalse();
        options.EnableExceptionHandling.ShouldBeFalse();
        options.EnableRequestResponseLogging.ShouldBeTrue();
        options.EnablePerformanceMonitoring.ShouldBeFalse();
        options.EnableHealthChecks.ShouldBeFalse();
        options.MiddlewareOrder.ShouldBe(new[] { "Test" });
        options.CustomMiddleware.ShouldBe(new[] { typeof(string) });
        options.CustomMiddlewarePosition.ShouldBe("Custom");
    }
}
