using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Fdw.Data.MsSql.Tests.TestSupport;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// The serial statements refuse a value a date column cannot take with the same code as the bulk plan, MSSQL-21030, naming the container, the column and the kind of value, before it
/// reaches SQL Server (FDW-4726).
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlSerialDateValueTests
{
    private static Mock<IField> Field(string name, Type clrType)
    {
        var fieldType = new Mock<IFieldType>();
        fieldType.Setup(f => f.ClrType).Returns(clrType);
        fieldType.Setup(f => f.TypeName).Returns(clrType.Name);
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(false);
        field.Setup(f => f.IsComputed).Returns(false);
        field.Setup(f => f.IsNullable).Returns(false);
        field.Setup(f => f.FieldType).Returns(fieldType.Object);
        return field;
    }

    private static Mock<IStorageContainer> Container(params IField[] fields)
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("Events");
        container.Setup(c => c.Path).Returns(new DatabasePath("", "dbo", "Events"));
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Metadata).Returns(new Dictionary<string, object> { ["SurrogateKeyField"] = "Id" });
        return container;
    }

    private static IDataCommand InsertOf(object entity)
    {
        var command = new Mock<IDataCommandWithInput>();
        command.Setup(c => c.InputData).Returns(new List<object> { entity });
        return command.As<IDataCommand>().Object;
    }

    private static IFilterableCommand UpdateOf(object entity)
    {
        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns((IFilterExpression?)null);
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(entity);
        return command.Object;
    }

    private static void ShouldBeTheCodedRefusal(IGenericResult result, string kind)
    {
        result.IsFailure.ShouldBeTrue();
        result.Code!.Name.ShouldBe("InvalidDateValue");
        result.Details!.Data["Container"].ShouldBe("Events");
        result.Details.Data["Column"].ShouldBe("Day");
        result.Details.Data["ValueKind"].ShouldBe(kind);
    }

    [Theory]
    [InlineData("not a date")]
    [InlineData("")]
    [InlineData("13/45/2026")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnInsertOfTextThatIsNotADateIsRefusedBeforeItReachesSqlServer(string text)
    {
        var container = Container(Field("Name", typeof(string)).Object, Field("Day", typeof(DateOnly)).Object);

        var result = await new MsSqlInsertTranslator().Translate(InsertOf(new { Name = "a", Day = text }), container.Object, TestContext.Current.CancellationToken);

        ShouldBeTheCodedRefusal(result, "String");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnInsertOfAValueOfAnotherTypeInADateColumnIsRefusedNamingItsKind()
    {
        var container = Container(Field("Day", typeof(DateOnly)).Object);

        var result = await new MsSqlInsertTranslator().Translate(InsertOf(new { Day = 42 }), container.Object, TestContext.Current.CancellationToken);

        ShouldBeTheCodedRefusal(result, "Int32");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheRefusalDoesNotCarryTheValue()
    {
        var container = Container(Field("Day", typeof(DateOnly)).Object);

        var result = await new MsSqlInsertTranslator().Translate(InsertOf(new { Day = "secret-not-a-date" }), container.Object, TestContext.Current.CancellationToken);

        result.CurrentMessage.ShouldNotContain("secret-not-a-date");
        result.Details!.Data.Values.ShouldNotContain((object?)"secret-not-a-date");
    }

    [Theory]
    [InlineData("10/27/2026")]
    [InlineData("2026-10-27")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnInsertOfDateTextStillTranslates(string text)
    {
        var container = Container(Field("Day", typeof(DateOnly)).Object);

        var result = await new MsSqlInsertTranslator().Translate(InsertOf(new { Day = text }), container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnInsertOfADateAndOfADateTimeStillTranslates()
    {
        var container = Container(Field("Day", typeof(DateOnly)).Object);

        (await new MsSqlInsertTranslator().Translate(InsertOf(new { Day = new DateOnly(2026, 10, 27) }), container.Object, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await new MsSqlInsertTranslator().Translate(InsertOf(new { Day = new DateTime(2026, 10, 27) }), container.Object, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnInsertOfTextInAColumnThatIsNotADateColumnIsLeftAlone()
    {
        var container = Container(Field("Day", typeof(string)).Object);

        var result = await new MsSqlInsertTranslator().Translate(InsertOf(new { Day = "not a date" }), container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUpdateOfTextThatIsNotADateIsRefusedBeforeItReachesSqlServer()
    {
        var container = Container(Field("Id", typeof(int)).Object, Field("Day", typeof(DateOnly)).Object);

        var result = await new MsSqlUpdateTranslator().Translate(UpdateOf(new { Id = 7, Day = "not a date" }), container.Object, TestContext.Current.CancellationToken);

        ShouldBeTheCodedRefusal(result, "String");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUpdateOfDateTextAndOfAnAbsentDateStillTranslates()
    {
        var container = Container(Field("Id", typeof(int)).Object, Field("Day", typeof(DateOnly)).Object, Field("Note", typeof(string)).Object);

        (await new MsSqlUpdateTranslator().Translate(UpdateOf(new { Id = 7, Day = "10/27/2026" }), container.Object, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await new MsSqlUpdateTranslator().Translate(UpdateOf(new { Id = 7, Note = "n" }), container.Object, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }
}
