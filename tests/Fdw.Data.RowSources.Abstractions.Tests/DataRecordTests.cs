using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Abstractions.Tests;

public class DataRecordTests
{
    private static RecordSchema Schema()
    {
        var id = new Mock<IDataField>();
        id.Setup(f => f.Name).Returns("Id");
        var name = new Mock<IDataField>();
        name.Setup(f => f.Name).Returns("Name");
        return new RecordSchema(new List<IDataField> { id.Object, name.Object });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IndexByOrdinalReadsValue()
    {
        var sut = new DataRecord(Schema(), [1L, "Alice"]);

        sut[0].ShouldBe(1L);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IndexByNameReadsValueThroughSchema()
    {
        var sut = new DataRecord(Schema(), [1L, "Alice"]);

        sut["Name"].ShouldBe("Alice");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void IndexByMissingNameReturnsNull()
    {
        var sut = new DataRecord(Schema(), [1L, "Alice"]);

        sut["Absent"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Performance")]
    public void ValuesSpanWindowsOverBackingArray()
    {
        var sut = new DataRecord(Schema(), [1L, "Alice"]);

        // Why: the record exposes a zero-copy ReadOnlySpan window over the value buffer.
        var span = sut.Values;
        span.Length.ShouldBe(2);
        span[1].ShouldBe("Alice");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenValueCountMismatchesSchema()
        => Should.Throw<ArgumentException>(() => new DataRecord(Schema(), [1L]));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ToDictionaryProjectsNamesToValues()
    {
        var map = new DataRecord(Schema(), [1L, "Alice"]).ToDictionary();

        map["Id"].ShouldBe(1L);
        map["Name"].ShouldBe("Alice");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void EqualityIsReferenceOfSchemaAndValues()
    {
        var schema = Schema();
        var values = new object?[] { 1L, "Alice" };
        var a = new DataRecord(schema, values);
        var b = new DataRecord(schema, values);

        (a == b).ShouldBeTrue();
    }
}
