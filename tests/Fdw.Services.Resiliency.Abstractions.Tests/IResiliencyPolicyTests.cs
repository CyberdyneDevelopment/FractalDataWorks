using Fdw.Collections;
using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Resiliency.Abstractions.Policies;
using Shouldly;
using Xunit;

namespace Fdw.Services.Resiliency.Abstractions.Tests;

/// <summary>
/// Tests for IResiliencyPolicy interface.
/// </summary>
public class IResiliencyPolicyTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfaceInheritsFromITypeOption()
    {
        // Arrange
        var interfaceType = typeof(IResiliencyPolicy);

        // Act
        var isTypeOption = typeof(ITypeOption<int, IResiliencyPolicy>).IsAssignableFrom(interfaceType);

        // Assert
        isTypeOption.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfaceDefinesMaxRetriesProperty()
    {
        // Arrange
        var interfaceType = typeof(IResiliencyPolicy);

        // Act
        var property = interfaceType.GetProperty(nameof(IResiliencyPolicy.MaxRetries));

        // Assert
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(int));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfaceDefinesInitialDelayProperty()
    {
        // Arrange
        var interfaceType = typeof(IResiliencyPolicy);

        // Act
        var property = interfaceType.GetProperty(nameof(IResiliencyPolicy.InitialDelay));

        // Assert
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(TimeSpan));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfaceDefinesMaxDelayProperty()
    {
        // Arrange
        var interfaceType = typeof(IResiliencyPolicy);

        // Act
        var property = interfaceType.GetProperty(nameof(IResiliencyPolicy.MaxDelay));

        // Assert
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(TimeSpan));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfaceDefinesBackoffMultiplierProperty()
    {
        // Arrange
        var interfaceType = typeof(IResiliencyPolicy);

        // Act
        var property = interfaceType.GetProperty(nameof(IResiliencyPolicy.BackoffMultiplier));

        // Assert
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(double));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfaceDefinesCircuitBreakerDurationProperty()
    {
        // Arrange
        var interfaceType = typeof(IResiliencyPolicy);

        // Act
        var property = interfaceType.GetProperty(nameof(IResiliencyPolicy.CircuitBreakerDuration));

        // Assert
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(TimeSpan));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfaceDefinesCircuitBreakerThresholdProperty()
    {
        // Arrange
        var interfaceType = typeof(IResiliencyPolicy);

        // Act
        var property = interfaceType.GetProperty(nameof(IResiliencyPolicy.CircuitBreakerThreshold));

        // Assert
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(int));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfaceDefinesResiliencyCategoryProperty()
    {
        // Arrange
        var interfaceType = typeof(IResiliencyPolicy);

        // Act
        var property = interfaceType.GetProperty(nameof(IResiliencyPolicy.ResiliencyCategory));

        // Assert
        property.ShouldNotBeNull();
        property!.PropertyType.ShouldBe(typeof(IResiliencyCategory));
        property.CanRead.ShouldBeTrue();
        property.CanWrite.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfaceIsPublic()
    {
        // Arrange
        var interfaceType = typeof(IResiliencyPolicy);

        // Assert
        interfaceType.IsPublic.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfaceIsInterface()
    {
        // Arrange
        var interfaceType = typeof(IResiliencyPolicy);

        // Assert
        interfaceType.IsInterface.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DatabasePolicyImplementsInterface()
    {
        // Arrange
        var policy = new DatabaseResiliencyPolicy();

        // Assert
        policy.ShouldBeAssignableTo<IResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void HttpClientPolicyImplementsInterface()
    {
        // Arrange
        var policy = new HttpClientResiliencyPolicy();

        // Assert
        policy.ShouldBeAssignableTo<IResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CriticalPolicyImplementsInterface()
    {
        // Arrange
        var policy = new CriticalResiliencyPolicy();

        // Assert
        policy.ShouldBeAssignableTo<IResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SimplePolicyImplementsInterface()
    {
        // Arrange
        var policy = new SimpleRetryResiliencyPolicy();

        // Assert
        policy.ShouldBeAssignableTo<IResiliencyPolicy>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfacePropertiesAreAccessibleThroughInterface()
    {
        // Arrange
        IResiliencyPolicy policy = new DatabaseResiliencyPolicy();

        // Act & Assert
        policy.MaxRetries.ShouldBe(3);
        policy.InitialDelay.ShouldBe(TimeSpan.FromMilliseconds(100));
        policy.MaxDelay.ShouldBe(TimeSpan.FromSeconds(5));
        policy.BackoffMultiplier.ShouldBe(2.0);
        policy.CircuitBreakerDuration.ShouldBe(TimeSpan.FromSeconds(30));
        policy.CircuitBreakerThreshold.ShouldBe(5);
        policy.ResiliencyCategory.Name.ShouldBe("Database");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfaceSupportsPolymorphism()
    {
        // Arrange
        IResiliencyPolicy[] policies = new IResiliencyPolicy[]
        {
            new DatabaseResiliencyPolicy(),
            new HttpClientResiliencyPolicy(),
            new CriticalResiliencyPolicy(),
            new SimpleRetryResiliencyPolicy()
        };

        // Act & Assert
        policies.Length.ShouldBe(4);
        policies.ShouldAllBe(p => p is IResiliencyPolicy);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void InterfaceCanBeUsedInCollections()
    {
        // Arrange
        var policies = new List<IResiliencyPolicy>
        {
            new DatabaseResiliencyPolicy(),
            new HttpClientResiliencyPolicy()
        };

        // Act
        var maxRetries = policies.Select(p => p.MaxRetries).ToList();

        // Assert
        maxRetries.ShouldContain(3);
        maxRetries.ShouldContain(5);
    }
}
