using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// A bulk insert runs one of two ways, the serial INSERT statements or the bulk copy (FDW-1152), and which one a command takes
/// must not change what is stored (FDW-1173): the same rows give the same value for every column on both routes, and a request
/// never carries more parameters than SQL Server accepts.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlInsertRoutesAgreeTests
{
    private readonly MsSqlInsertTranslator _sut = new();

    private static Mock<IField> CreateField(string name, Type clrType, bool isIdentity = false)
    {
        var fieldType = new Mock<IFieldType>();
        fieldType.Setup(f => f.ClrType).Returns(clrType);
        fieldType.Setup(f => f.TypeName).Returns(clrType.Name);

        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(isIdentity);
        field.Setup(f => f.IsNullable).Returns(true);
        field.Setup(f => f.FieldType).Returns(fieldType.Object);
        return field;
    }

    private static Mock<IStorageContainer> CreateContainer(params IField[] fields)
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("Brewery");
        container.Setup(c => c.Path).Returns(new DatabasePath("", "BreweryData", "Brewery"));
        container.Setup(c => c.Schema).Returns(schema.Object);
        return container;
    }

    private static Mock<IDataCommandWithInput> CreateCommand(IEnumerable<object> rows)
    {
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(rows);
        return command;
    }

    private static IField[] BreweryFields() =>
    [
        CreateField("Id", typeof(Guid), isIdentity: true).Object,
        CreateField("BreweryId", typeof(string)).Object,
        CreateField("Name", typeof(string)).Object,
        CreateField("Rank", typeof(int)).Object
    ];

    private static List<object> DictionaryRows() =>
    [
        new Dictionary<string, object?> { ["BreweryId"] = "b-1", ["Name"] = "One", ["Rank"] = 1 },
        new Dictionary<string, object?> { ["BreweryId"] = "b-2", ["Name"] = null, ["Rank"] = 2 },
        new Dictionary<string, object?> { ["breweryid"] = "b-3", ["name"] = "Three" }
    ];

    // The value each route gives row r, column c: the serial route as the bound parameter, the bulk route as the DataTable cell.
    private async Task AssertBothRoutesStoreTheSameRows(IField[] fields, List<object> rows)
    {
        var container = CreateContainer(fields);
        var command = CreateCommand(rows);
        var insertable = fields.Where(f => !f.IsIdentity).ToList();

        var serial = await _sut.Translate(command.Object, container.Object, TestContext.Current.CancellationToken);
        var plan = MsSqlInsertTranslator.Plan(command.Object, container.Object);

        serial.IsSuccess.ShouldBeTrue();
        plan.IsSuccess.ShouldBeTrue();
        plan.Value!.Rows.Rows.Count.ShouldBe(rows.Count);
        for (var row = 0; row < rows.Count; row++)
        {
            for (var column = 0; column < insertable.Count; column++)
            {
                var bound = serial.Value!.Parameters[$"@p{(row * insertable.Count) + column}"].Value;
                var copied = plan.Value.Rows.Rows[row][insertable[column].Name];
                copied.ShouldBe(bound, $"row {row}, column {insertable[column].Name}");
            }
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task DictionaryRowsStoreTheSameValuesOnBothRoutes()
        => await AssertBothRoutesStoreTheSameRows(BreweryFields(), DictionaryRows());

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task EntityRowsStoreTheSameValuesOnBothRoutes()
        => await AssertBothRoutesStoreTheSameRows(
            BreweryFields(),
            [new { BreweryId = "b-1", Name = "One", Rank = 1 }, new { BreweryId = "b-2", Name = (string?)null, Rank = 2 }]);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADictionaryRowCarriesItsValuesToTheSerialStatement()
    {
        // The pipeline's rows are dictionaries: the serial statement used to read properties only and bound NULL for every
        // column, so the first NOT NULL column refused the insert (FDW-1173).
        var container = CreateContainer(BreweryFields());

        var result = await _sut.Translate(CreateCommand(DictionaryRows()).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters["@p0"].Value.ShouldBe("b-1");
        result.Value.Parameters["@p1"].Value.ShouldBe("One");
        result.Value.Parameters["@p2"].Value.ShouldBe(1);
        result.Value.Parameters["@p4"].Value.ShouldBe(DBNull.Value);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANullInADictionaryRowForABinaryColumnIsBoundAsVarBinary()
    {
        var container = CreateContainer(CreateField("Name", typeof(string)).Object, CreateField("TokenHash", typeof(byte[])).Object);
        var rows = new List<object> { new Dictionary<string, object?> { ["Name"] = "Test", ["TokenHash"] = null } };

        var result = await _sut.Translate(CreateCommand(rows).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters["@p1"].SqlDbType.ShouldBe(SqlDbType.VarBinary);
        result.Value.Parameters["@p1"].Value.ShouldBe(DBNull.Value);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ARequestCarriesAsManyRowsAsKeepItWithinTheParameterLimit()
    {
        // Fifteen insertable columns, as the earthquake table has: 2100 / 15 = 140 rows.
        var fields = Enumerable.Range(0, 15).Select(i => CreateField($"C{i}", typeof(string)).Object)
            .Prepend(CreateField("Id", typeof(Guid), isIdentity: true).Object)
            .ToArray();

        MsSqlInsertTranslator.RowsPerRequest(CreateContainer(fields).Object).ShouldBe(140);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerWithNoInsertableColumnHasNoRowLimitOfItsOwn()
    {
        var container = CreateContainer(CreateField("Id", typeof(Guid), isIdentity: true).Object);

        MsSqlInsertTranslator.RowsPerRequest(container.Object).ShouldBe(int.MaxValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARequestOfExactlyTheLimitIsTranslated()
    {
        var fields = Enumerable.Range(0, 15).Select(i => CreateField($"C{i}", typeof(string)).Object).ToArray();
        var rows = Enumerable.Range(0, 140).Select(i => (object)fields.ToDictionary(f => f.Name, f => (object?)$"v{i}")).ToList();

        var result = await _sut.Translate(CreateCommand(rows).Object, CreateContainer(fields).Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters.Count.ShouldBe(2100);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARequestOverTheLimitIsRefusedNotSentToTheServer()
    {
        var fields = Enumerable.Range(0, 15).Select(i => CreateField($"C{i}", typeof(string)).Object).ToArray();
        var rows = Enumerable.Range(0, 141).Select(i => (object)fields.ToDictionary(f => f.Name, f => (object?)$"v{i}")).ToList();

        var result = await _sut.Translate(CreateCommand(rows).Object, CreateContainer(fields).Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
