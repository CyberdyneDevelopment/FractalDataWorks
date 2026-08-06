using Fdw.Services.Execution.Abstractions.OptionTypes;
using Shouldly;
using Xunit;

namespace Fdw.Services.Execution.Abstractions.Tests.OptionTypes;

public class ProcessStateBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsAllProperties()
    {
        // Arrange & Act
        var state = new TestableProcessState(
            id: 99,
            name: "TestState",
            isTerminal: true,
            isError: true,
            isActive: false,
            isInitial: true);

        // Assert
        state.Id.ShouldBe(99);
        state.Name.ShouldBe("TestState");
        state.IsTerminal.ShouldBeTrue();
        state.IsError.ShouldBeTrue();
        state.IsActive.ShouldBeFalse();
        state.IsInitial.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsNonTerminalState()
    {
        // Arrange & Act
        var state = new TestableProcessState(
            id: 1,
            name: "Active",
            isTerminal: false,
            isError: false,
            isActive: true,
            isInitial: false);

        // Assert
        state.IsTerminal.ShouldBeFalse();
        state.IsError.ShouldBeFalse();
        state.IsActive.ShouldBeTrue();
        state.IsInitial.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void StateInheritsFromTypeOptionBase()
    {
        // Arrange & Act
        var state = new TestableProcessState(1, "Test", false, false, false);

        // Assert
        state.ShouldBeAssignableTo<IProcessState>();
    }

    private sealed class TestableProcessState : ProcessStateBase
    {
        public TestableProcessState(int id, string name, bool isTerminal, bool isError, bool isActive, bool isInitial = false)
            : base(id, name, isTerminal, isError, isActive, isInitial)
        {
        }
    }
}
