using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Fdw.Web.RestEndpoints.Configuration;
using Fdw.Web.RestEndpoints.Extensions;

namespace Fdw.Web.RestEndpoints.Tests.Extensions;

public class ApplicationBuilderExtensionsTests
{
    private readonly IApplicationBuilder _appBuilder;

    public ApplicationBuilderExtensionsTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.Configure<SecurityConfiguration>(_ => { });
        services.Configure<WebConfiguration>(_ => { });
        services.AddAuthentication();
        services.AddAuthorizationBuilder();
        services.AddCors();
        services.AddRouting();
        services.AddHealthChecks();
        services.AddRateLimiter(_ => { });

        var serviceProvider = services.BuildServiceProvider();
        _appBuilder = new ApplicationBuilder(serviceProvider);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksWeb_ReturnsApplicationBuilder()
    {
        // Act - disable endpoints since ApplicationBuilder is not IEndpointRouteBuilder
        var result = _appBuilder.UseFractalDataWorksWeb(options =>
        {
            options.MiddlewareOrder = ["ExceptionHandling", "SecurityHeaders", "Cors",
                "RequestValidation", "Authentication", "Authorization",
                "RateLimiting", "PerformanceMonitoring", "RequestResponseLogging", "HealthChecks"];
        });

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksWeb_WithOptions_ReturnsApplicationBuilder()
    {
        // Act
        var result = _appBuilder.UseFractalDataWorksWeb(options =>
        {
            options.EnableCors = false;
            options.MiddlewareOrder = ["ExceptionHandling", "SecurityHeaders",
                "RequestValidation", "Authentication", "Authorization",
                "RateLimiting", "PerformanceMonitoring", "RequestResponseLogging", "HealthChecks"];
        });

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksWeb_WithNullOptions_ReturnsApplicationBuilder()
    {
        // Arrange - null options uses defaults which include Endpoints,
        // and ApplicationBuilder is not IEndpointRouteBuilder so this will throw
        // Act & Assert
        Should.Throw<InvalidCastException>(() => _appBuilder.UseFractalDataWorksWeb(null));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksRequestValidation_ReturnsApplicationBuilder()
    {
        // Act
        var result = _appBuilder.UseFractalDataWorksRequestValidation();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksEndpoints_ThrowsWithoutEndpointRouteBuilder()
    {
        // ApplicationBuilder does not implement IEndpointRouteBuilder,
        // which is required by UseFastEndpoints
        Should.Throw<InvalidCastException>(() => _appBuilder.UseFractalDataWorksEndpoints());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksSecurityHeaders_ReturnsApplicationBuilder()
    {
        // Act
        var result = _appBuilder.UseFractalDataWorksSecurityHeaders();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksRateLimiting_ReturnsApplicationBuilder()
    {
        // Act
        var result = _appBuilder.UseFractalDataWorksRateLimiting();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksAuthentication_ReturnsApplicationBuilder()
    {
        // Act
        var result = _appBuilder.UseFractalDataWorksAuthentication();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksAuthorization_ReturnsApplicationBuilder()
    {
        // Act
        var result = _appBuilder.UseFractalDataWorksAuthorization();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksCors_ReturnsApplicationBuilder()
    {
        // Act
        var result = _appBuilder.UseFractalDataWorksCors();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksExceptionHandling_ReturnsApplicationBuilder()
    {
        // Act
        var result = _appBuilder.UseFractalDataWorksExceptionHandling();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksRequestResponseLogging_ReturnsApplicationBuilder()
    {
        // Act
        var result = _appBuilder.UseFractalDataWorksRequestResponseLogging();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksPerformanceMonitoring_ReturnsApplicationBuilder()
    {
        // Act
        var result = _appBuilder.UseFractalDataWorksPerformanceMonitoring();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void UseFractalDataWorksHealthChecks_ReturnsApplicationBuilder()
    {
        // Act
        var result = _appBuilder.UseFractalDataWorksHealthChecks();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateFractalDataWorksWebConfiguration_ReturnsApplicationBuilder()
    {
        // Act
        var result = _appBuilder.ValidateFractalDataWorksWebConfiguration();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExtensionMethods_CanBeChained()
    {
        // Act - exclude Endpoints since ApplicationBuilder is not IEndpointRouteBuilder
        var result = _appBuilder
            .UseFractalDataWorksExceptionHandling()
            .UseFractalDataWorksSecurityHeaders()
            .UseFractalDataWorksCors()
            .UseFractalDataWorksAuthentication()
            .UseFractalDataWorksAuthorization()
            .UseFractalDataWorksRateLimiting();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(_appBuilder);
    }
}
