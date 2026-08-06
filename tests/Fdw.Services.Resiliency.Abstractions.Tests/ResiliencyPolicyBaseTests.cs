using Fdw.Services.Resiliency.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Resiliency.Abstractions.Tests;

/// <summary>
/// Tests for ResiliencyPolicyBase.
/// </summary>
public class ResiliencyPolicyBaseTests
{
    /// <summary>
    /// Testable implementation of ResiliencyPolicyBase for testing abstract class behavior.
    /// </summary>
    private sealed class TestResiliencyPolicy : ResiliencyPolicyBase
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _maxDelay;
        private readonly double _backoffMultiplier;
        private readonly TimeSpan _circuitBreakerDuration;
        private readonly int _circuitBreakerThreshold;
        private readonly IResiliencyCategory _category;

        public TestResiliencyPolicy(
            int id,
            string name,
            int maxRetries = 3,
            TimeSpan? initialDelay = null,
            TimeSpan? maxDelay = null,
            double backoffMultiplier = 2.0,
            TimeSpan? circuitBreakerDuration = null,
            int circuitBreakerThreshold = 5,
            IResiliencyCategory? category = null)
            : base(id, name)
        {
            _maxRetries = maxRetries;
            _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(100);
            _maxDelay = maxDelay ?? TimeSpan.FromSeconds(5);
            _backoffMultiplier = backoffMultiplier;
            _circuitBreakerDuration = circuitBreakerDuration ?? TimeSpan.FromSeconds(30);
            _circuitBreakerThreshold = circuitBreakerThreshold;
            _category = category ?? ResiliencyCategories.Database;
        }

