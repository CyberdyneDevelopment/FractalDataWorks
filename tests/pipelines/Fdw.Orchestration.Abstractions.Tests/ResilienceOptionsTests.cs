using Fdw.Orchestration.Abstractions.Resilience;

namespace Fdw.Orchestration.Abstractions.Tests;

public class ResilienceOptionsTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NothingIsAssumedUntilTheBuilderStatesIt()
    {
        // FDW-3931: a resilience policy's values are its definer's to state; none carries a value in code.
        var sut = new ResilienceOptions();

        sut.MaxRetryAttempts.ShouldBe(0);
        sut.BackoffStrategy.ShouldBeNull();
        sut.ErrorHandlingMode.ShouldBeNull();
        sut.Timeout.ShouldBeNull();
        sut.CircuitBreaker.ShouldBeNull();
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
            CircuitBreaker = new CircuitBreakerOptions
            {
                FailureRatio = 0.8,
                MinimumThroughput = 20,
                BreakDuration = TimeSpan.FromMinutes(1),
                SamplingDuration = TimeSpan.FromSeconds(45),
            },
        };

        sut.CircuitBreaker!.FailureRatio.ShouldBe(0.8);
        sut.CircuitBreaker.MinimumThroughput.ShouldBe(20);
        sut.CircuitBreaker.BreakDuration.ShouldBe(TimeSpan.FromMinutes(1));
        sut.CircuitBreaker.SamplingDuration.ShouldBe(TimeSpan.FromSeconds(45));
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
}
