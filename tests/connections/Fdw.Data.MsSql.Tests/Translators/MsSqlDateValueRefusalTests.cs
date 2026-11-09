using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Upserts;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Results;
using Fdw.Data.MsSql.Tests.TestSupport;
using Fdw.Data.MsSql.Translators;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// The paths the first FDW-4726 pass left to SQL Server: an upsert's staged rows, a field typed object whose type is named DateTime, and a filter value. Each refuses a value a date
/// column cannot take with MSSQL-21030, naming the container, the column and the kind of value, never the value.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlDateValueRefusalTests
{
    private static IField Field(string name, Type clrType, string? typeName = null)
    {
        var fieldType = new Mock<IFieldType>();
        fieldType.Setup(f => f.ClrType).Returns(clrType);
        fieldType.Setup(f => f.TypeName).Returns(typeName ?? clrType.Name);
        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.IsIdentity).Returns(false);
        field.Setup(f => f.IsComputed).Returns(false);
        field.Setup(f => f.IsNullable).Returns(false);
        field.Setup(f => f.FieldType).Returns(fieldType.Object);
        return field.Object;
    }

    private static Mock<IDataContainer> Container(params IField[] fields)
    {
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);
        var type = new Mock<IContainerType>();
        type.Setup(t => t.Name).Returns("Table");
        var container = new Mock<IDataContainer>();
        container.Setup(c => c.Name).Returns("Events");
        container.As<IStorageContainer>().Setup(c => c.Name).Returns("Events");
        container.As<IStorageContainer>().Setup(c => c.Path).Returns(new DatabasePath(string.Empty, "dbo", "Events"));
        container.As<IStorageContainer>().Setup(c => c.Metadata).Returns(new Dictionary<string, object> { ["SurrogateKeyField"] = "Id" });
        container.Setup(c => c.Path).Returns(new DatabasePath(string.Empty, "dbo", "Events"));
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.ContainerType).Returns(type.Object);
        container.Setup(c => c.Keys).Returns(new List<IContainerKey>());
        return container;
    }

    private static IDataCommand InsertOf(object entity)
    {
        var command = new Mock<IDataCommandWithInput>();
        command.Setup(c => c.InputData).Returns(new List<object> { entity });
        return command.As<IDataCommand>().Object;
    }

    private static IFilterExpression Where(IFilterNode node) => new FilterExpression { Root = node };

    private static FilterCondition Same(string column, object? value) => new() { PropertyName = column, Operator = new EqualOperator(), Value = value };

    private static IQueryCommand QueryWith(IFilterExpression? filter)
    {
        var query = new Mock<IQueryCommand>();
        query.Setup(q => q.Joins).Returns([]);
        query.Setup(q => q.Filter).Returns(filter);
        query.Setup(q => q.Projection).Returns((IProjectionExpression?)null);
        query.Setup(q => q.Ordering).Returns((IOrderingExpression?)null);
        query.Setup(q => q.Paging).Returns((IPagingExpression?)null);
        return query.Object;
    }

    private static IDeleteScopeCommand DeleteWith(IFilterExpression filter)
    {
        var delete = new Mock<IDeleteScopeCommand>();
        delete.Setup(c => c.Filter).Returns(filter);
        delete.Setup(c => c.AllRows).Returns(false);
        return delete.Object;
    }

    private static void ShouldBeTheCodedRefusal(IGenericResult result, string column, string kind)
    {
        result.IsFailure.ShouldBeTrue();
        result.Code!.Name.ShouldBe("InvalidDateValue");
        result.Details!.Data["Container"].ShouldBe("Events");
        result.Details.Data["Column"].ShouldBe(column);
        result.Details.Data["ValueKind"].ShouldBe(kind);
    }

    // ---- upsert

    private static Task<IGenericResult<Microsoft.Data.SqlClient.SqlCommand>> Upsert(object day, bool bulk = false)
    {
        var container = Container(Field("Code", typeof(string)), Field("Day", typeof(DateOnly)));
        var rows = new List<Dictionary<string, object?>> { new() { ["Code"] = "A", ["Day"] = day } };
        var options = new UpsertOptions(["Code"], ["Day"], ["Code", "Day"]);
        return bulk
            ? new MsSqlBulkUpsertTranslator().Translate(new BulkUpsertCommand<Dictionary<string, object?>>(rows, options), container.Object, TestContext.Current.CancellationToken)
            : new MsSqlUpsertTranslator().Translate(new UpsertCommand<Dictionary<string, object?>>(rows, options), container.Object, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("not a date")]
    [InlineData("13/45/2026")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUpsertOfTextThatIsNotADateIsRefusedBeforeTheRowsAreStaged(string text)
    {
        ShouldBeTheCodedRefusal(await Upsert(text), "Day", "String");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUpsertOfADateStillStagesItsRows()
    {
        var result = await Upsert("10/27/2026");

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        ((string)result.Value!.Parameters["@fdw_rows"].Value).ShouldContain("10/27/2026");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABulkUpsertOfTextThatIsNotADateIsRefusedToo()
    {
        ShouldBeTheCodedRefusal(await Upsert("not a date", bulk: true), "Day", "String");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ABulkUpsertOfADateStillStagesItsRows()
    {
        var result = await Upsert("10/27/2026", bulk: true);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        ((string)result.Value!.Parameters["@fdw_rows"].Value).ShouldContain("10/27/2026");
    }

    // ---- an object-typed field named DateTime

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASerialInsertIntoAFieldTypedObjectAndNamedDateTimeRefusesTextThatIsNotADate()
    {
        var container = Container(Field("Day", typeof(object), "DateTime"));

        var refused = await new MsSqlInsertTranslator().Translate(InsertOf(new { Day = "not a date" }), container.Object, TestContext.Current.CancellationToken);
        var written = await new MsSqlInsertTranslator().Translate(InsertOf(new { Day = new DateTime(2026, 10, 27) }), container.Object, TestContext.Current.CancellationToken);

        ShouldBeTheCodedRefusal(refused, "Day", "String");
        written.IsSuccess.ShouldBeTrue(written.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFieldTypedObjectAndNamedSomethingElseIsNotADateField()
    {
        var container = Container(Field("Note", typeof(object), "String"));

        var result = await new MsSqlInsertTranslator().Translate(InsertOf(new { Note = "not a date" }), container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    // ---- filters

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AQueryFilterValueThatIsNotADateIsRefusedBeforeItIsSentAsAParameter()
    {
        var container = Container(Field("Id", typeof(int)), Field("Day", typeof(DateOnly)));

        var result = await new MsSqlQueryTranslator().Translate(QueryWith(Where(Same("Day", "junk"))), container.Object, TestContext.Current.CancellationToken);

        ShouldBeTheCodedRefusal(result, "Day", "String");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFilterValueInsideAGroupIsFoundToo()
    {
        var container = Container(Field("Id", typeof(int)), Field("Day", typeof(DateOnly)));
        var filter = Where(new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = [Same("Id", 1), new FilterGroup { Operator = LogicalOperator.Or, Nodes = [Same("Day", "junk")] }],
        });

        var result = await new MsSqlQueryTranslator().Translate(QueryWith(filter), container.Object, TestContext.Current.CancellationToken);

        ShouldBeTheCodedRefusal(result, "Day", "String");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFilterOnADateStillTranslatesWithADateOrDateText()
    {
        var container = Container(Field("Id", typeof(int)), Field("Day", typeof(DateOnly)));

        (await new MsSqlQueryTranslator().Translate(QueryWith(Where(Same("Day", "10/27/2026"))), container.Object, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        (await new MsSqlQueryTranslator().Translate(QueryWith(Where(Same("Day", new DateOnly(2026, 10, 27)))), container.Object, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task APatternMatchOnADateColumnIsLeftAlone()
    {
        var container = Container(Field("Id", typeof(int)), Field("Day", typeof(DateOnly)));
        var like = new FilterCondition { PropertyName = "Day", Operator = new ContainsOperator(), Value = "2026" };

        var result = await new MsSqlQueryTranslator().Translate(QueryWith(Where(like)), container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADeleteFilterValueThatIsNotADateIsRefused()
    {
        var container = Container(Field("Id", typeof(int)), Field("Day", typeof(DateOnly)));

        var result = await new MsSqlDeleteTranslator().Translate(DeleteWith(Where(Same("Day", "junk"))), container.Object, TestContext.Current.CancellationToken);

        ShouldBeTheCodedRefusal(result, "Day", "String");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUpdateFilterValueThatIsNotADateIsRefused()
    {
        var container = Container(Field("Id", typeof(int)), Field("Name", typeof(string)), Field("Day", typeof(DateOnly)));
        var update = new Mock<IFilterableCommand>();
        update.Setup(c => c.Filter).Returns(Where(Same("Day", "junk")));
        update.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(new { Name = "n" });

        var result = await new MsSqlUpdateTranslator().Translate(update.Object, container.Object, TestContext.Current.CancellationToken);

        ShouldBeTheCodedRefusal(result, "Day", "String");
    }
}
