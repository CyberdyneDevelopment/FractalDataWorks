using System;
using System.Linq.Expressions;
using Fdw.Commands.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

public class WhereClauseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InitProperties_CanBeSet()
    {
        // Arrange
        var equalOp = new EqualOperator();
        var andOp = LogicalOperator.And;
        var expression = Expression.Constant(true);

        // Act
        var clause = new WhereClause
        {
            FieldName = "TestField",
            Operator = equalOp,
            Value = 42,
            LogicalOperator = andOp,
            OriginalExpression = expression
        };

        // Assert
        clause.FieldName.ShouldBe("TestField");
        clause.Operator.ShouldBe(equalOp);
        clause.Value.ShouldBe(42);
        clause.LogicalOperator.ShouldBe(andOp);
        clause.OriginalExpression.ShouldBe(expression);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange
        var equalOp = new EqualOperator();
        var andOp = LogicalOperator.And;

        // Act
        var clause = new WhereClause
        {
            Operator = equalOp,
            LogicalOperator = andOp
        };

        // Assert
        clause.FieldName.ShouldBe(string.Empty);
        clause.Value.ShouldBeNull();
        clause.OriginalExpression.ShouldBeOfType<DefaultExpression>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithNullValue_CanBeCreated()
    {
        // Arrange
        var equalOp = new EqualOperator();
        var andOp = LogicalOperator.And;

        // Act
        var clause = new WhereClause
        {
            FieldName = "TestField",
            Operator = equalOp,
            Value = null,
            LogicalOperator = andOp
        };

        // Assert
        clause.Value.ShouldBeNull();
    }
}

public class OrderByClauseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InitProperties_CanBeSet()
    {
        // Arrange
        var direction = new AscendingDirection();
        var expression = Expression.Constant("field");

        // Act
        var clause = new OrderByClause
        {
            FieldName = "TestField",
            Direction = direction,
            OriginalExpression = expression
        };

        // Assert
        clause.FieldName.ShouldBe("TestField");
        clause.Direction.ShouldBe(direction);
        clause.OriginalExpression.ShouldBe(expression);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultValues_AreSetCorrectly()
    {
        // Arrange
        var direction = new AscendingDirection();

        // Act
        var clause = new OrderByClause
        {
            Direction = direction
        };

        // Assert
        clause.FieldName.ShouldBe(string.Empty);
        clause.OriginalExpression.ShouldBeOfType<DefaultExpression>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithDescendingDirection_CanBeCreated()
    {
        // Arrange
        var direction = new DescendingDirection();

        // Act
        var clause = new OrderByClause
        {
            FieldName = "TestField",
            Direction = direction
        };

        // Assert
        clause.Direction.ShouldBe(direction);
        clause.Direction.IsAscending.ShouldBeFalse();
    }
}

public class SelectProjectionTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InitProperties_CanBeSet()
    {
        // Arrange
        var fields = new[]
        {
            new ProjectedField { SourceField = "Id", Alias = "UserId", FieldType = typeof(int) },
            new ProjectedField { SourceField = "Name", Alias = "UserName", FieldType = typeof(string) }
        };
        var expression = Expression.Constant("projection");

        // Act
        var projection = new SelectProjection
        {
            Fields = fields,
            OriginalExpression = expression,
            ResultType = typeof(object)
        };

        // Assert
        projection.Fields.Count.ShouldBe(2);
        projection.Fields[0].SourceField.ShouldBe("Id");
        projection.Fields[1].SourceField.ShouldBe("Name");
        projection.OriginalExpression.ShouldBe(expression);
        projection.ResultType.ShouldBe(typeof(object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var projection = new SelectProjection();

        // Assert
        projection.Fields.ShouldNotBeNull();
        projection.Fields.ShouldBeEmpty();
        projection.OriginalExpression.ShouldBeOfType<DefaultExpression>();
        projection.ResultType.ShouldBe(typeof(object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithEmptyFields_CanBeCreated()
    {
        // Act
        var projection = new SelectProjection
        {
            Fields = Array.Empty<ProjectedField>()
        };

        // Assert
        projection.Fields.ShouldBeEmpty();
    }
}

public class ProjectedFieldTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InitProperties_CanBeSet()
    {
        // Act
        var field = new ProjectedField
        {
            SourceField = "TestField",
            Alias = "FieldAlias",
            FieldType = typeof(string)
        };

        // Assert
        field.SourceField.ShouldBe("TestField");
        field.Alias.ShouldBe("FieldAlias");
        field.FieldType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var field = new ProjectedField();

        // Assert
        field.SourceField.ShouldBe(string.Empty);
        field.Alias.ShouldBe(string.Empty);
        field.FieldType.ShouldBe(typeof(object));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WithDifferentTypes_CanBeCreated()
    {
        // Act
        var intField = new ProjectedField { SourceField = "Id", FieldType = typeof(int) };
        var stringField = new ProjectedField { SourceField = "Name", FieldType = typeof(string) };
        var dateField = new ProjectedField { SourceField = "Created", FieldType = typeof(DateTime) };

        // Assert
        intField.FieldType.ShouldBe(typeof(int));
        stringField.FieldType.ShouldBe(typeof(string));
        dateField.FieldType.ShouldBe(typeof(DateTime));
    }
}
