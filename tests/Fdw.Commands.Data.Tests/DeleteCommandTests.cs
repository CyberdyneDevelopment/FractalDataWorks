using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// Tests for DeleteCommand with hierarchical filter support.
/// </summary>
public sealed class DeleteCommandTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DeleteCommand_CreatesCommandWithCorrectCommandType()
    {
        // Act
        var command = new DeleteCommand();

        // Assert
        command.CommandType.ShouldBe("Delete");
        command.Filter.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DeleteCommandShouldAcceptSingleConditionFilter()
    {
        // Arrange
        var condition = new FilterCondition
        {
            PropertyName = "Id",
            Operator = new EqualOperator(),
            Value = 42
        };
        var filter = new FilterExpression { Root = condition };

        // Act
        var command = new DeleteCommand
        {
            Filter = filter
        };

        // Assert
        command.Filter.ShouldNotBeNull();
        command.Filter.Root.ShouldNotBeNull();
        command.Filter.Root.ShouldBeOfType<FilterCondition>();
        var rootCondition = (FilterCondition)command.Filter.Root;
        rootCondition.PropertyName.ShouldBe("Id");
        rootCondition.Value.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DeleteCommandShouldAcceptFilterGroupWithMultipleConditions()
    {
        // Arrange
        var condition1 = new FilterCondition
        {
            PropertyName = "IsDeleted",
            Operator = new EqualOperator(),
            Value = true
        };
        var condition2 = new FilterCondition
        {
            PropertyName = "CreatedDate",
            Operator = new LessThanOperator(),
            Value = "2020-01-01"
        };
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new[] { condition1, condition2 }
        };
        var filter = new FilterExpression { Root = group };

        // Act
        var command = new DeleteCommand
        {
            Filter = filter
        };

        // Assert
        command.Filter.ShouldNotBeNull();
        command.Filter.Root.ShouldNotBeNull();
        command.Filter.Root.ShouldBeOfType<FilterGroup>();
        var rootGroup = (FilterGroup)command.Filter.Root;
        rootGroup.Operator.ShouldBe(LogicalOperator.And);
        rootGroup.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DeleteCommandShouldAcceptNestedFilterGroups()
    {
        // Arrange - ((Status = 'A' OR Status = 'B') AND IsActive = false)
        var statusCondition1 = new FilterCondition
        {
            PropertyName = "Status",
            Operator = new EqualOperator(),
            Value = "A"
        };
        var statusCondition2 = new FilterCondition
        {
            PropertyName = "Status",
            Operator = new EqualOperator(),
            Value = "B"
        };
        var orGroup = new FilterGroup
        {
            Operator = LogicalOperator.Or,
            Nodes = new[] { statusCondition1, statusCondition2 }
        };
        var activeCondition = new FilterCondition
        {
            PropertyName = "IsActive",
            Operator = new EqualOperator(),
            Value = false
        };
        var rootGroup = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = new IFilterNode[] { orGroup, activeCondition }
        };
        var filter = new FilterExpression { Root = rootGroup };

        // Act
        var command = new DeleteCommand
        {
            Filter = filter
        };

        // Assert
        command.Filter.ShouldNotBeNull();
        command.Filter.Root.ShouldNotBeNull();
        command.Filter.Root.ShouldBeOfType<FilterGroup>();
        var root = (FilterGroup)command.Filter.Root;
        root.Nodes.Count.ShouldBe(2);
        root.Nodes[0].ShouldBeOfType<FilterGroup>();
        root.Nodes[1].ShouldBeOfType<FilterCondition>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DeleteCommandWithNullFilterShouldBeValid()
    {
        // Act
        var command = new DeleteCommand
        {
            Filter = null
        };

        // Assert
        command.Filter.ShouldBeNull();
    }
}
