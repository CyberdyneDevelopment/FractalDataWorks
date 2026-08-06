namespace Fdw.Services.Resiliency.Polly.Tests;

/// <summary>
/// Unit tests for <see cref="PollyRetryResiliencyConfiguration"/>.
/// </summary>
public sealed class PollyRetryResiliencyConfigurationTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void SectionNameReturnsResiliencyPollyRetry()
    {
        // Arrange
        var config = new PollyRetryResiliencyConfiguration();

        // Act
        var sectionName = config.SectionName;

        // Assert
        sectionName.ShouldBe("Resiliency:PollyRetry");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void StrategyTypeReturnsPollyRetry()
    {
        // Arrange
        var config = new PollyRetryResiliencyConfiguration();

        // Act
        var strategyType = config.StrategyType;

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

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Configuration")]
    public void InheritedGenericConfigurationPropertiesAreSettable()
    {
        // Arrange
        var config = new PollyRetryResiliencyConfiguration();
        var id = System.Guid.NewGuid();
        var tenantId = System.Guid.NewGuid();

        // Act
        config.Id = id;
        config.Name = "MyPolicy";
        config.ServiceOptionType = "Custom";
        config.Description = "A description";
        config.TenantId = tenantId;

        // Assert
        config.Id.ShouldBe(id);
        config.Name.ShouldBe("MyPolicy");
        config.ServiceOptionType.ShouldBe("Custom");
        config.Description.ShouldBe("A description");
        config.TenantId.ShouldBe(tenantId);
        config.ServiceType.ShouldBe("Resiliency");
    }
}
