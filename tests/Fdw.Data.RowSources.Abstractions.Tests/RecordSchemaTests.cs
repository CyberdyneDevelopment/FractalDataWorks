using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Abstractions.Tests;

public class RecordSchemaTests
{
    private static IReadOnlyList<IDataField> TwoFields()
    {
        var id = new Mock<IDataField>();
        id.Setup(f => f.Name).Returns("Id");
        var name = new Mock<IDataField>();
        name.Setup(f => f.Name).Returns("Name");
        return new List<IDataField> { id.Object, name.Object };
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FieldCountMatchesFieldList()
    {
        var sut = new RecordSchema(TwoFields());

        sut.FieldCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldOrdinalIsCaseInsensitive()
    {
        var sut = new RecordSchema(TwoFields());

        sut.GetFieldOrdinal("name").ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldOrdinalReturnsNegativeOneForMissing()
    {
        var sut = new RecordSchema(TwoFields());

        sut.GetFieldOrdinal("Absent").ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void GetFieldNameReturnsNameAtOrdinal()
    {
        var sut = new RecordSchema(TwoFields());

        sut.GetFieldName(0).ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsOnNullFields()
        => Should.Throw<ArgumentNullException>(() => new RecordSchema(null!));
}
