using Fdw.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.Operators;

public sealed class LogicalOperatorTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AndOperatorHasCorrectProperties()
    {
        // Arrange & Act
        var andOp = LogicalOperator.And;

        // Assert
        andOp.Id.ShouldBe(1);
        andOp.Name.ShouldBe("And");
        andOp.SqlOperator.ShouldBe("AND");
        andOp.ODataOperator.ShouldBe("and");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void OrOperatorHasCorrectProperties()
    {
        // Arrange & Act
        var orOp = LogicalOperator.Or;

        // Assert
        orOp.Id.ShouldBe(2);
        orOp.Name.ShouldBe("Or");
        orOp.SqlOperator.ShouldBe("OR");
        orOp.ODataOperator.ShouldBe("or");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AndOperatorImplementsILogicalOperatorInterface()
    {
        // Arrange & Act
        var andOp = LogicalOperator.And;

        // Assert
        andOp.ShouldBeAssignableTo<ILogicalOperator>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void OrOperatorImplementsILogicalOperatorInterface()
    {
        // Arrange & Act
        var orOp = LogicalOperator.Or;

        // Assert
        orOp.ShouldBeAssignableTo<ILogicalOperator>();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AndAndOrAreDistinctInstances()
    {
        // Arrange
        var andOp = LogicalOperator.And;
        var orOp = LogicalOperator.Or;

        // Act & Assert
        andOp.ShouldNotBe(orOp);
        andOp.Id.ShouldNotBe(orOp.Id);
        andOp.Name.ShouldNotBe(orOp.Name);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void SqlOperatorPropertiesAreDifferent()
    {
        // Arrange
        var andOp = LogicalOperator.And;
        var orOp = LogicalOperator.Or;

        // Act & Assert
        andOp.SqlOperator.ShouldBe("AND");
        orOp.SqlOperator.ShouldBe("OR");
        andOp.SqlOperator.ShouldNotBe(orOp.SqlOperator);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ODataOperatorPropertiesAreDifferent()
    {
        // Arrange
        var andOp = LogicalOperator.And;
        var orOp = LogicalOperator.Or;

        // Act & Assert
        andOp.ODataOperator.ShouldBe("and");
        orOp.ODataOperator.ShouldBe("or");
        andOp.ODataOperator.ShouldNotBe(orOp.ODataOperator);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AndOperatorIsStaticReadonly()
    {
        // Arrange
        var firstReference = LogicalOperator.And;
        var secondReference = LogicalOperator.And;

        // Act & Assert
        ReferenceEquals(firstReference, secondReference).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void OrOperatorIsStaticReadonly()
    {
        // Arrange
        var firstReference = LogicalOperator.Or;
        var secondReference = LogicalOperator.Or;

        // Act & Assert
        ReferenceEquals(firstReference, secondReference).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void SqlOperatorsAreUpperCase()
    {
        // Arrange & Act
        var andSql = LogicalOperator.And.SqlOperator;
        var orSql = LogicalOperator.Or.SqlOperator;

        // Assert
        andSql.ShouldBe(andSql.ToUpperInvariant());
        orSql.ShouldBe(orSql.ToUpperInvariant());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ODataOperatorsAreLowerCase()
    {
        // Arrange & Act
        var andOData = LogicalOperator.And.ODataOperator;
        var orOData = LogicalOperator.Or.ODataOperator;

        // Assert
        andOData.ShouldBe(andOData.ToLowerInvariant());
        orOData.ShouldBe(orOData.ToLowerInvariant());
    }
}
