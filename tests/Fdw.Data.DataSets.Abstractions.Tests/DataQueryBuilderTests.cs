using System;
using System.Linq;
using System.Linq.Expressions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Abstractions.Tests;

public class DataQueryBuilderTests
{
    private sealed class TestRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    private sealed class ProjectedRecord
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithDataSetNameInitializesProperties()
    {
        // Arrange & Act
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Assert
        sut.DataSetName.ShouldBe("TestDataSet");
        sut.SourceType.ShouldBe(typeof(TestRecord));
        sut.ResultType.ShouldBe(typeof(TestRecord));
        sut.QueryExpression.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenDataSetNameIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new DataQueryBuilder<TestRecord>(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithExpressionInitializesProperties()
    {
        // Arrange
        var expression = Expression.Constant(42);

        // Act
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet", expression);

        // Assert
        sut.DataSetName.ShouldBe("TestDataSet");
        sut.QueryExpression.ShouldBe(expression);
        sut.SourceType.ShouldBe(typeof(TestRecord));
        sut.ResultType.ShouldBe(typeof(TestRecord));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereCreatesNewQueryWithPredicate()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");
        Expression<Func<TestRecord, bool>> predicate = r => r.Age > 18;

        // Act
        var result = sut.Where(predicate);

        // Assert
        result.ShouldNotBeNull();
        result.DataSetName.ShouldBe("TestDataSet");
        result.QueryExpression.ShouldNotBe(sut.QueryExpression);
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();

        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("Where");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereThrowsWhenPredicateIsNull()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => sut.Where(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WhereCanBeChained()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut
            .Where(r => r.Age > 18)
            .Where(r => r.IsActive);

        // Assert
        result.ShouldNotBeNull();
        result.DataSetName.ShouldBe("TestDataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectCreatesNewQueryWithSelector()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");
        Expression<Func<TestRecord, ProjectedRecord>> selector = r => new ProjectedRecord { Id = r.Id, Name = r.Name };

        // Act
        var result = sut.Select(selector);

        // Assert
        result.ShouldNotBeNull();
        result.DataSetName.ShouldBe("TestDataSet");
        result.ResultType.ShouldBe(typeof(ProjectedRecord));
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();

        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("Select");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectThrowsWhenSelectorIsNull()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => sut.Select<ProjectedRecord>(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectChangesResultType()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut.Select(r => r.Name);

        // Assert
        result.ResultType.ShouldBe(typeof(string));
        result.SourceType.ShouldBe(typeof(string)); // After Select, SourceType changes to TResult
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByCreatesNewQueryWithOrdering()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");
        Expression<Func<TestRecord, string>> keySelector = r => r.Name;

        // Act
        var result = sut.OrderBy(keySelector);

        // Assert
        result.ShouldNotBeNull();
        result.DataSetName.ShouldBe("TestDataSet");
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();

        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("OrderBy");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByThrowsWhenKeySelectorIsNull()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => sut.OrderBy<string>(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByDescendingCreatesNewQueryWithOrdering()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");
        Expression<Func<TestRecord, int>> keySelector = r => r.Age;

        // Act
        var result = sut.OrderByDescending(keySelector);

        // Assert
        result.ShouldNotBeNull();
        result.DataSetName.ShouldBe("TestDataSet");
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();

        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("OrderByDescending");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void OrderByDescendingThrowsWhenKeySelectorIsNull()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => sut.OrderByDescending<string>(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TakeCreatesNewQueryWithLimit()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut.Take(10);

        // Assert
        result.ShouldNotBeNull();
        result.DataSetName.ShouldBe("TestDataSet");
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();

        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("Take");
        methodCall.Arguments.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TakeThrowsWhenCountIsNegative()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act & Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => sut.Take(-1));
        ex.ParamName.ShouldBe("count");
        ex.Message.ShouldContain("Count cannot be negative");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TakeAllowsZero()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut.Take(0);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SkipCreatesNewQueryWithOffset()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut.Skip(5);

        // Assert
        result.ShouldNotBeNull();
        result.DataSetName.ShouldBe("TestDataSet");
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();

        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("Skip");
        methodCall.Arguments.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SkipThrowsWhenCountIsNegative()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act & Assert
        var ex = Should.Throw<ArgumentOutOfRangeException>(() => sut.Skip(-1));
        ex.ParamName.ShouldBe("count");
        ex.Message.ShouldContain("Count cannot be negative");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SkipAllowsZero()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut.Skip(0);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ComplexQueryCanBeChained()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut
            .Where(r => r.Age > 18)
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Skip(10)
            .Take(20);

        // Assert
        result.ShouldNotBeNull();
        result.DataSetName.ShouldBe("TestDataSet");
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ComplexQueryWithSelectCanBeChained()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut
            .Where(r => r.Age > 18)
            .OrderBy(r => r.Name)
            .Select(r => new ProjectedRecord { Id = r.Id, Name = r.Name })
            .Skip(5)
            .Take(10);

        // Assert
        result.ShouldNotBeNull();
        result.DataSetName.ShouldBe("TestDataSet");
        result.ResultType.ShouldBe(typeof(ProjectedRecord));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringReturnsDescriptiveFormat()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut.ToString();

        // Assert
        result.ShouldContain("DataQuery[TestDataSet]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToStringIncludesExpression()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var query = sut.Where(r => r.Age > 18);
        var result = query.ToString();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("TestDataSet");
        result.ShouldContain("Where");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void QueryExpressionIsConstantExpressionForRootQuery()
    {
        // Arrange & Act
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Assert
        sut.QueryExpression.ShouldBeAssignableTo<ConstantExpression>();
        var constant = (ConstantExpression)sut.QueryExpression;
        constant.Type.ShouldBe(typeof(IQueryable<TestRecord>));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void MultipleOrderByCreatesChainedExpressions()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut
            .OrderBy(r => r.Name)
            .OrderByDescending(r => r.Age);

        // Assert
        result.ShouldNotBeNull();
        result.QueryExpression.ShouldBeAssignableTo<MethodCallExpression>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TakeAndSkipCanBeCombined()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut.Skip(10).Take(20);

        // Assert
        result.ShouldNotBeNull();
        var methodCall = (MethodCallExpression)result.QueryExpression;
        methodCall.Method.Name.ShouldBe("Take");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void SelectPreservesDataSetName()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut.Select(r => r.Name);

        // Assert
        result.DataSetName.ShouldBe("TestDataSet");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void WherePreservesResultType()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");

        // Act
        var result = sut.Where(r => r.Age > 18);

        // Assert
        result.ResultType.ShouldBe(typeof(TestRecord));
        result.SourceType.ShouldBe(typeof(TestRecord));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void QueryableSourceGetEnumeratorThrowsNotSupportedException()
    {
        // Arrange
        var queryableSource = new System.Reflection.FieldInfo[0];
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");
        var constant = (System.Linq.Expressions.ConstantExpression)sut.QueryExpression;
        var source = (System.Linq.IQueryable<TestRecord>)constant.Value!;

        // Act & Assert
        Should.Throw<NotSupportedException>(() => source.GetEnumerator());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void QueryableSourceNonGenericGetEnumeratorThrowsNotSupportedException()
    {
        // Arrange
        var sut = new DataQueryBuilder<TestRecord>("TestDataSet");
        var constant = (System.Linq.Expressions.ConstantExpression)sut.QueryExpression;
        var source = (System.Collections.IEnumerable)constant.Value!;

        // Act & Assert
        Should.Throw<NotSupportedException>(() => source.GetEnumerator());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InternalConstructorThrowsWhenDataSetNameIsNull()
    {
        // Arrange
        var constructorInfo = typeof(DataQueryBuilder<TestRecord>)
            .GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(Expression), typeof(Type) },
                null);

        constructorInfo.ShouldNotBeNull();

        var expression = Expression.Constant(42);

        // Act & Assert
        var ex = Should.Throw<System.Reflection.TargetInvocationException>(() =>
            constructorInfo.Invoke(new object?[] { null, expression, typeof(TestRecord) }));

        ex.InnerException.ShouldBeOfType<ArgumentNullException>();
        ((ArgumentNullException)ex.InnerException!).ParamName.ShouldBe("dataSetName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void InternalConstructorThrowsWhenExpressionIsNull()
    {
        // Arrange
        var constructorInfo = typeof(DataQueryBuilder<TestRecord>)
            .GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(string), typeof(Expression), typeof(Type) },
                null);

        constructorInfo.ShouldNotBeNull();

        // Act & Assert
        var ex = Should.Throw<System.Reflection.TargetInvocationException>(() =>
            constructorInfo.Invoke(new object?[] { "TestDataSet", null, typeof(TestRecord) }));

        ex.InnerException.ShouldBeOfType<ArgumentNullException>();
        ((ArgumentNullException)ex.InnerException!).ParamName.ShouldBe("expression");
    }
}
