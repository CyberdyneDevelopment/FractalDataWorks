using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Fdw.Data.MsSql.Tests.TestSupport;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.MsSql.Tests.Translators;

/// <summary>
/// An UPDATE that states no filter is matched on the container's primary key, the WHOLE key (FDW-6309). On a table whose key is two
/// columns a match on the first column alone updates every row that shares that value. An UPDATE that states a filter is unchanged:
/// it sets every column its record carries, key columns included, exactly as the statement 1.5.10 produced.
/// </summary>
[Collection(nameof(DataMsSqlTestCollection))]
public sealed class MsSqlUpdateTranslatorCompositeKeyTests
{
    private readonly MsSqlUpdateTranslator _sut = new();

    private static Mock<IField> Field(string name)
    {
        var field = new Mock<IField>();
        field.Setup(f => f.FieldType).Returns(TestFieldTypes.Text);
        field.Setup(f => f.Name).Returns(name);
        return field;
    }

    private static Mock<IStorageContainer> Container(string[] columns, params string[] key)
    {
        var fields = columns.Select(column => Field(column).Object).ToArray();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("OrderLines");
        container.Setup(c => c.Path).Returns(new DatabasePath(string.Empty, "dbo", "OrderLines"));
        container.Setup(c => c.Schema).Returns(schema.Object);
        container.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        container.Setup(c => c.Keys).Returns(key.Length == 0 ? new List<IContainerKey>() : TestPrimaryKeys.On(key));
        return container;
    }

    private static Mock<IFilterableCommand> Update(object record, FilterExpression? filter)
    {
        var command = new Mock<IFilterableCommand>();
        command.Setup(c => c.Filter).Returns(filter);
        command.As<IDataCommandWithInput>().Setup(c => c.InputData).Returns(record);
        return command;
    }

    private static FilterExpression StatusIs(string value) => new()
    {
        Root = new FilterCondition { PropertyName = "Status", Operator = new EqualOperator(), Value = value },
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFilteredUpdateWhoseRecordCarriesACompositeKeySetsOnlyTheOtherColumn()
    {
        // The primary key's columns are never in SET, filter or none: a record that carries its key would write that one key value onto
        // every row the filter matches (FDW-6309).
        var container = Container(["OrderId", "LineNo", "Status"], "OrderId", "LineNo");

        var result = await _sut.Translate(
            Update(new { OrderId = "O7", LineNo = "L2", Status = "Shipped" }, StatusIs("Open")).Object,
            container.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe("UPDATE [dbo].[OrderLines] SET [Status] = @set_Status WHERE [Status] = @where_p0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFilteredUpdateWhoseRecordCarriesItsSingleColumnKeyAndOneOtherColumnSetsOnlyTheOtherColumn()
    {
        var container = Container(["Id", "Status"], "Id");

        var result = await _sut.Translate(
            Update(new { Id = "7", Status = "Shipped" }, StatusIs("Open")).Object,
            container.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe("UPDATE [dbo].[OrderLines] SET [Status] = @set_Status WHERE [Status] = @where_p0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUpdateWithNoFilterMatchesTheSingleColumnKeyAndLeavesItOutOfSet()
    {
        var container = Container(["Id", "Status"], "Id");

        var result = await _sut.Translate(
            Update(new { Id = "K5", Status = "Shipped" }, filter: null).Object,
            container.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe("UPDATE [dbo].[OrderLines] SET [Status] = @set_Status WHERE [Id] = @where_pk");
        result.Value.Parameters["@where_pk"].Value.ShouldBe("K5");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUpdateWithNoFilterMatchesEveryColumnOfACompositeKey()
    {
        var container = Container(["OrderId", "LineNo", "Status"], "OrderId", "LineNo");

        var result = await _sut.Translate(
            Update(new { OrderId = "O7", LineNo = "L2", Status = "Shipped" }, filter: null).Object,
            container.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldBe(
            "UPDATE [dbo].[OrderLines] SET [Status] = @set_Status WHERE [OrderId] = @where_pk0 AND [LineNo] = @where_pk1");
        result.Value.Parameters["@where_pk0"].Value.ShouldBe("O7");
        result.Value.Parameters["@where_pk1"].Value.ShouldBe("L2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARecordThatLacksAPartOfACompositeKeyIsRefusedNotMatchedOnTheFirstColumn()
    {
        var container = Container(["OrderId", "LineNo", "Status"], "OrderId", "LineNo");

        var result = await _sut.Translate(
            Update(new { OrderId = "O7", Status = "Shipped" }, filter: null).Object,
            container.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("MissingPrimaryKeyValue");
        result.Details!.GetValue<string>("Container").ShouldBe("OrderLines");
        result.Details.GetValue<string>("PrimaryKeyField").ShouldBe("LineNo");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARecordWhoseCompositeKeyPartIsNullIsRefused()
    {
        var container = Container(["OrderId", "LineNo", "Status"], "OrderId", "LineNo");

        var result = await _sut.Translate(
            Update(new { OrderId = "O7", LineNo = (string?)null, Status = "Shipped" }, filter: null).Object,
            container.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("NullPrimaryKeyValue");
        result.Details!.GetValue<string>("PrimaryKeyField").ShouldBe("LineNo");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AContainerThatDeclaresNoKeyAndAnUpdateWithNoFilterStillRefuses()
    {
        // Refused in 1.5.10 and refused now: nothing names the rows, so nothing is sent.
        var container = Container(["Id", "Status"]);

        var result = await _sut.Translate(
            Update(new { Id = "K5", Status = "Shipped" }, filter: null).Object,
            container.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("UpdateTranslationFailed");
    }
}
