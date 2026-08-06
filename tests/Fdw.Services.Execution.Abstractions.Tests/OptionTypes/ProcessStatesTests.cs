using System.Linq;
using Fdw.Services.Execution.Abstractions.OptionTypes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.OptionTypes;

public class ProcessStatesTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AllReturnsAllStates()
    {
        // Act
        var all = ProcessStates.All();

        // Assert
        all.ShouldNotBeEmpty();
        all.Count.ShouldBeGreaterThanOrEqualTo(7); // Created, Pending, Running, Completed, Failed, Cancelled, TimedOut
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsCorrectState()
    {
        // Arrange
        var all = ProcessStates.All();
        var firstState = all.First();

        // Act
        var result = ProcessStates.ById(firstState.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(firstState.Id);
        result.Name.ShouldBe(firstState.Name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByIdReturnsNotFoundForUnknownId()
    {
        // Act
        var result = ProcessStates.ById(99999);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsPendingState()
    {
        // Act
        var result = ProcessStates.ByName("Pending");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Pending");
        result.IsTerminal.ShouldBeFalse();
        result.IsActive.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsRunningState()
    {
        // Act
        var result = ProcessStates.ByName("Running");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Running");
        result.IsActive.ShouldBeTrue();
        result.IsTerminal.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameIsCaseSensitive()
    {
        // Act & Assert
        ProcessStates.ByName("Pending").ShouldNotBeNull();
        ProcessStates.ByName("Pending").Name.ShouldBe("Pending");
        ProcessStates.ByName("pending").Name.ShouldBe("_Empty");
        ProcessStates.ByName("PENDING").Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ByNameReturnsNotFoundForUnknownName()
    {
        // Act
        var result = ProcessStates.ByName("NonExistent");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void NotFoundReturnsEmptyInstance()
    {
        // Act
        var result = ProcessStates.NotFound;

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("_Empty");
    }
}
