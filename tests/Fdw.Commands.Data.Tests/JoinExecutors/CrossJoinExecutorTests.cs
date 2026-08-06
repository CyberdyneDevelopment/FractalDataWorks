using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions.FieldAccess;
using Fdw.Commands.Data.Abstractions.JoinExecutors;
using Fdw.Commands.Data.FieldAccess;
using Fdw.Commands.Data.Joins;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// Tests for <see cref="CrossJoinExecutor"/>, resolved via the <see cref="JoinExecutors"/> TypeCollection
/// exactly as <c>ResultMerger</c> does at runtime.
/// </summary>
/// <remarks>
/// CrossJoinExecutor ignores the join condition entirely (Cartesian product), so the
/// matched/right-only/left-only/duplicate-key/null-key distinctions that matter for the other
/// executors collapse into one property here: every left row pairs with every right row,
/// regardless of key values - including null or duplicate ones.
/// </remarks>
public sealed class CrossJoinExecutorTests
{
    private static readonly IFieldValueExtractor Extractor = new DictionaryFieldExtractor();
    private static readonly (string LeftField, string RightField) Condition = ("Id", "CustomerId");

    private readonly IJoinExecutor _sut = JoinExecutors.ByName("Cross");

    private sealed record Merged(int? LeftId, string? LeftName, int? RightCustomerId, string? RightTag);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteResolvesFromTypeCollectionByName()
    {
        // Assert
        _sut.ShouldNotBe(JoinExecutors.NotFound);
        _sut.Name.ShouldBe("Cross");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsCartesianProductOfAllRowsInLeftThenRightOrder()
    {
        // Arrange
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice"), LeftRow(2, "Bob") };
        var right = new List<Dictionary<string, object>> { RightRow(100, "X"), RightRow(200, "Y") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert - 2x2 = 4 rows, outer loop over left, inner loop over right
        results.Count.ShouldBe(4);
        results[0].ShouldBe(new Merged(1, "Alice", 100, "X"));
        results[1].ShouldBe(new Merged(1, "Alice", 200, "Y"));
        results[2].ShouldBe(new Merged(2, "Bob", 100, "X"));
        results[3].ShouldBe(new Merged(2, "Bob", 200, "Y"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteIgnoresJoinConditionAndIncludesMismatchedAndDuplicateKeys()
    {
        // Arrange - duplicate left keys, and no right key matches any left key; the join
        // condition is never consulted, so every combination is still produced.
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice1"), LeftRow(1, "Alice2") };
        var right = new List<Dictionary<string, object>> { RightRow(999, "NoMatch") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert - full Cartesian product regardless of key (mis)matches
        results.Count.ShouldBe(2);
        results[0].ShouldBe(new Merged(1, "Alice1", 999, "NoMatch"));
        results[1].ShouldBe(new Merged(1, "Alice2", 999, "NoMatch"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteIgnoresNullKeyValues()
    {
        // Arrange - null-keyed rows on both sides; CrossJoinExecutor never calls the field
        // extractor, so null keys cannot suppress or alter the product.
        var left = new List<Dictionary<string, object>> { LeftRowWithNullId("NullKeyLeft") };
        var right = new List<Dictionary<string, object>> { RightRowWithNullCustomerId("NullKeyRight") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldBe(new Merged(null, "NullKeyLeft", null, "NullKeyRight"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsEmptyWhenLeftIsEmpty()
    {
        // Arrange
        var left = new List<Dictionary<string, object>>();
        var right = new List<Dictionary<string, object>> { RightRow(100, "X") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsEmptyWhenRightIsEmpty()
    {
        // Arrange
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice") };
        var right = new List<Dictionary<string, object>>();

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsEmptyWhenBothSidesAreEmpty()
    {
        // Arrange
        var left = new List<Dictionary<string, object>>();
        var right = new List<Dictionary<string, object>>();

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteThrowsWhenLeftRecordsIsNull()
    {
        var right = new List<Dictionary<string, object>> { RightRow(100, "X") };

        Should.Throw<ArgumentNullException>(() =>
            _sut.Execute<Dictionary<string, object>, Dictionary<string, object>, Merged>(
                null!, right, Extractor, Condition, Merge).ToList());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteThrowsWhenRightRecordsIsNull()
    {
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice") };

        Should.Throw<ArgumentNullException>(() =>
            _sut.Execute<Dictionary<string, object>, Dictionary<string, object>, Merged>(
                left, null!, Extractor, Condition, Merge).ToList());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteThrowsWhenFieldExtractorIsNull()
    {
        // Arrange - even though CrossJoinExecutor never calls the extractor, the guard still
        // enforces the argument is non-null.
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice") };
        var right = new List<Dictionary<string, object>> { RightRow(100, "X") };

        Should.Throw<ArgumentNullException>(() =>
            _sut.Execute(left, right, null!, Condition, Merge).ToList());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteThrowsWhenResultSelectorIsNull()
    {
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice") };
        var right = new List<Dictionary<string, object>> { RightRow(100, "X") };

        Should.Throw<ArgumentNullException>(() =>
            _sut.Execute<Dictionary<string, object>, Dictionary<string, object>, Merged>(
                left, right, Extractor, Condition, null!).ToList());
    }

    private static Dictionary<string, object> LeftRow(int id, string name) =>
        new() { ["Id"] = id, ["Name"] = name };

    private static Dictionary<string, object> LeftRowWithNullId(string name) =>
        new() { ["Id"] = null!, ["Name"] = name };

    private static Dictionary<string, object> RightRow(int customerId, string tag) =>
        new() { ["CustomerId"] = customerId, ["Tag"] = tag };

    private static Dictionary<string, object> RightRowWithNullCustomerId(string tag) =>
        new() { ["CustomerId"] = null!, ["Tag"] = tag };

    private static Merged Merge(Dictionary<string, object> left, Dictionary<string, object> right) =>
        new(
            left is null ? null : (int?)left["Id"],
            left is null ? null : left["Name"] as string,
            right is null ? null : (int?)right["CustomerId"],
            right is null ? null : right["Tag"] as string);
}
