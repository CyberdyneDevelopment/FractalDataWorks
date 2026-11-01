using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests.Translators;

/// <summary>
/// The serial route of a PostgreSQL insert (FDW-1173): a dictionary row, which is what a pipeline load carries, binds its
/// values as an entity row does, and a command never carries more parameters than its limit.
/// </summary>
[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlInsertSerialRouteTests
{
    private readonly PostgreSqlInsertTranslator _sut = new();

    private static IField CreateField(string name, bool isIdentity = false)
    {
        var fieldType = new Mock<IFieldType>();
        fieldType.Setup(f => f.ClrType).Returns(typeof(string));
        fieldType.Setup(f => f.TypeName).Returns("String");

        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(isIdentity);
        field.Setup(f => f.FieldType).Returns(fieldType.Object);
        return field.Object;
    }

    private static Mock<IStorageContainer> CreateContainer(params IField[] fields)
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("brewery");
        container.Setup(c => c.Path).Returns(new PostgreSqlDatabasePath(null, "public", "brewery"));
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

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADictionaryRowCarriesItsValuesToTheSerialStatement()
    {
        var container = CreateContainer(CreateField("id", isIdentity: true), CreateField("brewery_id"), CreateField("name"));
        var rows = new List<object>
        {
            new Dictionary<string, object?> { ["brewery_id"] = "b-1", ["name"] = "One" },
            new Dictionary<string, object?> { ["BREWERY_ID"] = "b-2", ["name"] = null }
        };

        var result = await _sut.Translate(CreateCommand(rows).Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters.Count.ShouldBe(4);
        result.Value.Parameters[0].Value.ShouldBe("b-1");
        result.Value.Parameters[1].Value.ShouldBe("One");
        result.Value.Parameters[2].Value.ShouldBe("b-2");
        result.Value.Parameters[3].Value.ShouldBe(DBNull.Value);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ACommandCarriesAsManyRowsAsKeepItWithinTheParameterLimit()
    {
        var fields = Enumerable.Range(0, 15).Select(i => CreateField($"c{i}")).Prepend(CreateField("id", isIdentity: true)).ToArray();

        PostgreSqlInsertTranslator.RowsPerRequest(CreateContainer(fields).Object).ShouldBe(32767 / 15);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACommandOverTheParameterLimitIsRefusedNotSentToTheServer()
    {
        var fields = Enumerable.Range(0, 15).Select(i => CreateField($"c{i}")).ToArray();
        var perCommand = PostgreSqlInsertTranslator.RowsPerRequest(CreateContainer(fields).Object);
        var rows = Enumerable.Range(0, perCommand + 1)
            .Select(i => (object)fields.ToDictionary(f => f.Name, f => (object?)$"v{i}"))
            .ToList();

        var result = await _sut.Translate(CreateCommand(rows).Object, CreateContainer(fields).Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
