using System.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.EtlMappers.Dynamic;
using Microsoft.Extensions.Logging;
using Fdw;
using Fdw.Services;
using Fdw.Services.EtlMappers;

namespace Fdw.Services.EtlMappers.Dynamic.Tests;

public class DynamicStructMapperTests
{
    private readonly Mock<ILogger<DynamicStructMapper>> _mockLogger = new();
    private readonly DynamicStructMapperConfiguration _config = new();

    private DynamicStructMapper CreateSut() => new(_mockLogger.Object, _config);

    private static Mock<IStorageContainer> CreateContainer(params string[] fieldNames)
    {
        var container = new Mock<IStorageContainer>();
        var schema = new Mock<IContainerSchema>();

        var fields = new List<IField>();
        foreach (var name in fieldNames)
        {
            var field = new Mock<IField>();
            field.Setup(f => f.Name).Returns(name);
            fields.Add(field.Object);
        }

        schema.Setup(s => s.Fields).Returns(fields.AsReadOnly());
        container.Setup(c => c.Schema).Returns(schema.Object);
        return container;
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void IsInitializedIsFalseBeforeInitialize()
    {
        var sut = CreateSut();

        sut.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void EstimatedAllocationsPerRowReturnsOne()
    {
        var sut = CreateSut();

        sut.EstimatedAllocationsPerRow.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void InitializeSetsIsInitializedToTrue()
    {
        var sut = CreateSut();
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Age", typeof(int));
        table.Rows.Add("Alice", 30);
        using var reader = table.CreateDataReader();
        reader.Read();
        var container = CreateContainer("Name", "Age");

        sut.Initialize(reader, container.Object);

        sut.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapRowThrowsWhenNotInitialized()
    {
        var sut = CreateSut();
        var table = new DataTable();
        table.Columns.Add("X", typeof(string));
        table.Rows.Add("val");
        using var reader = table.CreateDataReader();

        Should.Throw<InvalidOperationException>(() => sut.MapRow(reader));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapRowReturnsDictionaryWithFieldValues()
    {
        var sut = CreateSut();
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Age", typeof(int));
        table.Rows.Add("Alice", 30);
        using var reader = table.CreateDataReader();
        reader.Read();
        var container = CreateContainer("Name", "Age");

        sut.Initialize(reader, container.Object);
        var row = sut.MapRow(reader);

        row.Count.ShouldBe(2);
        row["Name"].ShouldBe("Alice");
        row["Age"].ShouldBe(30);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapRowHandlesDbNullValues()
    {
        var sut = CreateSut();
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add(DBNull.Value);
        using var reader = table.CreateDataReader();
        reader.Read();
        var container = CreateContainer("Name");

        sut.Initialize(reader, container.Object);
        var row = sut.MapRow(reader);

        row["Name"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapRowHandlesMissingFieldWithNegativeOrdinal()
    {
        var sut = CreateSut();
        var table = new DataTable();
        table.Columns.Add("Other", typeof(string));
        table.Rows.Add("val");
        using var reader = table.CreateDataReader();
        reader.Read();
        var container = CreateContainer("Missing");

        sut.Initialize(reader, container.Object);
        var row = sut.MapRow(reader);

        row["Missing"].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ResetClearsInitializationState()
    {
        var sut = CreateSut();
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add("test");
        using var reader = table.CreateDataReader();
        reader.Read();
        var container = CreateContainer("Name");

        sut.Initialize(reader, container.Object);
        sut.IsInitialized.ShouldBeTrue();

        sut.Reset();

        sut.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ReturnRowDoesNotThrow()
    {
        var sut = CreateSut();
        var row = new Dictionary<string, object?> { ["Name"] = "test" };

        Should.NotThrow(() => sut.ReturnRow(row));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void MapRowUsesOrdinalIgnoreCaseComparer()
    {
        var sut = CreateSut();
        var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        table.Rows.Add("Alice");
        using var reader = table.CreateDataReader();
        reader.Read();
        var container = CreateContainer("Name");

        sut.Initialize(reader, container.Object);
        var row = sut.MapRow(reader);

        row["name"].ShouldBe("Alice");
        row["NAME"].ShouldBe("Alice");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Etl")]
    public void ResetOnUninitializedMapperDoesNotThrow()
    {
        var sut = CreateSut();

        Should.NotThrow(() => sut.Reset());
        sut.IsInitialized.ShouldBeFalse();
    }
}
