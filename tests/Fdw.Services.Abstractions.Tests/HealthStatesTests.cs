using Fdw.Services.Abstractions.Health;

namespace Fdw.Services.Abstractions.Tests;

public class HealthStatesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllReturnsHealthStates()
    {
        // Act
        var result = HealthStates.All();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        result.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllContainsHealthyState()
    {
        // Act
        var result = HealthStates.All();

        // Assert
        result.ShouldContain(x => x.Name == "Healthy");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllContainsUnhealthyState()
    {
        // Act
        var result = HealthStates.All();

        // Assert
        result.ShouldContain(x => x.Name == "Unhealthy");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AllContainsDegradedState()
    {
        // Act
        var result = HealthStates.All();

        // Assert
        result.ShouldContain(x => x.Name == "Degraded");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsHealthyForId1()
    {
        // Act
        var result = HealthStates.ById(1);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Healthy");
        result.IsHealthy.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsUnhealthyForId2()
    {
        // Act
        var result = HealthStates.ById(2);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Unhealthy");
        result.IsHealthy.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsDegradedForId3()
    {
        // Act
        var result = HealthStates.ById(3);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Degraded");
        result.IsHealthy.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var result = HealthStates.ById(999);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsHealthy()
    {
        // Act
        var result = HealthStates.ByName("Healthy");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Healthy");
        result.IsHealthy.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsUnhealthy()
    {
        // Act
        var result = HealthStates.ByName("Unhealthy");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Unhealthy");
        result.IsHealthy.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsDegraded()
    {
        // Act
        var result = HealthStates.ByName("Degraded");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Degraded");
        result.IsHealthy.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameIsCaseSensitive()
    {
        // Act & Assert
        HealthStates.ByName("Healthy").ShouldNotBeNull();
        HealthStates.ByName("Healthy").Name.ShouldBe("Healthy");

        // Case-insensitive lookups return NotFound
        HealthStates.ByName("healthy").Name.ShouldBe("_Empty");
        HealthStates.ByName("HEALTHY").Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = HealthStates.ByName("Unknown");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = HealthStates.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }
}
