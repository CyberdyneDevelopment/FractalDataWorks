using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Shouldly;
using Polly;
using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Resiliency.Extensions;

namespace Fdw.Services.Resiliency.Tests;

/// <summary>
/// Unit tests for ResiliencyPipelineExtensions.
/// </summary>
[Collection(nameof(ResiliencyTestCollection))]
public sealed class ResiliencyPipelineExtensionsTests
{
    private readonly Mock<ILogger> _loggerMock;

    // Force TypeCollection initialization before any parallel tests run
    // This prevents race conditions in the source-generated EnsureFrozen() method
    static ResiliencyPipelineExtensionsTests()
    {
        _ = ResiliencyPolicies.All();
    }

    public ResiliencyPipelineExtensionsTests()
    {
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CreatePipelineReturnsPipelineForValidPolicy()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Database");

        // Act
        var pipeline = policy.Create();

        // Assert
        pipeline.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CreatePipelineWithLoggerReturnsPipeline()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Database");

        // Act
        var pipeline = policy.Create(_loggerMock.Object);

        // Assert
        pipeline.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CreatePipelineWithOperationNameReturnsPipeline()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Database");

        // Act
        var pipeline = policy.Create(_loggerMock.Object, "TestOperation");

        // Assert
        pipeline.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CreatePipelineThrowsForNullPolicy()
    {
        // Arrange
        IResiliencyPolicy? policy = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => policy!.Create());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CreateGenericPipelineReturnsPipelineForValidPolicy()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("HttpClient");

        // Act
        var pipeline = policy.CreatePipeline<string>();

        // Assert
        pipeline.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CreateGenericPipelineWithLoggerReturnsPipeline()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("HttpClient");

        // Act
        var pipeline = policy.CreatePipeline<int>(_loggerMock.Object);

        // Assert
        pipeline.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CreateGenericPipelineThrowsForNullPolicy()
    {
        // Arrange
        IResiliencyPolicy? policy = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => policy!.CreatePipeline<string>());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteReturnsSuccessForSuccessfulOperation()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Simple");

        // Act
        var result = await policy.Execute<string>(
            _ => Task.FromResult("success"),
            _loggerMock.Object,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("success");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteWithoutResultReturnsSuccessForSuccessfulOperation()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Simple");
        var executed = false;

        // Act
        var result = await policy.Execute(
            _ =>
            {
                executed = true;
                return Task.CompletedTask;
            },
            _loggerMock.Object,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        executed.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteThrowsForNullPolicy()
    {
        // Arrange
        IResiliencyPolicy? policy = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await policy!.Execute<string>(_ => Task.FromResult("test")));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteThrowsForNullOperation()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Simple");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await policy.Execute<string>(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteWithoutResultThrowsForNullPolicy()
    {
        // Arrange
        IResiliencyPolicy? policy = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await policy!.Execute(_ => Task.CompletedTask));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteWithoutResultThrowsForNullOperation()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Simple");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await policy.Execute((Func<CancellationToken, Task>)null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteReturnsFailureForFailingOperation()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Simple");
        var attemptCount = 0;

        // Act
        var result = await policy.Execute<string>(
            _ =>
            {
                attemptCount++;
                throw new InvalidOperationException("Test failure");
            },
            _loggerMock.Object,
            "TestOperation",
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
        // The operation should have been retried based on the Simple policy
        attemptCount.ShouldBeGreaterThan(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteRespectsCustomOperationName()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Simple");
        var customOperationName = "CustomTestOperation";

        // Act
        var result = await policy.Execute<string>(
            _ => Task.FromResult("success"),
            _loggerMock.Object,
            customOperationName,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteReturnsFailureOnCancellation()
    {
        // Arrange
        var policy = ResiliencyPolicies.ByName("Simple");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var result = await policy.Execute<string>(
            async ct =>
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(100, ct);
                return "test";
            },
            _loggerMock.Object,
            cancellationToken: cts.Token);

        // Assert - when cancellation is requested before execution, it returns failure
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DifferentPoliciesCreateDifferentPipelines()
    {
        // Arrange
        var databasePolicy = ResiliencyPolicies.ByName("Database");
        var httpPolicy = ResiliencyPolicies.ByName("HttpClient");

        // Act
        var databasePipeline = databasePolicy.Create();
        var httpPipeline = httpPolicy.Create();

        // Assert
        databasePipeline.ShouldNotBeNull();
        httpPipeline.ShouldNotBeNull();
        ReferenceEquals(databasePipeline, httpPipeline).ShouldBeFalse();
    }
}