        public override int MaxRetries => _maxRetries;
        public override TimeSpan InitialDelay => _initialDelay;
        public override TimeSpan MaxDelay => _maxDelay;
        public override double BackoffMultiplier => _backoffMultiplier;
        public override TimeSpan CircuitBreakerDuration => _circuitBreakerDuration;
        public override int CircuitBreakerThreshold => _circuitBreakerThreshold;
        public override IResiliencyCategory ResiliencyCategory => _category;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsId()
    {
        // Act
        var policy = new TestResiliencyPolicy(42, "Test");

        // Assert
        policy.Id.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsName()
    {
        // Act
        var policy = new TestResiliencyPolicy(1, "TestPolicy");

        // Assert
        policy.Name.ShouldBe("TestPolicy");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MaxRetriesReturnsConfiguredValue()
    {
        // Arrange
        var policy = new TestResiliencyPolicy(1, "Test", maxRetries: 7);

        // Act
        var maxRetries = policy.MaxRetries;

        // Assert
        maxRetries.ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InitialDelayReturnsConfiguredValue()
    {
        // Arrange
        var expectedDelay = TimeSpan.FromMilliseconds(250);
        var policy = new TestResiliencyPolicy(1, "Test", initialDelay: expectedDelay);

        // Act
        var delay = policy.InitialDelay;

        // Assert
        delay.ShouldBe(expectedDelay);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MaxDelayReturnsConfiguredValue()
    {
        // Arrange
        var expectedDelay = TimeSpan.FromSeconds(10);
        var policy = new TestResiliencyPolicy(1, "Test", maxDelay: expectedDelay);

        // Act
        var delay = policy.MaxDelay;

        // Assert
        delay.ShouldBe(expectedDelay);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BackoffMultiplierReturnsConfiguredValue()
    {
        // Arrange
        var policy = new TestResiliencyPolicy(1, "Test", backoffMultiplier: 1.5);

        // Act
        var multiplier = policy.BackoffMultiplier;

        // Assert
        multiplier.ShouldBe(1.5);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CircuitBreakerDurationReturnsConfiguredValue()
    {
        // Arrange
        var expectedDuration = TimeSpan.FromMinutes(2);
        var policy = new TestResiliencyPolicy(1, "Test", circuitBreakerDuration: expectedDuration);

        // Act
        var duration = policy.CircuitBreakerDuration;

        // Assert
        duration.ShouldBe(expectedDuration);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CircuitBreakerThresholdReturnsConfiguredValue()
    {
        // Arrange
        var policy = new TestResiliencyPolicy(1, "Test", circuitBreakerThreshold: 15);

        // Act
        var threshold = policy.CircuitBreakerThreshold;

        // Assert
        threshold.ShouldBe(15);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ResiliencyCategoryReturnsConfiguredValue()
    {
        // Arrange
        var policy = new TestResiliencyPolicy(1, "Test", category: ResiliencyCategories.Critical);

        // Act
        var category = policy.ResiliencyCategory;

        // Assert
        category.ShouldBe(ResiliencyCategories.Critical);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIResiliencyPolicy()
    {
        // Arrange
        var policy = new TestResiliencyPolicy(1, "Test");

        // Assert
        policy.ShouldBeAssignableTo<IResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void IsAbstract()
    {
        // Arrange
        var type = typeof(ResiliencyPolicyBase);

        // Assert
        type.IsAbstract.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CanBeInheritedByConcreteClass()
    {
        // Act
        var policy = new TestResiliencyPolicy(1, "Test");

        // Assert
        policy.ShouldBeAssignableTo<ResiliencyPolicyBase>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void PropertiesAreAbstract()
    {
        // Arrange
        var type = typeof(ResiliencyPolicyBase);

        // Act
        var maxRetriesProperty = type.GetProperty(nameof(IResiliencyPolicy.MaxRetries));
        var initialDelayProperty = type.GetProperty(nameof(IResiliencyPolicy.InitialDelay));
        var maxDelayProperty = type.GetProperty(nameof(IResiliencyPolicy.MaxDelay));
        var backoffProperty = type.GetProperty(nameof(IResiliencyPolicy.BackoffMultiplier));
        var cbDurationProperty = type.GetProperty(nameof(IResiliencyPolicy.CircuitBreakerDuration));
        var cbThresholdProperty = type.GetProperty(nameof(IResiliencyPolicy.CircuitBreakerThreshold));
        var categoryProperty = type.GetProperty(nameof(IResiliencyPolicy.ResiliencyCategory));

        // Assert
        maxRetriesProperty.ShouldNotBeNull();
        maxRetriesProperty!.GetGetMethod()!.IsAbstract.ShouldBeTrue();

        initialDelayProperty.ShouldNotBeNull();
        initialDelayProperty!.GetGetMethod()!.IsAbstract.ShouldBeTrue();

        maxDelayProperty.ShouldNotBeNull();
        maxDelayProperty!.GetGetMethod()!.IsAbstract.ShouldBeTrue();

        backoffProperty.ShouldNotBeNull();
        backoffProperty!.GetGetMethod()!.IsAbstract.ShouldBeTrue();

        cbDurationProperty.ShouldNotBeNull();
        cbDurationProperty!.GetGetMethod()!.IsAbstract.ShouldBeTrue();

        cbThresholdProperty.ShouldNotBeNull();
        cbThresholdProperty!.GetGetMethod()!.IsAbstract.ShouldBeTrue();

        categoryProperty.ShouldNotBeNull();
        categoryProperty!.GetGetMethod()!.IsAbstract.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SupportsZeroMaxRetries()
    {
        // Arrange
        var policy = new TestResiliencyPolicy(1, "Test", maxRetries: 0);

        // Assert
        policy.MaxRetries.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SupportsZeroInitialDelay()
    {
        // Arrange
        var policy = new TestResiliencyPolicy(1, "Test", initialDelay: TimeSpan.Zero);

        // Assert
        policy.InitialDelay.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SupportsZeroCircuitBreakerThreshold()
    {
        // Arrange
        var policy = new TestResiliencyPolicy(1, "Test", circuitBreakerThreshold: 0);

        // Assert
        policy.CircuitBreakerThreshold.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SupportsLargeMaxRetries()
    {
        // Arrange
        var policy = new TestResiliencyPolicy(1, "Test", maxRetries: 100);

        // Assert
        policy.MaxRetries.ShouldBe(100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SupportsLargeBackoffMultiplier()
    {
        // Arrange
        var policy = new TestResiliencyPolicy(1, "Test", backoffMultiplier: 10.0);

        // Assert
        policy.BackoffMultiplier.ShouldBe(10.0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SupportsSmallBackoffMultiplier()
    {
        // Arrange
        var policy = new TestResiliencyPolicy(1, "Test", backoffMultiplier: 1.1);

        // Assert
        policy.BackoffMultiplier.ShouldBe(1.1);
    }
}
