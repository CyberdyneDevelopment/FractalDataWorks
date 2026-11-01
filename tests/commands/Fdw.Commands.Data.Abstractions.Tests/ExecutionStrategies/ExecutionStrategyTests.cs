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
    public void SequentialStrategyHasCorrectProperties()
    {
        // Act
        var strategy = ExecutionStrategiesCollection.ByName("Sequential");
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
}
