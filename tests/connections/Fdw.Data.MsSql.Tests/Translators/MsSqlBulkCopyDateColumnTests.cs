using System;
using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Results;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// A bulk copy fits each value to its DataTable column (FDW-4723): the same day arrives as a DateOnly, a DateTime or a DateTimeOffset, and a DataColumn refuses a value of any other
/// type ("Expected type is DateOnly").
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlBulkCopyDateColumnTests
{
    private static readonly DateOnly Day = new(2026, 10, 27);

    private static Mock<IField> Field(string name, Type clrType, string typeName)
    {
        var fieldType = new Mock<IFieldType>();
        fieldType.Setup(f => f.ClrType).Returns(clrType);
        fieldType.Setup(f => f.TypeName).Returns(typeName);
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(false);
        field.Setup(f => f.IsComputed).Returns(false);
        field.Setup(f => f.IsNullable).Returns(false);
        field.Setup(f => f.FieldType).Returns(fieldType.Object);
        return field;
    }

    private static IGenericResult<MsSqlBulkCopyPlan> PlanResult(IField field, object entity)
    {
        var path = new DatabasePath("", "dbo", "Events");
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(new IField[] { field });
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(new IField[] { field });
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("Events");
        container.Setup(c => c.Path).Returns(path);
        container.Setup(c => c.Schema).Returns(schema.Object);
        var command = new Mock<IDataCommandWithInput>();
        command.As<IDataCommand>();
        command.Setup(c => c.InputData).Returns(new List<object> { entity });

        return MsSqlInsertTranslator.Plan(command.Object, container.Object);
    }

    private static System.Data.DataTable Plan(IField field, object entity)
    {
        var result = PlanResult(field, entity);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        return result.Value!.Rows;
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADateOnlyColumnTakesADateTime()
    {
        var table = Plan(Field("Day", typeof(DateOnly), "Date").Object, new { Day = new DateTime(2026, 10, 27) });

        table.Columns["Day"]!.DataType.ShouldBe(typeof(DateOnly));
        table.Rows[0]["Day"].ShouldBe(Day);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADateOnlyColumnTakesADateTimeWithATimeOfDay()
    {
        var table = Plan(Field("Day", typeof(DateOnly), "Date").Object, new { Day = new DateTime(2026, 10, 27, 17, 45, 10) });

        table.Rows[0]["Day"].ShouldBe(Day);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADateOnlyColumnTakesADateTimeOffsetsDay()
    {
        var table = Plan(Field("Day", typeof(DateOnly), "Date").Object, new { Day = new DateTimeOffset(2026, 10, 27, 23, 30, 0, TimeSpan.FromHours(-5)) });

        table.Rows[0]["Day"].ShouldBe(Day);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADateOnlyColumnKeepsADateOnlyAsItIs()
    {
        var table = Plan(Field("Day", typeof(DateOnly), "Date").Object, new { Day });

        table.Rows[0]["Day"].ShouldBe(Day);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADateTimeColumnTakesADateOnlyAsMidnight()
    {
        var table = Plan(Field("At", typeof(DateTime), "DateTime").Object, new { At = Day });

        table.Columns["At"]!.DataType.ShouldBe(typeof(DateTime));
        table.Rows[0]["At"].ShouldBe(new DateTime(2026, 10, 27));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ANullWritesAsDbNull()
    {
        var field = Field("Day", typeof(DateOnly), "Date");
        field.Setup(f => f.IsNullable).Returns(true);

        var table = Plan(field.Object, new { Day = (DateTime?)null });

        table.Rows[0]["Day"].ShouldBe(DBNull.Value);
    }

    [Theory]
    [InlineData("10/27/2026")]
    [InlineData("2026-10-27")]
    [InlineData("10/27/2026 00:00:00")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADateOnlyColumnTakesDateText(string text)
    {
        var table = Plan(Field("Day", typeof(DateOnly), "Date").Object, new { Day = text });

        table.Rows[0]["Day"].ShouldBe(Day);
    }

    [Theory]
    [InlineData("not a date")]
    [InlineData("")]
    [InlineData("13/45/2026")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TextThatIsNotADateRefusesThePlanNamingTheContainerColumnAndKind(string text)
    {
        var result = PlanResult(Field("Day", typeof(DateOnly), "Date").Object, new { Day = text });

        result.IsFailure.ShouldBeTrue();
        result.Code!.Name.ShouldBe("InvalidDateValue");
        result.Details!.Data["Container"].ShouldBe("Events");
        result.Details.Data["Column"].ShouldBe("Day");
        result.Details.Data["ValueKind"].ShouldBe("String");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AValueOfAnotherTypeInADateColumnRefusesThePlanNamingItsKind()
    {
        var result = PlanResult(Field("Day", typeof(DateOnly), "Date").Object, new { Day = 42 });

        result.IsFailure.ShouldBeTrue();
        result.Code!.Name.ShouldBe("InvalidDateValue");
        result.Details!.Data["ValueKind"].ShouldBe("Int32");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void TheRefusalDoesNotCarryTheValue()
    {
        var result = PlanResult(Field("Day", typeof(DateOnly), "Date").Object, new { Day = "secret-not-a-date" });

        result.CurrentMessage.ShouldNotBeNull().ShouldNotContain("secret-not-a-date");
        result.Details!.Data.Values.ShouldNotContain((object?)"secret-not-a-date");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ADateTimeColumnTakesDateText()
    {
        var table = Plan(Field("At", typeof(DateTime), "DateTime").Object, new { At = "10/27/2026" });

        table.Rows[0]["At"].ShouldBe(new DateTime(2026, 10, 27));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AColumnThatIsNotADateColumnIsLeftAlone()
    {
        var table = Plan(Field("Name", typeof(string), "String").Object, new { Name = "10/27/2026" });

        table.Rows[0]["Name"].ShouldBe("10/27/2026");
    }
}
