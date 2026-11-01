using Fdw.Orchestration.Abstractions.Resilience;

namespace Fdw.Orchestration.Abstractions.Tests;

public class ResilienceOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultValuesAreCorrect()
    {
        var sut = new ResilienceOptions();

        sut.MaxRetryAttempts.ShouldBe(3);
        sut.BackoffStrategy.ShouldBeNull();
        sut.ErrorHandlingMode.ShouldBeNull();
        sut.Timeout.ShouldBeNull();
        sut.EnableCircuitBreaker.ShouldBeFalse();
        sut.CircuitBreakerFailureRatio.ShouldBe(0.5);
        sut.CircuitBreakerMinimumThroughput.ShouldBe(10);
        sut.CircuitBreakerBreakDuration.ShouldBe(TimeSpan.FromSeconds(30));
        sut.ShouldRetryOnException.ShouldBeNull();
        sut.OnRetry.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MaxRetryAttemptsCanBeSet()
    {
        var sut = new ResilienceOptions { MaxRetryAttempts = 5 };

        sut.MaxRetryAttempts.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TimeoutCanBeSet()
    {
        var sut = new ResilienceOptions { Timeout = TimeSpan.FromSeconds(60) };

        sut.Timeout.ShouldBe(TimeSpan.FromSeconds(60));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void CircuitBreakerCanBeEnabled()
    {
        var sut = new ResilienceOptions
        {
            EnableCircuitBreaker = true,
            CircuitBreakerFailureRatio = 0.8,
            CircuitBreakerMinimumThroughput = 20,
            CircuitBreakerBreakDuration = TimeSpan.FromMinutes(1)
        };

        sut.EnableCircuitBreaker.ShouldBeTrue();
        sut.CircuitBreakerFailureRatio.ShouldBe(0.8);
        sut.CircuitBreakerMinimumThroughput.ShouldBe(20);
        sut.CircuitBreakerBreakDuration.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ShouldRetryOnExceptionCanBeSet()
    {
        var sut = new ResilienceOptions
        {
            ShouldRetryOnException = ex => ex is TimeoutException
        };

        sut.ShouldRetryOnException.ShouldNotBeNull();
        sut.ShouldRetryOnException!(new TimeoutException()).ShouldBeTrue();
        sut.ShouldRetryOnException!(new InvalidOperationException()).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void OnRetryCallbackCanBeSet()
    {
        var callbackInvoked = false;
        var sut = new ResilienceOptions
        {
            OnRetry = (_, _, _) => callbackInvoked = true
        };

        sut.OnRetry.ShouldNotBeNull();
        sut.OnRetry!(new InvalidOperationException("retry probe"), 1, TimeSpan.Zero);
        callbackInvoked.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void DefaultFactoryReturnsDefaultOptions()
    {
        var sut = ResilienceOptions.Default;

        sut.MaxRetryAttempts.ShouldBe(3);
        sut.EnableCircuitBreaker.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NoRetryFactoryReturnsZeroAttempts()
    {
        var sut = ResilienceOptions.NoRetry;

        sut.MaxRetryAttempts.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ExponentialBackoffFactorySetsMaxAttempts()
    {
        var sut = ResilienceOptions.ExponentialBackoff(5);

        sut.MaxRetryAttempts.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ExponentialBackoffDefaultMaxAttempts()
    {
        var sut = ResilienceOptions.ExponentialBackoff();

        sut.MaxRetryAttempts.ShouldBe(3);
    }
}
