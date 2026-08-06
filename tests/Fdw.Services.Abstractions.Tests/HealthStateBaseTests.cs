using Fdw.Services.Abstractions.Health;

namespace Fdw.Services.Abstractions.Tests;

public class HealthStateBaseTests
{
    [ExcludeFromCodeCoverage]
    private sealed class TestHealthState : HealthStateBase
    {
        public TestHealthState(int id, string name, bool isHealthy)
            : base(id, name, isHealthy)
        {
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsId()
    {
        // Arrange
        var id = 99;

        // Act
        var result = new TestHealthState(id, "Test", true);

        // Assert
        result.Id.ShouldBe(id);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsName()
    {
        // Arrange
        var name = "TestState";

        // Act
        var result = new TestHealthState(1, name, true);

        // Assert
        result.Name.ShouldBe(name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsIsHealthyTrue()
    {
        // Act
        var result = new TestHealthState(1, "Test", true);

        // Assert
        result.IsHealthy.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsIsHealthyFalse()
    {
        // Act
        var result = new TestHealthState(1, "Test", false);

        // Assert
        result.IsHealthy.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ImplementsIHealthState()
    {
        // Act
        var result = new TestHealthState(1, "Test", true);

        // Assert
        result.ShouldBeAssignableTo<IHealthState>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigurationKeyFollowsPattern()
    {
        // Act
        var result = new TestHealthState(1, "TestState", true);

        // Assert
        result.ConfigurationKey.ShouldBe("HealthStates:TestState");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DisplayNameEqualsName()
    {
        // Arrange
        var name = "TestState";

        // Act
        var result = new TestHealthState(1, name, true);

        // Assert
        result.DisplayName.ShouldBe(name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void DescriptionContainsName()
    {
        // Arrange
        var name = "TestState";

        // Act
        var result = new TestHealthState(1, name, true);

        // Assert
        result.Description.ShouldContain(name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CategoryIsHealthChecks()
    {
        // Act
        var result = new TestHealthState(1, "Test", true);

        // Assert
        result.Category.ShouldBe("HealthChecks");
    }
}
