using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Resiliency.Polly.Tests.TestDoubles;
using Polly.Timeout;

namespace Fdw.Services.Resiliency.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="PollyRetryResiliencyType"/>.
/// </summary>
public sealed class PollyRetryResiliencyTypeTests
{
    private static Func<CancellationToken, Task<IGenericResult>> AlwaysSucceeds() =>
        _ => Task.FromResult(GenericResult.Success());

    private static Func<CancellationToken, Task<IGenericResult>> AlwaysFails(string message) =>
        _ => Task.FromResult(GenericResult.Failure(new GenericMessage(message)));

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsIdentityMetadata()
    {
        // Arrange & Act
        var type = new PollyRetryResiliencyType();
        type.Name.ShouldBe("PollyRetry");
        type.DisplayName.ShouldBe("Polly Retry");
        type.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteReturnsFailureWhenConfigurationIsWrongType()
    {
        // Arrange
        var type = new PollyRetryResiliencyType();
        var wrongConfig = new FakeGenericConfiguration();
        var ctx = new FakeResiliencyExecutionContext();

        // Act
        var result = await type.Execute(AlwaysSucceeds(), wrongConfig, ctx, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("wrong configuration type");
        result.CurrentMessage.ShouldContain(nameof(FakeGenericConfiguration));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteReturnsSuccessWhenStageSucceedsOnFirstAttempt()
    {
        // Arrange
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration { BackoffKind = "Exponential", MaxRetries = 3, BaseDelayMs = 1, MaxDelayMs = 5 };
        var ctx = new FakeResiliencyExecutionContext();
        var callCount = 0;
        Func<CancellationToken, Task<IGenericResult>> runStage = _ =>
        {
            callCount++;
            return Task.FromResult(GenericResult.Success());
        };

        // Act
        var result = await type.Execute(runStage, config, ctx, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        callCount.ShouldBe(1);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("Exponential")]
    [InlineData("exponential")]
    [InlineData("Fixed")]
    [InlineData("FIXED")]
    [InlineData("Random")]
    [InlineData("random")]
    public async Task ExecuteRetriesUntilStageSucceeds(string backoffKind)
    {
        // Arrange
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration
        {
            MaxRetries = 3,
            BaseDelayMs = 1,
            MaxDelayMs = 5,
            BackoffKind = backoffKind,
            JitterPercent = 10
        };
        var ctx = new FakeResiliencyExecutionContext();
        var callCount = 0;
        Func<CancellationToken, Task<IGenericResult>> runStage = _ =>
        {
            callCount++;
            return Task.FromResult(callCount < 3
                ? GenericResult.Failure(new GenericMessage("transient failure"))
                : GenericResult.Success());
        };

        // Act
        var result = await type.Execute(runStage, config, ctx, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        callCount.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteReturnsFailureWhenRetriesExhausted()
    {
        // Arrange
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration { BackoffKind = "Exponential", MaxRetries = 2, BaseDelayMs = 1, MaxDelayMs = 5 };
        var ctx = new FakeResiliencyExecutionContext();
        var callCount = 0;
        Func<CancellationToken, Task<IGenericResult>> runStage = _ =>
        {
            callCount++;
            return Task.FromResult(GenericResult.Failure(new GenericMessage("boom")));
        };

        // Act
        var result = await type.Execute(runStage, config, ctx, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        callCount.ShouldBe(3); // initial attempt + 2 retries
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("boom");
        result.CurrentMessage.ShouldContain(ctx.ExecutionId.ToString());
        result.CurrentMessage.ShouldContain("2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecutePropagatesCancellationWithoutWrapping()
    {
        // Arrange
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration { BackoffKind = "Exponential", MaxRetries = 3, BaseDelayMs = 1, MaxDelayMs = 5 };
        var ctx = new FakeResiliencyExecutionContext();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Func<CancellationToken, Task<IGenericResult>> runStage = ct =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(GenericResult.Success());
        };

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => type.Execute(runStage, config, ctx, cts.Token));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteUsesLoggerFromContextWhenAvailable()
    {
        // Arrange
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration { BackoffKind = "Exponential", MaxRetries = 1, BaseDelayMs = 1, MaxDelayMs = 5 };
        var ctx = new FakeLoggerProviderExecutionContext();

        // Act
        var result = await type.Execute(AlwaysFails("boom"), config, ctx, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        ctx.CreateLoggerCalled.ShouldBeTrue();
        ctx.LastCategoryName.ShouldBe(nameof(PollyRetryResiliencyType));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteAppliesPerAttemptTimeoutWhenConfigured()
    {
        // Arrange
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration
        { BackoffKind = "Exponential", BaseDelayMs = 1, MaxDelayMs = 5,
            MaxRetries = 1,
            TimeoutSeconds = 1,
        };
        var ctx = new FakeResiliencyExecutionContext();
        Func<CancellationToken, Task<IGenericResult>> runStage = async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
            return GenericResult.Success();
        };

        // Act & Assert
        await Should.ThrowAsync<TimeoutRejectedException>(
            () => type.Execute(runStage, config, ctx, TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteThrowsValidationExceptionWhenMaxRetriesIsZero()
    {
        // Arrange
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration { BackoffKind = "Exponential", BaseDelayMs = 1, MaxDelayMs = 5, MaxRetries = 0 };
        var ctx = new FakeResiliencyExecutionContext();

        // Act & Assert
        await Should.ThrowAsync<System.ComponentModel.DataAnnotations.ValidationException>(
            () => type.Execute(AlwaysSucceeds(), config, ctx, TestContext.Current.CancellationToken));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteIgnoresCircuitBreakerThresholdConfiguration()
    {
        // Arrange
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration
        { BackoffKind = "Exponential",
            MaxRetries = 2,
            BaseDelayMs = 1,
            MaxDelayMs = 5,
            CircuitBreakerThreshold = 1,
        };
        var ctx = new FakeResiliencyExecutionContext();
        var callCount = 0;
        Func<CancellationToken, Task<IGenericResult>> runStage = _ =>
        {
            callCount++;
            return Task.FromResult(GenericResult.Failure(new GenericMessage("boom")));
        };

        // Act
        var result = await type.Execute(runStage, config, ctx, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        callCount.ShouldBe(3); // initial attempt + 2 retries; a wired circuit breaker would have stopped short
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    [InlineData("MaxRetries")]
    [InlineData("BackoffKind")]
    [InlineData("BaseDelayMs")]
    [InlineData("MaxDelayMs")]
    public async Task ExecuteRefusesAPolicyThatLeavesOutAValue(string field)
    {
        // Arrange: whoever defines the policy states every value; nothing is assumed (FDW-3931).
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration { Name = "Strict", MaxRetries = 1, BackoffKind = "Fixed", BaseDelayMs = 1, MaxDelayMs = 5 };
        switch (field)
        {
            case "MaxRetries": config.MaxRetries = null; break;
            case "BackoffKind": config.BackoffKind = null; break;
            case "BaseDelayMs": config.BaseDelayMs = null; break;
            default: config.MaxDelayMs = null; break;
        }

        var calls = 0;
        Func<CancellationToken, Task<IGenericResult>> runStage = _ =>
        {
            calls++;
            return Task.FromResult(GenericResult.Success());
        };

        // Act
        var result = await type.Execute(runStage, config, new FakeResiliencyExecutionContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(field);
        calls.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteRefusesABackoffKindThatIsNotOne()
    {
        // Arrange: an unknown kind was read as Exponential before FDW-3931.
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration { Name = "Odd", MaxRetries = 1, BackoffKind = "SomeUnknownKind", BaseDelayMs = 1, MaxDelayMs = 5 };

        // Act
        var result = await type.Execute(AlwaysSucceeds(), config, new FakeResiliencyExecutionContext(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("SomeUnknownKind");
    }
}
