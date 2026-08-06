using Fdw.Services.Execution.Abstractions.OptionTypes.States;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.OptionTypes.States;

public class RunningTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsCorrectProperties()
    {
        // Arrange & Act
        var running = new Running();

        // Assert
        running.Id.ShouldBe(2);
        running.Name.ShouldBe("Running");
        running.IsTerminal.ShouldBeFalse();
        running.IsError.ShouldBeFalse();
        running.IsActive.ShouldBeTrue();
        running.IsInitial.ShouldBeFalse();
    }
}
