using System;
using System.Linq;
using System.Linq.Expressions;
using Fdw.Data.DataSets.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

public class DataQueryBuilderTests
{
    private class TestRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_WithValidDataSetName_CreatesInstance()
    {
        // Act
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Assert
        query.DataSetName.ShouldBe("TestDataSet");
        query.SourceType.ShouldBe(typeof(TestRecord));
        query.ResultType.ShouldBe(typeof(TestRecord));
        query.QueryExpression.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Constructor_WithNullDataSetName_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            new DataQueryBuilder<TestRecord>(null!));

        exception.ParamName.ShouldBe("dataSetName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Where_WithValidPredicate_ReturnsNewQuery()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");
        Expression<Func<TestRecord, bool>> predicate = r => r.Id > 10;

        // Act
        var result = query.Where(predicate);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(query);
        result.DataSetName.ShouldBe("TestDataSet");
        result.SourceType.ShouldBe(typeof(TestRecord));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Where_WithNullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            query.Where(null!));

        exception.ParamName.ShouldBe("predicate");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Where_CreatesMethodCallExpression()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");
        Expression<Func<TestRecord, bool>> predicate = r => r.Id > 10;

        // Act
        var result = query.Where(predicate);

        // Assert
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();
        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("Where");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Select_WithValidSelector_ReturnsNewQuery()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");
        Expression<Func<TestRecord, string>> selector = r => r.Name;

        // Act
        var result = query.Select(selector);

        // Assert
        result.ShouldNotBeNull();
        result.DataSetName.ShouldBe("TestDataSet");
        result.ResultType.ShouldBe(typeof(string));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Select_WithNullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            query.Select<string>(null!));

        exception.ParamName.ShouldBe("selector");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Select_CreatesMethodCallExpression()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");
        Expression<Func<TestRecord, string>> selector = r => r.Name;

        // Act
        var result = query.Select(selector);

        // Assert
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();
        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("Select");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderBy_WithValidKeySelector_ReturnsNewQuery()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");
        Expression<Func<TestRecord, int>> keySelector = r => r.Id;

        // Act
        var result = query.OrderBy(keySelector);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(query);
        result.DataSetName.ShouldBe("TestDataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderBy_WithNullKeySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            query.OrderBy<int>(null!));

        exception.ParamName.ShouldBe("keySelector");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderBy_CreatesMethodCallExpression()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");
        Expression<Func<TestRecord, int>> keySelector = r => r.Id;

        // Act
        var result = query.OrderBy(keySelector);

        // Assert
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();
        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("OrderBy");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByDescending_WithValidKeySelector_ReturnsNewQuery()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");
        Expression<Func<TestRecord, int>> keySelector = r => r.Id;

        // Act
        var result = query.OrderByDescending(keySelector);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(query);
        result.DataSetName.ShouldBe("TestDataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByDescending_WithNullKeySelector_ThrowsArgumentNullException()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act & Assert
        var exception = Should.Throw<ArgumentNullException>(() =>
            query.OrderByDescending<int>(null!));

        exception.ParamName.ShouldBe("keySelector");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByDescending_CreatesMethodCallExpression()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");
        Expression<Func<TestRecord, int>> keySelector = r => r.Id;

        // Act
        var result = query.OrderByDescending(keySelector);

        // Assert
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();
        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("OrderByDescending");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Take_WithValidCount_ReturnsNewQuery()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = query.Take(10);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(query);
        result.DataSetName.ShouldBe("TestDataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Take_WithNegativeCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            query.Take(-1));

        exception.ParamName.ShouldBe("count");
        exception.Message.ShouldContain("cannot be negative");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Take_WithZero_CreatesQuery()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = query.Take(0);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Take_CreatesMethodCallExpression()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = query.Take(10);

        // Assert
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();
        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("Take");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Skip_WithValidCount_ReturnsNewQuery()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = query.Skip(10);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(query);
        result.DataSetName.ShouldBe("TestDataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Skip_WithNegativeCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act & Assert
        var exception = Should.Throw<ArgumentOutOfRangeException>(() =>
            query.Skip(-1));

        exception.ParamName.ShouldBe("count");
        exception.Message.ShouldContain("cannot be negative");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Skip_WithZero_CreatesQuery()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = query.Skip(0);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void Skip_CreatesMethodCallExpression()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = query.Skip(10);

        // Assert
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();
        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("Skip");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FluentQuery_ChainsMultipleOperations()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = query
            .Where(r => r.Age > 18)
            .OrderBy(r => r.Name)
            .Skip(10)
            .Take(5);

        // Assert
        result.ShouldNotBeNull();
        result.DataSetName.ShouldBe("TestDataSet");
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToString_ReturnsDescriptiveString()
    {
        // Arrange
        var query = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = query.ToString();

        // Assert
        result.ShouldContain("DataQuery");
        result.ShouldContain("TestDataSet");
    }

}
