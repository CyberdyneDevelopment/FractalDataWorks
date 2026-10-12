using System.Text;
using Fdw.Data.RowSources.Json.Abstractions;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// Tests for the JsonStreamRowSource streaming JSON reader.
/// </summary>
public class JsonStreamRowSourceTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenStreamIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new JsonStreamRowSource(null!));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadReturnsRowsFromJsonArray()
    {
        // Arrange
        var json = """[{"id": 1, "name": "Alice"}, {"id": 2, "name": "Bob"}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        // Act & Assert
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.HasCurrentRow.ShouldBeTrue();
        source.FieldCount.ShouldBe(2);
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(1L);
        source.GetValue(source.GetFieldOrdinal("name")).ShouldBe("Alice");

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(2L);
        source.GetValue(source.GetFieldOrdinal("name")).ShouldBe("Bob");

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
        source.HasCurrentRow.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetFieldNameReturnsCorrectName()
    {
        // Arrange
        var json = """[{"firstName": "Test", "lastName": "User"}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.GetFieldName(0).ShouldBe("firstName");
        source.GetFieldName(1).ShouldBe("lastName");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetFieldOrdinalIsCaseInsensitive()
    {
        // Arrange
        var json = """[{"MyField": "value"}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.GetFieldOrdinal("MyField").ShouldBe(0);
        source.GetFieldOrdinal("myfield").ShouldBe(0);
        source.GetFieldOrdinal("MYFIELD").ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetFieldOrdinalReturnsMinusOneForUnknown()
    {
        // Arrange
        var json = """[{"field": "value"}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.GetFieldOrdinal("unknown").ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task IsNullReturnsTrueForNullValue()
    {
        // Arrange
        var json = """[{"value": null}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.IsNull(source.GetFieldOrdinal("value")).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task IsNullReturnsFalseForNonNullValue()
    {
        // Arrange
        var json = """[{"value": "test"}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.IsNull(source.GetFieldOrdinal("value")).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadSupportsNestedArrayPath()
    {
        // Arrange
        var json = """{"data": {"items": [{"id": 1}, {"id": 2}]}}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { RowArrayPath = "$.data.items" };
        using var source = new JsonStreamRowSource(stream, options);

        // Act & Assert
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(1L);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(2L);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadSupportsArrayIndexSegmentsInPath()
    {
        // Arrange — real-world nesting (ESPN-style): the row array sits under indexed wrappers.
        var json = """{"sports": [{"leagues": [{"teams": [{"id": 1}, {"id": 2}]}]}]}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { RowArrayPath = "sports.0.leagues.0.teams" };
        using var source = new JsonStreamRowSource(stream, options);

        // Act & Assert
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(1L);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(2L);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadReturnsNoRowsForOutOfRangeArrayIndexInPath()
    {
        // Arrange — index 5 does not exist; navigation must fail closed (no rows), not throw.
        var json = """{"sports": [{"teams": [{"id": 1}]}]}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { RowArrayPath = "sports.5.teams" };
        using var source = new JsonStreamRowSource(stream, options);

        // Act & Assert
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadReturnsNoRowsForNonNumericSegmentAgainstArray()
    {
        // Arrange — a property-name segment applied to an ARRAY is a path authoring error.
        var json = """{"sports": [{"teams": [{"id": 1}]}]}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { RowArrayPath = "sports.teams" };
        using var source = new JsonStreamRowSource(stream, options);

        // Act & Assert
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadSupportsNestedArrayPathWithoutDollarPrefix()
    {
        // Arrange
        var json = """{"data": {"items": [{"id": 1}]}}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { RowArrayPath = "data.items" };
        using var source = new JsonStreamRowSource(stream, options);

        // Act & Assert
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(1L);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadParsesNumberTypes()
    {
        // Arrange
        var json = """[{"integer": 42, "decimal": 3.14}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.GetValue(source.GetFieldOrdinal("integer")).ShouldBe(42L);
        source.GetValue(source.GetFieldOrdinal("decimal")).ShouldBe(3.14);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadParsesBooleanTypes()
    {
        // Arrange
        var json = """[{"active": true, "deleted": false}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.GetValue(source.GetFieldOrdinal("active")).ShouldBe(true);
        source.GetValue(source.GetFieldOrdinal("deleted")).ShouldBe(false);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadFlattensNestedObjectsWhenOptionEnabled()
    {
        // Arrange
        var json = """[{"address": {"city": "NYC", "zip": "10001"}}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { FlattenNestedObjects = true };
        using var source = new JsonStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        source.GetFieldOrdinal("address.city").ShouldBeGreaterThanOrEqualTo(0);
        source.GetValue(source.GetFieldOrdinal("address.city")).ShouldBe("NYC");
        source.GetValue(source.GetFieldOrdinal("address.zip")).ShouldBe("10001");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadKeepsNestedObjectsAsJsonWhenNotFlattening()
    {
        // Arrange
        var json = """[{"address": {"city": "NYC"}}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { FlattenNestedObjects = false };
        using var source = new JsonStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert
        var value = source.GetValue(source.GetFieldOrdinal("address")) as string;
        value.ShouldNotBeNull();
        value.ShouldContain("city");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void EstimatedAllocationsPerRowIsOne()
    {
        // Arrange
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        // Assert
        source.EstimatedAllocationsPerRow.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CanResetIsFalse()
    {
        // Arrange
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        // Assert
        source.CanReset.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void HasCurrentRowIsFalseBeforeRead()
    {
        // Arrange
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        // Assert
        source.HasCurrentRow.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadReturnsFalseForEmptyArray()
    {
        // Arrange
        var json = """[]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        // Act & Assert
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}
