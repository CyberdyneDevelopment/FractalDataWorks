namespace Fdw.Services.Resiliency.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="PollyRetryResiliencyConfiguration"/>.
/// </summary>
public sealed class PollyRetryResiliencyConfigurationTests
{

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ImplementationReturnsPollyRetry()
    {
        // Arrange
        var config = new PollyRetryResiliencyConfiguration();

        // Act
        var strategyType = config.Implementation;

        // Assert
        strategyType.ShouldBe("PollyRetry");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void DefaultsMatchDocumentedValues()
    {
        // Arrange & Act
        var config = new PollyRetryResiliencyConfiguration();

        // Assert
        config.MaxRetries.ShouldBe(3);
        config.BackoffKind.ShouldBe("Exponential");
        config.BaseDelayMs.ShouldBe(1000);
        config.MaxDelayMs.ShouldBe(30000);
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
