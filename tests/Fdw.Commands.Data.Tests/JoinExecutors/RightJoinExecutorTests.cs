using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions.FieldAccess;
using Fdw.Commands.Data.Abstractions.JoinExecutors;
using Fdw.Commands.Data.FieldAccess;
using Fdw.Commands.Data.Joins;

namespace Fdw.Commands.Data.Tests;

/// <summary>
/// Tests for <see cref="RightJoinExecutor"/>, resolved via the <see cref="JoinExecutors"/> TypeCollection
/// exactly as <c>ResultMerger</c> does at runtime.
/// </summary>
public sealed class RightJoinExecutorTests
{
    private static readonly IFieldValueExtractor Extractor = new DictionaryFieldExtractor();
    private static readonly (string LeftField, string RightField) Condition = ("Id", "CustomerId");

    private readonly IJoinExecutor _sut = JoinExecutors.ByName("Right");

    private sealed record Merged(int? LeftId, string? LeftName, int? RightCustomerId, string? RightTag);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteResolvesFromTypeCollectionByName()
    {
        // Assert
        _sut.ShouldNotBe(JoinExecutors.NotFound);
        _sut.Name.ShouldBe("Right");
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

        // Assert - unmatched right row keeps its own data, left fields default to null
        results.Count.ShouldBe(2);
        results[0].ShouldBe(new Merged(1, "Alice", 1, "O100"));
        results[1].ShouldBe(new Merged(null, null, 99, "Orphan"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteDropsUnmatchedLeftRecordsEntirely()
    {
        // Arrange - RIGHT JOIN semantics: a left row with no matching right key produces NO
        // output row at all (unlike FULL JOIN, which would emit a left-only row).
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice"), LeftRow(2, "Orphan") };
        var right = new List<Dictionary<string, object>> { RightRow(1, "O100") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert
        results.Count.ShouldBe(1);
        results[0].ShouldBe(new Merged(1, "Alice", 1, "O100"));
        results.ShouldNotContain(r => r.LeftName == "Orphan");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsAllCombinationsForDuplicateLeftKeys()
    {
        // Arrange - two left rows share key 1; single right row should fan out to both.
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice1"), LeftRow(1, "Alice2") };
        var right = new List<Dictionary<string, object>> { RightRow(1, "O100") };

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert
        results.Count.ShouldBe(2);
        results[0].ShouldBe(new Merged(1, "Alice1", 1, "O100"));
        results[1].ShouldBe(new Merged(1, "Alice2", 1, "O100"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ExecuteReturnsAllCombinationsForDuplicateRightKeys()
    {
        // Arrange - two right rows share key 1, single matching left row repeats for each.
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
    public void ExecuteTreatsNullKeysAsMatching()
    {
        // Arrange - hash-join semantics (ToLookup) treat null == null, so a null-keyed right
        // row matches a null-keyed left row.
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
    public void ExecuteReturnsEmptyWhenRightIsEmpty()
    {
        // Arrange
        var left = new List<Dictionary<string, object>> { LeftRow(1, "Alice"), LeftRow(2, "Bob") };
        var right = new List<Dictionary<string, object>>();

        // Act
        var results = _sut.Execute(left, right, Extractor, Condition, Merge).ToList();

        // Assert - RIGHT JOIN is driven entirely by the right side; no right rows means no output
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
