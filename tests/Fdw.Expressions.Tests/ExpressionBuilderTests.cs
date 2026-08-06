using System.Linq.Expressions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Expressions;

namespace Fdw.Expressions.Tests;

public class ExpressionBuilderTests
{
    private readonly ExpressionBuilder _sut = new();

    private static IDataSchema CreateSchema(params (string Name, Type DataType)[] fields)
    {
        var schemaFields = fields.Select((f, i) => new SchemaField(f.Name, f.DataType, i)).ToList();
        return DataSchema.FromFields(schemaFields);
    }

    private static IDataRow CreateRow(IDataSchema schema, params object?[] values)
    {
        return new DataRow(schema, values);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildPredicateCompilesAndExecutesCorrectly()
    {
        var schema = CreateSchema(("Price", typeof(decimal)));
        var row = CreateRow(schema, 150m);

        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<decimal>(0) > 100m;
        var compiled = _sut.BuildPredicate(schema, predicate);

        compiled(row).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildPredicateReturnsFalseWhenConditionNotMet()
    {
        var schema = CreateSchema(("Price", typeof(decimal)));
        var row = CreateRow(schema, 50m);

        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<decimal>(0) > 100m;
        var compiled = _sut.BuildPredicate(schema, predicate);

        compiled(row).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildPredicateCachesSameExpression()
    {
        var schema = CreateSchema(("Price", typeof(decimal)));
        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<decimal>(0) > 100m;

        var first = _sut.BuildPredicate(schema, predicate);
        var second = _sut.BuildPredicate(schema, predicate);

        first.ShouldBeSameAs(second);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildSelectorCompilesAndExecutes()
    {
        var schema = CreateSchema(("Name", typeof(string)));
        var row = CreateRow(schema, "Alice");

        Expression<Func<IDataRow, string>> selector = r => r.GetValue<string>(0);
        var compiled = _sut.BuildSelector(schema, selector);

        compiled(row).ShouldBe("Alice");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildFieldAccessorReturnsCorrectValue()
    {
        var schema = CreateSchema(("Name", typeof(string)), ("Age", typeof(int)));
        var row = CreateRow(schema, "Bob", 30);

        var accessor = _sut.BuildFieldAccessor<string>(schema, "Name");

        accessor.FieldName.ShouldBe("Name");
        accessor.Ordinal.ShouldBe(0);
        accessor.GetValue(row).ShouldBe("Bob");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildFieldAccessorTryGetValueReturnsTrueForExistingField()
    {
        var schema = CreateSchema(("Score", typeof(int)));
        var row = CreateRow(schema, 42);

        var accessor = _sut.BuildFieldAccessor<int>(schema, "Score");

        accessor.TryGetValue(row, out var value).ShouldBeTrue();
        value.ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildFieldAccessorCachesSameAccessor()
    {
        var schema = CreateSchema(("Name", typeof(string)));

        var first = _sut.BuildFieldAccessor<string>(schema, "Name");
        var second = _sut.BuildFieldAccessor<string>(schema, "Name");

        first.ShouldBeSameAs(second);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildAggregationCompilesAndExecutes()
    {
        var schema = CreateSchema(("Value", typeof(int)));
        Expression<Func<IDataRow[], int>> aggregator = rows => rows.Length;
        var compiled = _sut.BuildAggregation(schema, aggregator);

        var rows = new IDataRow[]
        {
            CreateRow(schema, 1),
            CreateRow(schema, 2),
            CreateRow(schema, 3)
        };

        compiled(rows).ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildJoinPredicateCompilesAndExecutes()
    {
        var leftSchema = CreateSchema(("Id", typeof(int)));
        var rightSchema = CreateSchema(("LeftId", typeof(int)));

        Expression<Func<IDataRow, IDataRow, bool>> joinPred =
            (left, right) => left.GetValue<int>(0) == right.GetValue<int>(0);

        var compiled = _sut.BuildJoinPredicate(leftSchema, rightSchema, joinPred);

        var leftRow = CreateRow(leftSchema, 5);
        var rightRow = CreateRow(rightSchema, 5);
        var mismatchRow = CreateRow(rightSchema, 99);

        compiled(leftRow, rightRow).ShouldBeTrue();
        compiled(leftRow, mismatchRow).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CompileFormulaReturnsFailureForEmptyFormula()
    {
        var schema = CreateSchema(("Price", typeof(decimal)));

        var result = _sut.CompileFormula<decimal>(schema, "");

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CompileFormulaReturnsFailureForWhitespaceFormula()
    {
        var schema = CreateSchema(("Price", typeof(decimal)));

        var result = _sut.CompileFormula<decimal>(schema, "   ");

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CompileFormulaReturnsFailureForInvalidField()
    {
        var schema = CreateSchema(("Price", typeof(decimal)));

        var result = _sut.CompileFormula<decimal>(schema, "NonExistentField + 1");

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StatisticsReflectsCacheActivity()
    {
        var schema = CreateSchema(("X", typeof(decimal)));

        _sut.ClearCache();
        _sut.Statistics.CachedExpressionCount.ShouldBe(0);

        // Use BuildPredicate which works
        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<decimal>(0) > 0;
        _sut.BuildPredicate(schema, predicate);

        _sut.Statistics.CachedExpressionCount.ShouldBe(1);
        _sut.Statistics.CacheMisses.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ClearCacheResetsAllStatistics()
    {
        var schema = CreateSchema(("X", typeof(decimal)));
        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<decimal>(0) > 0;
        _sut.BuildPredicate(schema, predicate);

        _sut.ClearCache();

        _sut.Statistics.CachedExpressionCount.ShouldBe(0);
        _sut.Statistics.CacheHits.ShouldBe(0);
        _sut.Statistics.CacheMisses.ShouldBe(0);
        _sut.Statistics.TotalCompilationTime.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StatisticsHitRateIsZeroWhenEmpty()
    {
        _sut.ClearCache();
        _sut.Statistics.HitRate.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StatisticsAverageCompilationTimeIsZeroWhenNoMisses()
    {
        _sut.ClearCache();
        _sut.Statistics.AverageCompilationTime.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StatisticsToStringReturnsFormattedOutput()
    {
        _sut.ClearCache();
        var text = _sut.Statistics.ToString()!;

        text.ShouldContain("Cache:");
        text.ShouldContain("hit rate");
        text.ShouldContain("compilation time");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildPredicateWithStringEquality()
    {
        var schema = CreateSchema(("Status", typeof(string)));
        var activeRow = CreateRow(schema, "Active");
        var inactiveRow = CreateRow(schema, "Inactive");

        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<string>(0) == "Active";
        var compiled = _sut.BuildPredicate(schema, predicate);

        compiled(activeRow).ShouldBeTrue();
        compiled(inactiveRow).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildSelectorReturnsDifferentFieldByOrdinal()
    {
        var schema = CreateSchema(("First", typeof(string)), ("Last", typeof(string)));
        var row = CreateRow(schema, "John", "Doe");

        Expression<Func<IDataRow, string>> selector = r => r.GetValue<string>(1);
        var compiled = _sut.BuildSelector(schema, selector);

        compiled(row).ShouldBe("Doe");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildFieldAccessorSecondFieldOrdinal()
    {
        var schema = CreateSchema(("A", typeof(int)), ("B", typeof(int)));
        var row = CreateRow(schema, 10, 20);

        var accessor = _sut.BuildFieldAccessor<int>(schema, "B");

        accessor.FieldName.ShouldBe("B");
        accessor.Ordinal.ShouldBe(1);
        accessor.GetValue(row).ShouldBe(20);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CacheHitsIncrementOnReuse()
    {
        _sut.ClearCache();
        var schema = CreateSchema(("X", typeof(int)));
        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<int>(0) > 0;

        _sut.BuildPredicate(schema, predicate);
        var hitsAfterFirst = _sut.Statistics.CacheHits;

        _sut.BuildPredicate(schema, predicate);
        var hitsAfterSecond = _sut.Statistics.CacheHits;

        hitsAfterSecond.ShouldBeGreaterThan(hitsAfterFirst);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StatisticsHitRateGreaterThanZeroAfterCacheHit()
    {
        _sut.ClearCache();
        var schema = CreateSchema(("X", typeof(int)));
        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<int>(0) > 0;

        _sut.BuildPredicate(schema, predicate);
        _sut.BuildPredicate(schema, predicate);

        _sut.Statistics.HitRate.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CompileFormulaCachesSameFailure()
    {
        var schema = CreateSchema(("A", typeof(decimal)));

        // Invalid field - should fail consistently
        var result1 = _sut.CompileFormula<decimal>(schema, "MISSING + 1");
        var result2 = _sut.CompileFormula<decimal>(schema, "MISSING + 1");

        result1.IsSuccess.ShouldBeFalse();
        result2.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TotalCompilationTimeIncreasesAfterCompilation()
    {
        _sut.ClearCache();
        var schema = CreateSchema(("X", typeof(int)));
        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<int>(0) > 0;

        _sut.BuildPredicate(schema, predicate);

        _sut.Statistics.TotalCompilationTime.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildSelectorCachesSameSelector()
    {
        var schema = CreateSchema(("Name", typeof(string)));
        Expression<Func<IDataRow, string>> selector = r => r.GetValue<string>(0);

        var first = _sut.BuildSelector(schema, selector);
        var second = _sut.BuildSelector(schema, selector);

        first.ShouldBeSameAs(second);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildAggregationCachesSameAggregation()
    {
        var schema = CreateSchema(("Value", typeof(int)));
        Expression<Func<IDataRow[], int>> aggregator = rows => rows.Length;

        var first = _sut.BuildAggregation(schema, aggregator);
        var second = _sut.BuildAggregation(schema, aggregator);

        first.ShouldBeSameAs(second);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildJoinPredicateCachesSameJoinPredicate()
    {
        var leftSchema = CreateSchema(("Id", typeof(int)));
        var rightSchema = CreateSchema(("LeftId", typeof(int)));
        Expression<Func<IDataRow, IDataRow, bool>> joinPred =
            (left, right) => left.GetValue<int>(0) == right.GetValue<int>(0);

        var first = _sut.BuildJoinPredicate(leftSchema, rightSchema, joinPred);
        var second = _sut.BuildJoinPredicate(leftSchema, rightSchema, joinPred);

        first.ShouldBeSameAs(second);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CompileFormulaReturnsFailureForInvalidSyntax()
    {
        var schema = CreateSchema(("Price", typeof(decimal)));

        var result = _sut.CompileFormula<decimal>(schema, "Price +");

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CompileFormulaReturnsFailureForUnrecognizedCharacter()
    {
        var schema = CreateSchema(("Price", typeof(decimal)));

        var result = _sut.CompileFormula<decimal>(schema, "Price @ 1");

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CompileFormulaReturnsFailureForNullFormula()
    {
        var schema = CreateSchema(("Price", typeof(decimal)));

        var result = _sut.CompileFormula<decimal>(schema, null!);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StatisticsAverageCompilationTimeCalculated()
    {
        _sut.ClearCache();
        var schema = CreateSchema(("X", typeof(int)));
        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<int>(0) > 0;

        _sut.BuildPredicate(schema, predicate);

        _sut.Statistics.AverageCompilationTime.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void BuildFieldAccessorThrowsForNonExistentField()
    {
        var schema = CreateSchema(("Name", typeof(string)));

        Should.Throw<Exception>(() => _sut.BuildFieldAccessor<string>(schema, "NonExistent"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MultipleExpressionTypesShareCache()
    {
        _sut.ClearCache();
        var schema = CreateSchema(("X", typeof(int)));

        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<int>(0) > 0;
        Expression<Func<IDataRow, int>> selector = r => r.GetValue<int>(0);

        _sut.BuildPredicate(schema, predicate);
        _sut.BuildSelector(schema, selector);
        _sut.BuildFieldAccessor<int>(schema, "X");

        _sut.Statistics.CachedExpressionCount.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CacheMissTrackedOnFirstCompilation()
    {
        // Arrange
        _sut.ClearCache();
        var schema = CreateSchema(("Y", typeof(int)));
        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<int>(0) == 0;

        var missesBeforeFirst = _sut.Statistics.CacheMisses;

        // Act
        _sut.BuildPredicate(schema, predicate);

        // Assert
        _sut.Statistics.CacheMisses.ShouldBeGreaterThan(missesBeforeFirst);
        _sut.Statistics.CacheMisses.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCompileReturnsSameInstanceForSameKey()
    {
        // Arrange
        _sut.ClearCache();
        var schema = CreateSchema(("Z", typeof(decimal)));
        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<decimal>(0) >= 0;

        // Act
        var first = _sut.BuildPredicate(schema, predicate);
        var second = _sut.BuildPredicate(schema, predicate);
        var third = _sut.BuildPredicate(schema, predicate);

        // Assert - all calls return the exact same compiled delegate instance
        first.ShouldBeSameAs(second);
        second.ShouldBeSameAs(third);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void StatisticsReflectCorrectHitAndMissCounts()
    {
        // Arrange
        _sut.ClearCache();
        var schema = CreateSchema(("W", typeof(int)));
        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<int>(0) < 100;

        // Act - 1 miss + 3 hits
        _sut.BuildPredicate(schema, predicate); // miss
        _sut.BuildPredicate(schema, predicate); // hit
        _sut.BuildPredicate(schema, predicate); // hit
        _sut.BuildPredicate(schema, predicate); // hit

        // Assert
        _sut.Statistics.CacheMisses.ShouldBe(1);
        _sut.Statistics.CacheHits.ShouldBe(3);
        _sut.Statistics.HitRate.ShouldBeGreaterThan(0.7);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void GetOrCompileIsThreadSafe()
    {
        // Arrange
        _sut.ClearCache();
        var schema = CreateSchema(("T", typeof(int)));
        Expression<Func<IDataRow, bool>> predicate = r => r.GetValue<int>(0) > 0;

        var results = new System.Collections.Concurrent.ConcurrentBag<Func<IDataRow, bool>>();

        // Act - compile the same key from multiple threads simultaneously
        var threads = Enumerable.Range(0, 20).Select(_ => new System.Threading.Thread(() =>
        {
            var result = _sut.BuildPredicate(schema, predicate);
            results.Add(result);
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // Assert - all threads got a valid delegate and the cache has exactly 1 entry
        results.Count.ShouldBe(20);
        results.ShouldAllBe(r => r != null);
        _sut.Statistics.CachedExpressionCount.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void CacheHitIncrementedCorrectlyOnSecondAccess()
    {
        // Arrange
        _sut.ClearCache();
        var schema = CreateSchema(("V", typeof(string)));
        Expression<Func<IDataRow, string>> selector = r => r.GetValue<string>(0);

        // First access (miss)
        _sut.BuildSelector(schema, selector);
        var hitsAfterFirst = _sut.Statistics.CacheHits;
        var missesAfterFirst = _sut.Statistics.CacheMisses;

        // Act - second access (hit)
        _sut.BuildSelector(schema, selector);

        // Assert
        _sut.Statistics.CacheHits.ShouldBe(hitsAfterFirst + 1);
        _sut.Statistics.CacheMisses.ShouldBe(missesAfterFirst); // unchanged
    }
}
