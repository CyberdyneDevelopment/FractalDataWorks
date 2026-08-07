using System.Data;
using Fdw.Services.EtlMappers.Dynamic;
using Fdw;
using Fdw.Services;
using Fdw.Services.EtlMappers;

namespace Fdw.Services.EtlMappers.Dynamic.Tests;

public class CompiledFieldAccessorTests
{
    private static DataTable CreateTestTable(params (string name, Type type)[] columns)
    {
        var table = new DataTable();
        foreach (var (name, type) in columns)
        {
            table.Columns.Add(name, type);
        }
        return table;
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsFieldName()
    {
        var sut = new CompiledFieldAccessor("Name", 0);

        sut.FieldName.ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ConstructorSetsOrdinal()
    {
        var sut = new CompiledFieldAccessor("Name", 3);

        sut.Ordinal.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void GetValueReturnsNullForNegativeOrdinal()
    {
        var table = CreateTestTable(("Name", typeof(string)));
        table.Rows.Add("test");
        using var reader = table.CreateDataReader();
        reader.Read();

        var sut = new CompiledFieldAccessor("Missing", -1);

        sut.GetValue(reader).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void GetValueReturnsValueFromReader()
    {
        var table = CreateTestTable(("Name", typeof(string)));
        table.Rows.Add("TestValue");
        using var reader = table.CreateDataReader();
        reader.Read();

        var sut = new CompiledFieldAccessor("Name", 0);

        sut.GetValue(reader).ShouldBe("TestValue");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void GetValueReturnsNullForDbNull()
    {
        var table = CreateTestTable(("Name", typeof(string)));
        table.Rows.Add(DBNull.Value);
        using var reader = table.CreateDataReader();
        reader.Read();

        var sut = new CompiledFieldAccessor("Name", 0);

        sut.GetValue(reader).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void GetValueReturnsIntValue()
    {
        var table = CreateTestTable(("Age", typeof(int)));
        table.Rows.Add(42);
        using var reader = table.CreateDataReader();
        reader.Read();

        var sut = new CompiledFieldAccessor("Age", 0);

        sut.GetValue(reader).ShouldBe(42);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void GetValueReturnsDateTimeValue()
    {
        var now = DateTime.UtcNow;
        var table = CreateTestTable(("Created", typeof(DateTime)));
        table.Rows.Add(now);
        using var reader = table.CreateDataReader();
        reader.Read();

        var sut = new CompiledFieldAccessor("Created", 0);

        sut.GetValue(reader).ShouldBe(now);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MultipleAccessorsCanCoexist()
    {
        var table = CreateTestTable(
            ("Name", typeof(string)),
            ("Age", typeof(int)));
        table.Rows.Add("Alice", 30);
        using var reader = table.CreateDataReader();
        reader.Read();

        var accessor1 = new CompiledFieldAccessor("Name", 0);
        var accessor2 = new CompiledFieldAccessor("Age", 1);
        var accessor3 = new CompiledFieldAccessor("Missing", -1);

        accessor1.GetValue(reader).ShouldBe("Alice");
        accessor2.GetValue(reader).ShouldBe(30);
        accessor3.GetValue(reader).ShouldBeNull();
    }
}
