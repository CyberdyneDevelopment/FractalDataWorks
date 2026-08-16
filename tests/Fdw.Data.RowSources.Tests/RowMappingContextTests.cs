using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.RowSources.Tests;

public sealed class RowMappingContextTests
{
    private static Mock<IField> CreateField(string name, int? converterTypeId = null)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.ConverterTypeId).Returns(converterTypeId);
        return field;
    }

    private static (Mock<IRecordCursor> Source, Mock<IStorageContainer> Container) CreateMocks(
        params (string Name, int Ordinal, int? ConverterTypeId)[] fields)
    {
        var source = new Mock<IRecordCursor>();
        var container = new Mock<IStorageContainer>();
        var schema = new Mock<IContainerSchema>();
        var fieldList = new List<IField>();

        foreach (var (name, ordinal, converterTypeId) in fields)
        {
            var field = CreateField(name, converterTypeId);
            fieldList.Add(field.Object);
            source.Setup(s => s.GetFieldOrdinal(name)).Returns(ordinal);
        }

        schema.Setup(s => s.Fields).Returns(fieldList);
        schema.Setup(s => s.GetProjectableFields()).Returns(fieldList);
        container.Setup(c => c.Schema).Returns(schema.Object);

        return (source, container);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CreateSetsFieldOrdinalsFromSource()
    {
        var (source, container) = CreateMocks(
            ("Id", 0, null),
            ("Name", 1, null));

        var ctx = RowMappingContext.Create(source.Object, container.Object);

        ctx.FieldCount.ShouldBe(2);
        ctx.FieldOrdinals[0].ShouldBe(0);
        ctx.FieldOrdinals[1].ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CreateSetsFieldNames()
    {
        var (source, container) = CreateMocks(
            ("Id", 0, null),
            ("Name", 1, null));

        var ctx = RowMappingContext.Create(source.Object, container.Object);

        ctx.FieldNames[0].ShouldBe("Id");
        ctx.FieldNames[1].ShouldBe("Name");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CreateSetsMinusOneForMissingFields()
    {
        var (source, container) = CreateMocks(
            ("Missing", -1, null));

        var ctx = RowMappingContext.Create(source.Object, container.Object);

        ctx.FieldOrdinals[0].ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CreateLogsWarningForMissingFieldsWhenLoggerProvided()
    {
        var (source, container) = CreateMocks(
            ("Missing", -1, null));

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger>();

        // Should not throw even with missing fields and logger
        var ctx = RowMappingContext.Create(source.Object, container.Object, logger: logger.Object);

        ctx.FieldOrdinals[0].ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CreateResolvesConvertersFromCollection()
    {
        var (source, container) = CreateMocks(
            ("Price", 0, 5));

        var converter = new Mock<IDataTypeConverter>();
        converter.Setup(c => c.Name).Returns("Decimal");
        var converterCollection = new Mock<IDataTypeConverters>();
        converterCollection.Setup(c => c.ById(5)).Returns(converter.Object);

        var ctx = RowMappingContext.Create(source.Object, container.Object, converterCollection.Object);

        ctx.FieldConverters[0].ShouldBe(converter.Object);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CreateLeavesConverterNullWhenNoConverterTypeId()
    {
        var (source, container) = CreateMocks(
            ("Name", 0, null));

        var converterCollection = new Mock<IDataTypeConverters>();

        var ctx = RowMappingContext.Create(source.Object, container.Object, converterCollection.Object);

        ctx.FieldConverters[0].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CreateLeavesConverterNullWhenNoConverterCollection()
    {
        var (source, container) = CreateMocks(
            ("Price", 0, 5));

        var ctx = RowMappingContext.Create(source.Object, container.Object, converterCollection: null);

        ctx.FieldConverters[0].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CreateHandlesEmptySchema()
    {
        var source = new Mock<IRecordCursor>();
        var container = new Mock<IStorageContainer>();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(new List<IField>());
        schema.Setup(s => s.GetProjectableFields()).Returns(new List<IField>());
        container.Setup(c => c.Schema).Returns(schema.Object);

        var ctx = RowMappingContext.Create(source.Object, container.Object);

        ctx.FieldCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "DataIntegrity")]
    public void CreateHandlesMultipleFieldsWithMixedConverters()
    {
        var (source, container) = CreateMocks(
            ("Id", 0, null),
            ("Price", 1, 5),
            ("Name", 2, null),
            ("Amount", 3, 7));

        var decimalConverter = new Mock<IDataTypeConverter>();
        var moneyConverter = new Mock<IDataTypeConverter>();
        var converterCollection = new Mock<IDataTypeConverters>();
        converterCollection.Setup(c => c.ById(5)).Returns(decimalConverter.Object);
        converterCollection.Setup(c => c.ById(7)).Returns(moneyConverter.Object);

        var ctx = RowMappingContext.Create(source.Object, container.Object, converterCollection.Object);

        ctx.FieldCount.ShouldBe(4);
        ctx.FieldConverters[0].ShouldBeNull();
        ctx.FieldConverters[1].ShouldBe(decimalConverter.Object);
        ctx.FieldConverters[2].ShouldBeNull();
        ctx.FieldConverters[3].ShouldBe(moneyConverter.Object);
    }
}
