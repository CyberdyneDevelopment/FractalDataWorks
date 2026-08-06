using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions.FieldAccess;
using Fdw.Commands.Data.Abstractions.JoinExecutors;
using Fdw.Commands.Data.FieldAccess;
using Fdw.Commands.Data.Joins;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// Tests for <see cref="FullJoinExecutor"/>, resolved via the <see cref="JoinExecutors"/> TypeCollection
/// exactly as <c>ResultMerger</c> does at runtime.
/// </summary>
public sealed class FullJoinExecutorTests
{
    private static readonly IFieldValueExtractor Extractor = new DictionaryFieldExtractor();
    private static readonly (string LeftField, string RightField) Condition = ("Id", "CustomerId");

    private readonly IJoinExecutor _sut = JoinExecutors.ByName("Full");

    private sealed record Merged(int? LeftId, string? LeftName, int? RightCustomerId, string? RightTag);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteResolvesFromTypeCollectionByName()
    {
        // Assert
        _sut.ShouldNotBe(JoinExecutors.NotFound);
        _sut.Name.ShouldBe("Full");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsMatchedRowsForBothSides()
    {
        // Arrange
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice"), LeftRow(2, "Bob") };
        var right = new List<Dictionary<string, object>> { RightRow(1, "O100"), RightRow(2, "O200") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert
        results.Count.ShouldBe(2);
        results[0].ShouldBe(new Merged(1, "Alice", 1, "O100"));
        results[1].ShouldBe(new Merged(2, "Bob", 2, "O200"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsRightOnlyRowWithNullLeftFieldsWhenNoMatch()
    {
        // Arrange
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice") };
        var right = new List<Dictionary<string, object>> { RightRow(1, "O100"), RightRow(99, "Orphan") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert
        results.Count.ShouldBe(2);
        results[0].ShouldBe(new Merged(1, "Alice", 1, "O100"));
        results[1].ShouldBe(new Merged(null, null, 99, "Orphan"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsLeftOnlyRowWithNullRightFieldsWhenNoMatch()
    {
        // Arrange
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice"), LeftRow(2, "Orphan") };
        var right = new List<Dictionary<string, object>> { RightRow(1, "O100") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert
        results.Count.ShouldBe(2);
        results[0].ShouldBe(new Merged(1, "Alice", 1, "O100"));
        results[1].ShouldBe(new Merged(2, "Orphan", null, null));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsAllCombinationsForDuplicateLeftKeysAndDedupesTrackingSet()
    {
        // Arrange - two left rows share key 1; the matched-value HashSet must dedupe the
        // tracked key without dropping either output row from the match loop.
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice1"), LeftRow(1, "Alice2") };
        var right = new List<Dictionary<string, object>> { RightRow(1, "O100") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert - both left rows matched once each, no unmatched-left pass duplicates them again
        results.Count.ShouldBe(2);
        results[0].ShouldBe(new Merged(1, "Alice1", 1, "O100"));
        results[1].ShouldBe(new Merged(1, "Alice2", 1, "O100"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsAllCombinationsForDuplicateRightKeys()
    {
        // Arrange - two right rows share key 1, single matching left row.
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice") };
        var right = new List<Dictionary<string, object>> { RightRow(1, "O100"), RightRow(1, "O101") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert
        results.Count.ShouldBe(2);
        results[0].ShouldBe(new Merged(1, "Alice", 1, "O100"));
        results[1].ShouldBe(new Merged(1, "Alice", 1, "O101"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteTreatsNullKeysAsMatchingAndTracksThemInHashSet()
    {
        // Arrange - left has a null-keyed row and a non-null-keyed row; right has only a
        // null-keyed row. Hash-join semantics (ToLookup/HashSet<object?>) treat null == null,
        // so the null-keyed rows match each other; the non-null left row is left-only.
        var left = new List<Dictionary<string, object>> { LeftRowWithNullId("NullKeyLeft"), LeftRow(5, "Five") };
        var right = new List<Dictionary<string, object>> { RightRowWithNullCustomerId("NullKeyRight") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert - null-key match emitted exactly once (HashSet<object?> correctly tracks
        // null so the unmatched-left pass does not re-emit it), then the unmatched non-null row.
        results.Count.ShouldBe(2);
        results[0].ShouldBe(new Merged(null, "NullKeyLeft", null, "NullKeyRight"));
        results[1].ShouldBe(new Merged(5, "Five", null, null));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsRightOnlyRowsWhenLeftIsEmpty()
    {
        // Arrange
        var left = new List<Dictionary<string, object>>();
        var right = new List<Dictionary<string, object>> { RightRow(1, "O100"), RightRow(2, "O200") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldAllBe(r => r.LeftId == null && r.LeftName == null);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsLeftOnlyRowsWhenRightIsEmpty()
    {
        // Arrange
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice"), LeftRow(2, "Bob") };
        var right = new List<Dictionary<string, object>>();

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldAllBe(r => r.RightCustomerId == null && r.RightTag == null);
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
        var right = new List<Dictionary<string, object>> { RightRow(1, "O100") };

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
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice") };
        var right = new List<Dictionary<string, object>> { RightRow(1, "O100") };

        Should.Throw<ArgumentNullException>(() =>
            _sut.Execute(left, right, null!, Condition, Merge).ToList());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteThrowsWhenResultSelectorIsNull()
    {
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice") };
        var right = new List<Dictionary<string, object>> { RightRow(1, "O100") };

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
