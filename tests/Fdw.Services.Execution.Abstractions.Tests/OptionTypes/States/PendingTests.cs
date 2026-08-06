using Fdw.Services.Execution.Abstractions.OptionTypes.States;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.OptionTypes.States;

public class PendingTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsCorrectProperties()
    {
        // Arrange & Act
        var pending = new Pending();

        // Assert
        pending.Id.ShouldBe(6);
        pending.Name.ShouldBe("Pending");
        pending.IsTerminal.ShouldBeFalse();
        pending.IsError.ShouldBeFalse();
        pending.IsActive.ShouldBeFalse();
        pending.IsInitial.ShouldBeFalse();
    }
}
