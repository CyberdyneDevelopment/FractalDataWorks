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

        // Assert
        type.Id.ShouldBe(2);
        type.Name.ShouldBe("PollyRetry");
        type.DisplayName.ShouldBe("Polly Retry");
        type.Description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteThrowsArgumentNullExceptionWhenRunStageIsNull()
    {
        // Arrange
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration();
        var ctx = new FakeResiliencyExecutionContext();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => type.Execute(null!, config, ctx, TestContext.Current.CancellationToken));
        ex.ParamName.ShouldBe("runStage");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteThrowsArgumentNullExceptionWhenConfigIsNull()
    {
        // Arrange
        var type = new PollyRetryResiliencyType();
        var ctx = new FakeResiliencyExecutionContext();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => type.Execute(AlwaysSucceeds(), null!, ctx, TestContext.Current.CancellationToken));
        ex.ParamName.ShouldBe("config");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteThrowsArgumentNullExceptionWhenContextIsNull()
    {
        // Arrange
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            () => type.Execute(AlwaysSucceeds(), config, null!, TestContext.Current.CancellationToken));
        ex.ParamName.ShouldBe("ctx");
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
        var config = new PollyRetryResiliencyConfiguration { MaxRetries = 3, BaseDelayMs = 1, MaxDelayMs = 5 };
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
    [InlineData("SomeUnknownKind")]
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
        var config = new PollyRetryResiliencyConfiguration { MaxRetries = 2, BaseDelayMs = 1, MaxDelayMs = 5 };
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
        var config = new PollyRetryResiliencyConfiguration { MaxRetries = 3, BaseDelayMs = 1, MaxDelayMs = 5 };
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
        var config = new PollyRetryResiliencyConfiguration { MaxRetries = 1, BaseDelayMs = 1, MaxDelayMs = 5 };
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
        // Why (DEFECT): PollyRetryResiliencyType.Execute only catches OperationCanceledException and
        // ResiliencyRetryException. Polly.Timeout.TimeoutRejectedException derives from
        // Polly.ExecutionRejectedException (plain Exception), NOT OperationCanceledException, so a
        // per-attempt timeout escapes Execute as an unhandled exception instead of the documented
        // IGenericResult failure contract. This test characterizes the CURRENT (defective) behavior;
        // it should start failing (in a good way) once Execute gains a catch for TimeoutRejectedException
        // that converts it into a GenericResult.Failure.
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration
        {
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
        // Why (DEFECT): BuildPipeline(pollyConfig) is called BEFORE the try/catch block in Execute, and
        // Polly's RetryStrategyOptions.MaxRetryAttempts carries a [Range(1, int.MaxValue)] validation
        // attribute. MaxRetries = 0 is a plausible "no retry, just run once" configuration, but it makes
        // pipeline.Build() throw a raw System.ComponentModel.DataAnnotations.ValidationException that is
        // never caught, violating both "fail loud via IGenericResult" and the IResiliencyType.Execute
        // contract (Task<IGenericResult>, not an exception, for a config-driven failure).
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration { MaxRetries = 0 };
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
        // Why (DEFECT): PollyRetryResiliencyConfiguration.CircuitBreakerThreshold is documented as
        // "the circuit opens after this many consecutive failures", but PollyRetryResiliencyType.BuildPipeline
        // never calls builder.AddCircuitBreaker(...) — the setting is inert. This test proves the stage still
        // runs MaxRetries+1 times even though CircuitBreakerThreshold is set to 1, i.e. no circuit ever opens.
        var type = new PollyRetryResiliencyType();
        var config = new PollyRetryResiliencyConfiguration
        {
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
}
