namespace Fdw.Services.Resiliency.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="PollyRetryResiliencyConfiguration"/>.
/// </summary>
public sealed class PollyRetryResiliencyConfigurationTests
{

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void APolicyHasNoValueUntilItsDefinerStatesOne()
    {
        // Arrange & Act: every value of a policy is its definer's to state (FDW-3931).
        var config = new PollyRetryResiliencyConfiguration();

        // Assert
        config.MaxRetries.ShouldBeNull();
        config.BackoffKind.ShouldBeNull();
        config.BaseDelayMs.ShouldBeNull();
        config.MaxDelayMs.ShouldBeNull();
        config.JitterPercent.ShouldBeNull();
        config.CircuitBreakerThreshold.ShouldBeNull();
        config.TimeoutSeconds.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void PropertiesRoundTripAssignedValues()
    {
        // Arrange
        var config = new PollyRetryResiliencyConfiguration();

        // Act
        config.MaxRetries = 7;
        config.BackoffKind = "Fixed";
        config.BaseDelayMs = 250;
        config.MaxDelayMs = 5000;
        config.JitterPercent = 25;
        config.CircuitBreakerThreshold = 5;
        config.TimeoutSeconds = 30;

        // Assert
        config.MaxRetries.ShouldBe(7);
        config.BackoffKind.ShouldBe("Fixed");
        config.BaseDelayMs.ShouldBe(250);
        config.MaxDelayMs.ShouldBe(5000);
        config.JitterPercent.ShouldBe(25);
        config.CircuitBreakerThreshold.ShouldBe(5);
        config.TimeoutSeconds.ShouldBe(30);
    }

}
