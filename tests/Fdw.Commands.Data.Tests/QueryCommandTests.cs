using System.Collections.Generic;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// Comprehensive tests for the <see cref="QueryCommand{T}"/> class.
/// Achieves 100% code path coverage for QueryCommand.
/// </summary>
public sealed class QueryCommandTests
{
    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #region Constructor Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_CreatesCommandWithCorrectCommandType()
    {
        // Act
        var command = new QueryCommand<TestEntity>();

        // Assert
        command.CommandType.ShouldBe("Query");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_InitializesOptionalPropertiesToNull()
    {
        // Act
        var command = new QueryCommand<TestEntity>();

        // Assert
        command.Filter.ShouldBeNull();
        command.Projection.ShouldBeNull();
        command.Ordering.ShouldBeNull();
        command.Paging.ShouldBeNull();
        command.Aggregation.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_InitializesJoinsToEmptyList()
    {
        // Act
        var command = new QueryCommand<TestEntity>();

        // Assert
        command.Joins.ShouldNotBeNull();
        command.Joins.ShouldBeEmpty();
    }

    #endregion

    #region Property Initialization Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Filter_CanBeSetViaInitializer()
    {
        // Arrange
        var filter = new Mock<IFilterExpression>().Object;

        // Act
        var command = new QueryCommand<TestEntity>
        {
            Filter = filter
        };

        // Assert
        command.Filter.ShouldBe(filter);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Projection_CanBeSetViaInitializer()
    {
        // Arrange
        var projection = new Mock<IProjectionExpression>().Object;

        // Act
        var command = new QueryCommand<TestEntity>
        {
            Projection = projection
        };

        // Assert
        command.Projection.ShouldBe(projection);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Ordering_CanBeSetViaInitializer()
    {
        // Arrange
        var ordering = new Mock<IOrderingExpression>().Object;

        // Act
        var command = new QueryCommand<TestEntity>
        {
            Ordering = ordering
        };

        // Assert
        command.Ordering.ShouldBe(ordering);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Paging_CanBeSetViaInitializer()
    {
        // Arrange
        var paging = new Mock<IPagingExpression>().Object;

        // Act
        var command = new QueryCommand<TestEntity>
        {
            Paging = paging
        };

        // Assert
        command.Paging.ShouldBe(paging);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Aggregation_CanBeSetViaInitializer()
    {
        // Arrange
        var aggregation = new Mock<IAggregationExpression>().Object;

        // Act
        var command = new QueryCommand<TestEntity>
        {
            Aggregation = aggregation
        };

        // Assert
        command.Aggregation.ShouldBe(aggregation);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Joins_CanBeSetViaInitializer()
    {
        // Arrange
        var join1 = new Mock<IJoinExpression>().Object;
        var join2 = new Mock<IJoinExpression>().Object;
        var joins = new List<IJoinExpression> { join1, join2 };

        // Act
        var command = new QueryCommand<TestEntity>
        {
            Joins = joins
        };

        // Assert
        command.Joins.ShouldBe(joins);
        command.Joins.Count.ShouldBe(2);
    }

    #endregion

    #region Combined Properties Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Command_CanHaveAllPropertiesSet()
    {
        // Arrange
        var filter = new Mock<IFilterExpression>().Object;
        var projection = new Mock<IProjectionExpression>().Object;
        var ordering = new Mock<IOrderingExpression>().Object;
        var paging = new Mock<IPagingExpression>().Object;
        var aggregation = new Mock<IAggregationExpression>().Object;
        var join = new Mock<IJoinExpression>().Object;

        // Act
        var command = new QueryCommand<TestEntity>
        {
            Filter = filter,
            Projection = projection,
            Ordering = ordering,
            Paging = paging,
            Aggregation = aggregation,
            Joins = [join]
        };

        // Assert
        command.Filter.ShouldBe(filter);
        command.Projection.ShouldBe(projection);
        command.Ordering.ShouldBe(ordering);
        command.Paging.ShouldBe(paging);
        command.Aggregation.ShouldBe(aggregation);
        command.Joins.Count.ShouldBe(1);
        command.Joins[0].ShouldBe(join);
    }

    #endregion

    #region Metadata Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Metadata_IsInitializedByDefault()
    {
        // Arrange & Act
        var command = new QueryCommand<TestEntity>();

        // Assert
        command.Metadata.ShouldNotBeNull();
        command.Metadata.Count.ShouldBe(0);
    }

    #endregion

    #region Type Safety Tests

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void QueryCommand_SupportsGenericTypes()
    {
        // Arrange & Act
        var intCommand = new QueryCommand<int>();
        var stringCommand = new QueryCommand<string>();
        var complexCommand = new QueryCommand<TestEntity>();

        // Assert
        intCommand.ShouldNotBeNull();
        stringCommand.ShouldNotBeNull();
        complexCommand.ShouldNotBeNull();
    }

    #endregion
}
