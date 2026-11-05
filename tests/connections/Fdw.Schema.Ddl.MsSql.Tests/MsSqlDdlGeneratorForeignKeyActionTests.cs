using Fdw.Configuration.Persistence.Schema;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Definitions;
using Fdw.Schema.Ddl.MsSql;
using Fdw.Schema.Ddl.Results;
using Shouldly;
using Xunit;

namespace Fdw.Schema.Ddl.MsSql.Tests;

/// <summary>
/// FDW-3610: SQL Server renders every referential action it can express from the action's own keyword, and
/// refuses the ones it cannot. It used to write any action it did not know, Restrict included, as NO ACTION.
/// </summary>
public sealed class MsSqlDdlGeneratorForeignKeyActionTests
{
    private readonly MsSqlDdlGenerator _sut = new();

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Cascade", "CASCADE")]
    [InlineData("SetNull", "SET NULL")]
    [InlineData("SetDefault", "SET DEFAULT")]
    public void GenerateSqlRendersEachSupportedActionOnBothClauses(string name, string keyword)
    {
        var action = ForeignKeyActions.ByName(name);

        var result = _sut.GenerateSql(Table(onDelete: action, onUpdate: action));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldContain($"ON DELETE {keyword}");
        result.Value.ShouldContain($"ON UPDATE {keyword}");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlRefusesRestrictOnDelete()
    {
        var result = _sut.GenerateSql(Table(onDelete: ForeignKeyActions.Restrict, onUpdate: ForeignKeyActions.NoAction));

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(DdlResultCodes.ByName("ForeignKeyActionNotSupported"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlRefusesRestrictOnUpdate()
    {
        var result = _sut.GenerateSql(Table(onDelete: ForeignKeyActions.NoAction, onUpdate: ForeignKeyActions.Restrict));

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(DdlResultCodes.ByName("ForeignKeyActionNotSupported"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateSqlRefusesTheNotFoundSentinel()
    {
        var result = _sut.GenerateSql(Table(onDelete: ForeignKeyActions.ByName("NoSuchAction"), onUpdate: ForeignKeyActions.NoAction));

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(DdlResultCodes.ByName("ForeignKeyActionNotSupported"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void GenerateScriptStopsAtARefusedTable()
    {
        var result = _sut.GenerateScript([Table(onDelete: ForeignKeyActions.Restrict, onUpdate: ForeignKeyActions.NoAction)]);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBe(DdlResultCodes.ByName("ForeignKeyActionNotSupported"));
    }

    private static CreateTableCommand Table(IForeignKeyAction onDelete, IForeignKeyAction onUpdate) => new()
    {
        SchemaName = "cfg",
        TableName = "Child",
        Columns =
        [
            new DdlColumnDefinition { Name = "Id", SqlType = "INT", IsNullable = false },
            new DdlColumnDefinition { Name = "ParentId", SqlType = "INT", IsNullable = false },
        ],
        ForeignKeys =
        [
            new DdlForeignKeyDefinition
            {
                Name = "FK_Child_Parent",
                ColumnName = "ParentId",
                ReferencedSchema = "cfg",
                ReferencedTable = "Parent",
                ReferencedColumn = "Id",
                OnDelete = onDelete,
                OnUpdate = onUpdate,
            },
        ],
    };
}
