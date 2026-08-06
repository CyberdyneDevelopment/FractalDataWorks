using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Shouldly;
using Fdw.Services.RateLimiting.Handlers;

namespace Fdw.Services.RateLimiting.Tests;

/// <summary>
/// Unit tests for RateLimitRejectionHandler.
/// </summary>
public sealed class RateLimitRejectionHandlerTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task HandleRejectionAsyncSets429StatusCode()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var rejectionContext = CreateRejectionContext(context);

        // Act
        await RateLimitRejectionHandler.HandleRejection(rejectionContext, CancellationToken.None);

        // Assert
        context.Response.StatusCode.ShouldBe((int)HttpStatusCode.TooManyRequests);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task HandleRejectionAsyncSetsRetryAfterHeader()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var rejectionContext = CreateRejectionContext(context);

        // Act
        await RateLimitRejectionHandler.HandleRejection(rejectionContext, CancellationToken.None);

        // Assert
        context.Response.Headers.RetryAfter.ToString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task HandleRejectionAsyncWritesJsonResponse()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var rejectionContext = CreateRejectionContext(context);

        // Act
        await RateLimitRejectionHandler.HandleRejection(rejectionContext, CancellationToken.None);

        // Assert
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        responseBody.ShouldNotBeNullOrEmpty();
        responseBody.ShouldContain("Too Many Requests");
        responseBody.ShouldContain("retryAfterSeconds");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task HandleRejectionAsyncUsesDefaultRetryAfterWhenNoMetadata()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var rejectionContext = CreateRejectionContext(context);

        // Act
        await RateLimitRejectionHandler.HandleRejection(rejectionContext, CancellationToken.None);

        // Assert - default is 60 seconds
        var retryAfterValue = context.Response.Headers.RetryAfter.ToString();
        int.Parse(retryAfterValue).ShouldBe(60);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task HandleRejectionAsyncReturnsValidJsonStructure()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var rejectionContext = CreateRejectionContext(context);

        // Act
        await RateLimitRejectionHandler.HandleRejection(rejectionContext, CancellationToken.None);

        // Assert
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);

        var response = JsonSerializer.Deserialize<RateLimitRejectionResponse>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        response.ShouldNotBeNull();
        response.Error.ShouldBe("Too Many Requests");
        response.Message.ShouldNotBeNullOrEmpty();
        response.RetryAfterSeconds.ShouldBeGreaterThan(0);
    }

    private static DefaultHttpContext CreateMockHttpContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        context.Response.Body = new MemoryStream();

        return context;
    }

    private static OnRejectedContext CreateRejectionContext(HttpContext httpContext)
    {
        // Create a simple lease that always fails
        var lease = new MockRateLimitLease();

        return new OnRejectedContext
        {
            HttpContext = httpContext,
            Lease = lease
        };
    }

    /// <summary>
    /// Mock lease for testing rejection handling.
    /// </summary>
    private sealed class MockRateLimitLease : RateLimitLease
    {
        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames => Array.Empty<string>();

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = default;
            return false;
        }
    }
}
