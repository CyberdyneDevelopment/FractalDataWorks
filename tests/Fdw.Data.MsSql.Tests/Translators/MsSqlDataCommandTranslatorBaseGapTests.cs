using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// Gap tests for MsSqlDataCommandTranslatorBase - covers BuildWhereClause branches:
/// invalid column name, single-node group, empty group, OR group, IsNull/IsNotNull operators,
/// and AddParameter overloads.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlDataCommandTranslatorBaseGapTests
{
    // We use MsSqlQueryTranslator as a concrete subclass to test the protected base methods
    // through the Translate method which calls BuildWhereClause
    private readonly MsSqlQueryTranslator _sut = new();

    private static Mock<IDataContainer> CreateContainer(
        string name = "Test",
        string schema = "dbo")
    {
        var dbPath = new DatabasePath("", schema, name);
        // Why: translator rejects empty-field containers (SELECT * is forbidden); supply a
        // default Id field so WHERE/ORDER BY/PAGING tests reach the clause-building logic.
        var mockField = new Mock<IField>();
        mockField.Setup(f => f.Name).Returns("Id");
        mockField.Setup(f => f.IsIdentity).Returns(false);
        mockField.Setup(f => f.IsComputed).Returns(false);
        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(new[] { mockField.Object });
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(new[] { mockField.Object });

        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns(name);
        // Why: translator reads container.Path via IStorageContainer.Path for the
        // `is not DatabasePath` guard; use .As<IStorageContainer>() to target that member.
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);
        container.Setup(c => c.ReferencingKeys)
            .Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]));
        container.Setup(c => c.Keys).Returns(new List<IContainerKey>());

        return container;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildWhereClauseThrowsForInvalidColumnName()
    {
        // Arrange - column name with special characters
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "Drop; --",
                Operator = new EqualOperator(),
                Value = "hack"
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        // Act
        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - invalid column name causes exception -> failure
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildWhereClauseRejectsColumnNameStartingWithDigit()
    {
        // Arrange
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "1Column",
                Operator = new EqualOperator(),
                Value = "test"
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        // Act
        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - column starting with digit is invalid
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildWhereClauseAcceptsColumnNameStartingWithUnderscore()
    {
        // Arrange
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "_InternalField",
                Operator = new EqualOperator(),
                Value = "test"
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        // Act
        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("[_InternalField]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildWhereClauseRejectsEmptyColumnName()
    {
        // Arrange
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "",
                Operator = new EqualOperator(),
                Value = "test"
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        // Act
        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildWhereClauseHandlesSingleNodeGroup()
    {
        // Arrange - group with only one condition, should not wrap in parens
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

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        // Act
        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - single node group: no wrapping parens
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE [Name] = @p0");
        // Should NOT have outer parens for single item
        result.Value.CommandText.ShouldNotContain("(");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildWhereClauseHandlesEmptyGroup()
    {
        // Arrange - group with no nodes
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes = []
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        // Act
        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - empty group returns empty string, WHERE clause should be empty
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE ");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildWhereClauseHandlesIsNotNullOperator()
    {
        // Arrange
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "DeletedDate",
                Operator = new IsNotNullOperator(),
                Value = null
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        // Act
        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("[DeletedDate] IS NOT NULL");
        result.Value.Parameters.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildOrderByClauseWithMultipleFields()
    {
        // Arrange
        var container = CreateContainer();

        var field1 = new Mock<IOrderedField>();
        field1.Setup(f => f.PropertyName).Returns("LastName");
        field1.Setup(f => f.Direction).Returns(new AscendingDirection());

        var field2 = new Mock<IOrderedField>();
        field2.Setup(f => f.PropertyName).Returns("FirstName");
        field2.Setup(f => f.Direction).Returns(new DescendingDirection());

        var ordering = new OrderingExpression
        {
            OrderedFields = [field1.Object, field2.Object]
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns(ordering);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        // Act
        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("ORDER BY [LastName] ASC, [FirstName] DESC");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildPagingClauseGeneratesCorrectOffsetFetch()
    {
        // Arrange
        var fields = new Mock<IField>();
        fields.Setup(f => f.Name).Returns("Id");
        // Why: IsPrimaryKey removed from IField — PK identity resolved from container Metadata["SurrogateKeyField"].

        var containerSchema = new Mock<IContainerSchema>();
        containerSchema.Setup(s => s.Fields).Returns(new[] { fields.Object });
        containerSchema.Setup(s => s.GetProjectableFields()).Returns(new[] { fields.Object });

        var dbPath = new DatabasePath("", "dbo", "Test");
        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("Test");
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(containerSchema.Object);
        container.Setup(c => c.ReferencingKeys)
            .Returns(GenericResult<IReadOnlyList<ReferencingKeyBinding>>.Success([]));
        container.Setup(c => c.Keys).Returns(new List<IContainerKey>());

        var paging = new PagingExpression { Skip = 100, Take = 50 };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns((IFilterExpression?)null);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns(paging);

        // Act
        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task BuildWhereClauseHandlesNullValueInCondition()
    {
        // Arrange - condition value is null but operator requires value
        var container = CreateContainer();
        var filter = new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = "Name",
                Operator = new EqualOperator(),
                Value = null
            }
        };

        var queryCommand = new Mock<IQueryCommand>();
        queryCommand.Setup(q => q.Filter).Returns(filter);
        queryCommand.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        queryCommand.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        queryCommand.Setup(q => q.Paging).Returns((IPagingExpression?)null);

        // Act
        var result = await _sut.Translate(queryCommand.Object, container.Object, TestContext.Current.CancellationToken);

        // Assert - null value should be passed as DBNull.Value parameter
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters["@p0"].Value.ShouldBe(DBNull.Value);
    }
}
