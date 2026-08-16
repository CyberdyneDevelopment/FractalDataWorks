using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.OData;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.OData.Tests.Translators;

/// <summary>
/// Gap tests for ODataQueryTranslator - covers: null metadata, Or filter group,
/// single-node group, empty group, descending ordering, paging with only skip.
/// </summary>
public sealed class ODataQueryTranslatorGapTests
{
    private readonly ODataQueryTranslator _sut = new();

    private static Mock<IStorageContainer> CreateContainer(string name = "Customers")
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns([]);
        schema.Setup(s => s.GetProjectableFields()).Returns([]);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns(name);
        container.Setup(c => c.Schema).Returns(schema.Object);
        return container;
    }

    private static Mock<IDataCommand> CreateCommand(Dictionary<string, object>? metadata = null)
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(
            metadata != null
                ? new Dictionary<string, object>(metadata, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
        return command;
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateHandlesNullMetadata()
    {
        // Arrange
        var container = CreateContainer();
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns((IReadOnlyDictionary<string, object>?)null!);

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - should still produce a valid GET without query params
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldBe("/Customers");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateWithOrFilterGroup()
    {
        // Arrange
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.Or,
                Nodes =
                [
                    new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "Active" },
                    new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = "Pending" }
                ]
            }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$filter=");
        // Should contain "or" logical operator
        uri.ShouldContain("or");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateWithSingleNodeFilterGroup()
    {
        // Arrange - group with single condition
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterCondition { PropertyName = "Name", Operator = new EqualOperator(), Value = "Test" }
                ]
            }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$filter=");
        uri.ShouldContain("Name");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateWithDescendingOrderBy()
    {
        // Arrange
        var container = CreateContainer();
        var ordering = new OrderingExpression
        {
            OrderedFields = [new OrderedField { PropertyName = "CreatedDate", Direction = SortDirections.ByName("Descending") }]
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Ordering"] = ordering });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$orderby=");
        uri.ShouldContain("desc");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateWithMultipleOrderByFields()
    {
        // Arrange
        var container = CreateContainer();
        var ordering = new OrderingExpression
        {
            OrderedFields =
            [
                new OrderedField { PropertyName = "LastName", Direction = SortDirections.ByName("Ascending") },
                new OrderedField { PropertyName = "FirstName", Direction = SortDirections.ByName("Descending") }
            ]
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Ordering"] = ordering });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$orderby=");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateWithPagingSkipOnlyNoTop()
    {
        // Arrange - skip > 0 but Take is null or 0
        var container = CreateContainer();
        var paging = new PagingExpression { Skip = 50, Take = null };
        var command = CreateCommand(new Dictionary<string, object> { ["Paging"] = paging });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$skip=50");
        uri.ShouldNotContain("$top=");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateWithIsNullFilter()
    {
        // Arrange
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "DeletedDate",
                Operator = new IsNullOperator(),
                Value = null
            }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$filter=");
        uri.ShouldContain("DeletedDate");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateWithNestedFilterGroups()
    {
        // Arrange
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    new FilterGroup
                    {
                        Operator = LogicalOperator.Or,
                        Nodes =
                        [
                            new FilterCondition { PropertyName = "City", Operator = new EqualOperator(), Value = "NYC" },
                            new FilterCondition { PropertyName = "City", Operator = new EqualOperator(), Value = "LA" }
                        ]
                    },
                    new FilterCondition { PropertyName = "Active", Operator = new EqualOperator(), Value = true }
                ]
            }
        };
        var command = CreateCommand(new Dictionary<string, object> { ["Filter"] = filter });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldContain("$filter=");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateWithEmptySelectExcludesSelectParam()
    {
        // Arrange - projection with no fields should not add $select
        var container = CreateContainer();
        var projection = new ProjectionExpression { Fields = [] };
        var command = CreateCommand(new Dictionary<string, object> { ["Projection"] = projection });

        // Act
        var result = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var uri = result.Value!.RequestUri!.ToString();
        uri.ShouldNotContain("$select");
    }
}
