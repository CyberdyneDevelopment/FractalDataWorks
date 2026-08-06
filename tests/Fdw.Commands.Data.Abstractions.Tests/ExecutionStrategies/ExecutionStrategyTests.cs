using System.Linq;
using Shouldly;
using Xunit;
using ExecutionStrategiesCollection = Fdw.Commands.Data.Abstractions.ExecutionStrategies;

namespace Fdw.Commands.Data.Abstractions.Tests.ExecutionStrategies;

/// <summary>
/// Tests for ExecutionStrategy TypeCollection and concrete strategies.
/// </summary>
public sealed class ExecutionStrategyTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllReturnsAllStrategies()
    {
        // Act
        var all = ExecutionStrategiesCollection.All();

        // Assert
        all.ShouldNotBeNull();
        all.ShouldNotBeEmpty();
        all.Count.ShouldBe(4); // Sequential, Parallel, SequentialStopOnFailure, SequentialContinueOnFailure
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsCorrectStrategy()
    {
        // Act
        var sequential = ExecutionStrategiesCollection.ByName("Sequential");

        // Assert
        sequential.ShouldNotBeNull();
        sequential.Name.ShouldBe("Sequential");
        sequential.Id.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameIsCaseSensitive()
    {
        // Act
        var lowercase = ExecutionStrategiesCollection.ByName("sequential");
        var uppercase = ExecutionStrategiesCollection.ByName("SEQUENTIAL");

        // Assert - Case-sensitive, so these return NotFound
        lowercase.ShouldBe(ExecutionStrategiesCollection.NotFound);
        uppercase.ShouldBe(ExecutionStrategiesCollection.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByNameReturnsNotFoundForInvalidName()
    {
        // Act
        var result = ExecutionStrategiesCollection.ByName("InvalidStrategy");

        // Assert
        result.ShouldBe(ExecutionStrategiesCollection.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsCorrectStrategy()
    {
        // Act
        var sequential = ExecutionStrategiesCollection.ById(1);

        // Assert
        sequential.ShouldNotBeNull();
        sequential.Id.ShouldBe(1);
        sequential.Name.ShouldBe("Sequential");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ByIdReturnsNotFoundForInvalidId()
    {
        // Act
        var result = ExecutionStrategiesCollection.ById(999);

        // Assert
        result.ShouldBe(ExecutionStrategiesCollection.NotFound);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NotFoundIsAvailable()
    {
        // Act
        var notFound = ExecutionStrategiesCollection.NotFound;

        // Assert
        notFound.ShouldNotBeNull();
        notFound.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SequentialStrategyHasCorrectProperties()
    {
        // Act
        var strategy = ExecutionStrategiesCollection.ByName("Sequential");

        // Assert
        strategy.Id.ShouldBe(1);
        strategy.Name.ShouldBe("Sequential");
        strategy.IsSequential.ShouldBeTrue();
        strategy.StopOnFailure.ShouldBeTrue();
        strategy.SupportsParallel.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ParallelStrategyHasCorrectProperties()
    {
        // Act
        var strategy = ExecutionStrategiesCollection.ByName("Parallel");

        // Assert
        strategy.Id.ShouldBe(2);
        strategy.Name.ShouldBe("Parallel");
        strategy.IsSequential.ShouldBeFalse();
        strategy.StopOnFailure.ShouldBeFalse();
        strategy.SupportsParallel.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SequentialStopOnFailureStrategyHasCorrectProperties()
    {
        // Act
        var strategy = ExecutionStrategiesCollection.ByName("SequentialStopOnFailure");

        // Assert
        strategy.Id.ShouldBe(3);
        strategy.Name.ShouldBe("SequentialStopOnFailure");
        strategy.IsSequential.ShouldBeTrue();
        strategy.StopOnFailure.ShouldBeTrue();
        strategy.SupportsParallel.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SequentialContinueOnFailureStrategyHasCorrectProperties()
    {
        // Act
        var strategy = ExecutionStrategiesCollection.ByName("SequentialContinueOnFailure");

        // Assert
        strategy.Id.ShouldBe(4);
        strategy.Name.ShouldBe("SequentialContinueOnFailure");
        strategy.IsSequential.ShouldBeTrue();
        strategy.StopOnFailure.ShouldBeFalse();
        strategy.SupportsParallel.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void StaticPropertiesProvideDirectAccess()
    {
        // Assert - All strategies are accessible via static properties
        ExecutionStrategiesCollection.Sequential.ShouldNotBeNull();
        ExecutionStrategiesCollection.Sequential.Name.ShouldBe("Sequential");

        ExecutionStrategiesCollection.Parallel.ShouldNotBeNull();
        ExecutionStrategiesCollection.Parallel.Name.ShouldBe("Parallel");

        ExecutionStrategiesCollection.SequentialStopOnFailure.ShouldNotBeNull();
        ExecutionStrategiesCollection.SequentialStopOnFailure.Name.ShouldBe("SequentialStopOnFailure");

        ExecutionStrategiesCollection.SequentialContinueOnFailure.ShouldNotBeNull();
        ExecutionStrategiesCollection.SequentialContinueOnFailure.Name.ShouldBe("SequentialContinueOnFailure");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllStrategiesHaveUniqueIds()
    {
        // Act
        var all = ExecutionStrategiesCollection.All();

        // Assert
        var ids = all.Select(s => s.Id).ToList();
        ids.Distinct().Count().ShouldBe(ids.Count);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AllStrategiesHaveUniqueNames()
    {
        // Act
        var all = ExecutionStrategiesCollection.All();

        // Assert
        var names = all.Select(s => s.Name).ToList();
        names.Distinct().Count().ShouldBe(names.Count);
    }
}
