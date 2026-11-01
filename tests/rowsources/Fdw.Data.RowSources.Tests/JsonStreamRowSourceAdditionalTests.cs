using System.Text;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Json.Abstractions;

namespace Fdw.Data.RowSources.Tests;

/// <summary>
/// Additional tests for JsonStreamRowSource covering boundary and edge case paths.
/// </summary>
public sealed class JsonStreamRowSourceAdditionalTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameReturnsEmptyForNegativeOrdinal()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        source.GetFieldName(-1).ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameReturnsEmptyForOutOfRangeOrdinal()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        source.GetFieldName(999).ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldOrdinalReturnsMinusOneForNull()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        source.GetFieldOrdinal(null!).ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldOrdinalReturnsMinusOneForEmpty()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        source.GetFieldOrdinal("").ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task IsNullReturnsTrueForNegativeOrdinal()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        source.IsNull(-1).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task IsNullReturnsTrueForOutOfRangeOrdinal()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        source.IsNull(999).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetValueReturnsNullForNegativeOrdinal()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        source.GetValue(-1).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetValueReturnsNullForOutOfRangeOrdinal()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        source.GetValue(999).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetConvertedValueReturnsNullForNullValue()
    {
        var json = """[{"value": null}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        var mockConverter = new Mock<IDataTypeConverter>();
        var ordinal = source.GetFieldOrdinal("value");
        source.GetConvertedValue(ordinal, mockConverter.Object).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task GetConvertedValueCallsConverterForNonNullValue()
    {
        var json = """[{"value": "test"}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        var mockConverter = new Mock<IDataTypeConverter>();
        mockConverter.Setup(c => c.ToClr("test")).Returns("converted");
        var ordinal = source.GetFieldOrdinal("value");
        source.GetConvertedValue(ordinal, mockConverter.Object).ShouldBe("converted");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadWorksCorrectly()
    {
        var json = """[{"id": 1}, {"id": 2}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.HasCurrentRow.ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(1L);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(2L);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ResetIsNoOp()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);
        await source.Read(TestContext.Current.CancellationToken);

        // Reset is a no-op but should not throw
        source.Reset();
        source.HasCurrentRow.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void DoubleDisposeDoesNotThrow()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var source = new JsonStreamRowSource(stream);
        source.Dispose();
        source.Dispose();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task DisposeAsyncDisposesCorrectly()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var source = new JsonStreamRowSource(stream);
        await source.DisposeAsync();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadHandlesArrayValuesAsRawJsonWhenNotFlattening()
    {
        var json = """[{"tags": [1, 2, 3]}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { FlattenNestedObjects = false };
        using var source = new JsonStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        var value = source.GetValue(source.GetFieldOrdinal("tags")) as string;
        value.ShouldNotBeNull();
        value.ShouldContain("1");
        // Field ordinals for indexed keys must NOT exist when flattening is disabled.
        source.GetFieldOrdinal("tags.0").ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadFlattensArrayByIndexWhenOptionEnabled()
    {
        // Arrange: geometry.coordinates => ["-122.0", "37.0", "8.0"] (doubles)
        var json = """[{"geometry": {"coordinates": [-122.0, 37.0, 8.0]}}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { FlattenNestedObjects = true };
        using var source = new JsonStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert: each element becomes geometry.coordinates.{i} with its typed value
        source.GetFieldOrdinal("geometry.coordinates.0").ShouldBeGreaterThanOrEqualTo(0);
        source.GetFieldOrdinal("geometry.coordinates.1").ShouldBeGreaterThanOrEqualTo(0);
        source.GetFieldOrdinal("geometry.coordinates.2").ShouldBeGreaterThanOrEqualTo(0);

        source.GetValue(source.GetFieldOrdinal("geometry.coordinates.0")).ShouldBe(-122.0);
        source.GetValue(source.GetFieldOrdinal("geometry.coordinates.1")).ShouldBe(37.0);
        source.GetValue(source.GetFieldOrdinal("geometry.coordinates.2")).ShouldBe(8.0);

        // The parent flattened key must NOT be present (no raw-JSON fallback)
        source.GetFieldOrdinal("geometry.coordinates").ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadFlattensTopLevelArrayByIndexWhenOptionEnabled()
    {
        // Arrange: top-level array property (not the RowArrayPath array)
        var json = """[{"tags": ["a", "b", "c"]}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { FlattenNestedObjects = true };
        using var source = new JsonStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert: string elements flattened by index
        source.GetValue(source.GetFieldOrdinal("tags.0")).ShouldBe("a");
        source.GetValue(source.GetFieldOrdinal("tags.1")).ShouldBe("b");
        source.GetValue(source.GetFieldOrdinal("tags.2")).ShouldBe("c");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadFlattensArrayOfObjectsByIndexWhenOptionEnabled()
    {
        // Arrange: array of objects — each element becomes {field}.{i}.{prop}
        var json = """[{"items": [{"id": 1, "name": "first"}, {"id": 2, "name": "second"}]}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { FlattenNestedObjects = true };
        using var source = new JsonStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        // Assert: nested object properties under each index
        source.GetValue(source.GetFieldOrdinal("items.0.id")).ShouldBe(1L);
        source.GetValue(source.GetFieldOrdinal("items.0.name")).ShouldBe("first");
        source.GetValue(source.GetFieldOrdinal("items.1.id")).ShouldBe(2L);
        source.GetValue(source.GetFieldOrdinal("items.1.name")).ShouldBe("second");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadHandlesCustomFlattenSeparator()
    {
        var json = """[{"address": {"city": "NYC"}}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions
        {
            FlattenNestedObjects = true,
            FlattenSeparator = "_"
        };
        using var source = new JsonStreamRowSource(stream, options);
        await source.Read(TestContext.Current.CancellationToken);

        source.GetFieldOrdinal("address_city").ShouldBeGreaterThanOrEqualTo(0);
        source.GetValue(source.GetFieldOrdinal("address_city")).ShouldBe("NYC");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadReturnsFalseForNonObjectArrayElements()
    {
        // Array of primitives instead of objects
        var json = """[1, 2, 3]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        // Non-object elements return false from ReadNextRow
        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadReturnsFalseForNonArrayRoot()
    {
        var json = """{"key": "value"}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task NavigateToPathWithDollarOnly()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { RowArrayPath = "$" };
        using var source = new JsonStreamRowSource(stream, options);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(1L);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task NavigateToPathWithInvalidPathReturnsFalse()
    {
        var json = """{"data": [{"id": 1}]}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions { RowArrayPath = "nonexistent.path" };
        using var source = new JsonStreamRowSource(stream, options);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void FieldCountIsZeroBeforeRead()
    {
        var json = """[{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        source.FieldCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task FieldCountGrowsWithNewFields()
    {
        var json = """[{"a": 1}, {"a": 2, "b": 3}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var source = new JsonStreamRowSource(stream);

        await source.Read(TestContext.Current.CancellationToken);
        source.FieldCount.ShouldBe(1);

        await source.Read(TestContext.Current.CancellationToken);
        source.FieldCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public async Task AllowCommentsOptionWorks()
    {
        var json = """[/* comment */{"id": 1}]""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var options = new JsonRowSourceOptions
        {
            AllowComments = true,
            AllowTrailingCommas = true
        };
        using var source = new JsonStreamRowSource(stream, options);

        (await source.Read(TestContext.Current.CancellationToken)).ShouldBeTrue();
        source.GetValue(source.GetFieldOrdinal("id")).ShouldBe(1L);
    }
}
