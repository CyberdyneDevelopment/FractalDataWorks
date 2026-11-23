using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.PostgreSql.Tests.Translators;

/// <summary>
/// An UPDATE that states no filter is matched on the container's primary key, the WHOLE key (FDW-6309): on a table whose key is two
/// columns, a match on the first alone updates every row that shares that value. An UPDATE that states a filter is unchanged and sets
/// every column its record carries.
/// </summary>
[Collection(nameof(PostgreSqlTestCollection))]
public sealed class PostgreSqlUpdateTranslatorCompositeKeyTests
{
    private readonly PostgreSqlUpdateTranslator _sut = new();

    private static Mock<IStorageContainer> Container(string[] columns, params string[] key)
    {
        var fields = columns.Select(column =>
        {
            var field = new Mock<IField>();
            field.Setup(f => f.Name).Returns(column);
            return field.Object;
        }).ToArray();
        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields);
        schema.Setup(s => s.GetReturnedFields(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(fields);

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns("order_lines");
        container.Setup(c => c.Path).Returns(new PostgreSqlDatabasePath(null, "public", "order_lines"));
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

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFilteredUpdateWhoseRecordCarriesACompositeKeySetsOnlyTheOtherColumn()
    {
        // The primary key's columns are never in SET, filter or none: a record that carries its key would write that one key value onto
        // every row the filter matches (FDW-6309).
        var container = Container(["order_id", "line_no", "status"], "order_id", "line_no");
        var filter = new FilterExpression { Root = new FilterCondition { PropertyName = "status", Operator = new EqualOperator(), Value = "open" } };

        var result = await _sut.Translate(
            Update(new { order_id = "O7", line_no = "L2", status = "shipped" }, filter).Object,
            container.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("\"status\" = @set_status");
        result.Value.CommandText.ShouldNotContain("\"order_id\" = @set_order_id");
        result.Value.CommandText.ShouldNotContain("\"line_no\" = @set_line_no");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFilteredUpdateWhoseRecordCarriesItsSingleColumnKeyAndOneOtherColumnSetsOnlyTheOtherColumn()
    {
        var container = Container(["id", "status"], "id");
        var filter = new FilterExpression { Root = new FilterCondition { PropertyName = "status", Operator = new EqualOperator(), Value = "open" } };

        var result = await _sut.Translate(
            Update(new { id = "7", status = "shipped" }, filter).Object,
            container.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("\"status\" = @set_status");
        result.Value.CommandText.ShouldNotContain("\"id\" = @set_id");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnUpdateWithNoFilterMatchesEveryColumnOfACompositeKeyAndLeavesThemOutOfSet()
    {
        var container = Container(["order_id", "line_no", "status"], "order_id", "line_no");

        var result = await _sut.Translate(
            Update(new { order_id = "O7", line_no = "L2", status = "shipped" }, filter: null).Object,
            container.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE \"order_id\" = @where_pk0 AND \"line_no\" = @where_pk1");
        result.Value.CommandText.ShouldNotContain("\"order_id\" = @set_order_id");
        result.Value.CommandText.ShouldNotContain("\"line_no\" = @set_line_no");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARecordThatLacksAPartOfACompositeKeyIsRefusedNotMatchedOnTheFirstColumn()
    {
        var container = Container(["order_id", "line_no", "status"], "order_id", "line_no");

        var result = await _sut.Translate(
            Update(new { order_id = "O7", status = "shipped" }, filter: null).Object,
            container.Object,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("MissingPrimaryKeyValue");
        result.Details!.GetValue<string>("PrimaryKeyField").ShouldBe("line_no");
    }
}
