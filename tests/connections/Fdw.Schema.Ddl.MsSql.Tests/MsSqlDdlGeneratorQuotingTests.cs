using Fdw.Configuration.Persistence.Schema;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.Definitions;
using Fdw.Schema.Ddl.MsSql;
using Fdw.Schema.Ddl.Results;

namespace Fdw.Schema.Ddl.MsSql.Tests;

/// <summary>
/// The SQL Server dialect writes every identifier through its own quoting, so the text it writes is safe to run (FDW-3617): a
/// name with a space, a reserved word or a closing bracket is that name, never more SQL. A statement ends where the engine's batch
/// ends, with no <c>GO</c>; the separator belongs to the script a person reads.
/// </summary>
public class MsSqlDdlGeneratorQuotingTests
{
    private readonly MsSqlDdlGenerator _sut = new();

    // The statement of a result that succeeded; a failure or a missing statement fails the test here, so the assertions read a checked value.
    private static string SqlOf(IGenericResult<string> result)
    {
        result.IsSuccess.ShouldBeTrue();
        return result.Value.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("Orders", "[Orders]")]
    [InlineData("my table", "[my table]")]
    [InlineData("x]y", "[x]]y]")]
    [InlineData("]", "[]]]")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void QuoteIdentifierBracketsTheNameAndDoublesAClosingBracket(string name, string expected)
    {
        var result = _sut.QuoteIdentifier(name);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a\0b")]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void QuoteIdentifierRefusesAnEmptyBlankOrNulNameWithTheIdentifierCode(string name)
    {
        var result = _sut.QuoteIdentifier(name);

        result.IsSuccess.ShouldBeFalse();
        result.FailureReason().Code.ShouldBe(DdlResultCodes.ByName("IdentifierInvalid").Code);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateTableQuotesTheSchemaTableColumnAndConstraintNames()
    {
        var cmd = new CreateTableCommand
        {
            SchemaName = "my schema",
            TableName = "my]table",
            Columns = [new DdlColumnDefinition { Name = "col one", SqlType = "INT", IsNullable = false }],
            PrimaryKeyName = "PK my]table",
            PrimaryKeyColumns = ["col one"],
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        SqlOf(result).ShouldContain("CREATE TABLE [my schema].[my]]table]");
        SqlOf(result).ShouldContain("[col one]");
        SqlOf(result).ShouldContain("CONSTRAINT [PK my]]table] PRIMARY KEY ([col one])");
        SqlOf(result).ShouldContain("OBJECT_ID(N'[my schema].[my]]table]')");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ANameThatIsAnAttackStaysInsideItsBracketsAndRunsNothingElse()
    {
        var cmd = new CreateTableCommand
        {
            TableName = "x]; DROP TABLE y;--",
            Columns = [new DdlColumnDefinition { Name = "Id", SqlType = "INT" }],
        };

        var result = _sut.GenerateSql(cmd);

        result.IsSuccess.ShouldBeTrue();
        SqlOf(result).ShouldContain("CREATE TABLE [x]]; DROP TABLE y;--]");
        SqlOf(result).Split('\n').ShouldNotContain(line => line.TrimStart().StartsWith("DROP", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ASingleQuoteInANameIsEscapedInTheExistenceCheckAndInsideExec()
    {
        var table = _sut.GenerateSql(new CreateTableCommand
        {
            SchemaName = "dbo",
            TableName = "o'brien",
            Columns = [new DdlColumnDefinition { Name = "Id", SqlType = "INT" }],
        });
        var schema = _sut.GenerateSql(new CreateSchemaCommand { Name = "o'brien" });

        SqlOf(table).ShouldContain("OBJECT_ID(N'[dbo].[o''brien]')");
        SqlOf(schema).ShouldContain("name = N'o''brien'");
        SqlOf(schema).ShouldContain("EXEC(N'CREATE SCHEMA [o''brien]')");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ForeignKeyUniqueIndexAndDropNamesAreQuoted()
    {
        var table = _sut.GenerateSql(new CreateTableCommand
        {
            SchemaName = "s one",
            TableName = "child table",
            Columns = [new DdlColumnDefinition { Name = "ref col", SqlType = "INT", IsUnique = true }],
            ForeignKeys =
            [
                new DdlForeignKeyDefinition
                {
                    Name = "FK one",
                    ColumnName = "ref col",
                    ReferencedSchema = "s two",
                    ReferencedTable = "ref table",
                    ReferencedColumn = "ref id",
                    OnDelete = ForeignKeyActions.Cascade,
                    OnUpdate = ForeignKeyActions.NoAction,
                },
            ],
        });
        var index = _sut.GenerateSql(new CreateIndexCommand
        {
            SchemaName = "s one",
            TableName = "child table",
            IndexName = "IX one",
            Definition = new DdlIndexDefinition { Name = "IX one", Columns = ["ref col"], IncludeColumns = ["other col"] },
        });
        var dropIndex = _sut.GenerateSql(new DropIndexCommand { SchemaName = "s one", TableName = "child table", IndexName = "IX one" });
        var dropTable = _sut.GenerateSql(new DropTableCommand { SchemaName = "s one", TableName = "child table" });
        var dropSchema = _sut.GenerateSql(new DropSchemaCommand { Name = "s one" });

        SqlOf(table).ShouldContain("CONSTRAINT [UQ_child table_ref col] UNIQUE ([ref col])");
        SqlOf(table).ShouldContain("CONSTRAINT [FK one] FOREIGN KEY ([ref col]) REFERENCES [s two].[ref table]([ref id])");
        SqlOf(index).ShouldContain("INDEX [IX one] ON [s one].[child table] ([ref col]) INCLUDE ([other col])");
        SqlOf(dropIndex).ShouldContain("DROP INDEX [IX one] ON [s one].[child table]");
        SqlOf(dropIndex).ShouldContain("name = N'IX one'");
        SqlOf(dropTable).ShouldContain("DROP TABLE [s one].[child table]");
        SqlOf(dropSchema).ShouldContain("DROP SCHEMA [s one]");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AnIdentityColumnIsWrittenWithIdentityAndTheOthersAreNot()
    {
        var cmd = new CreateTableCommand
        {
            TableName = "T",
            Columns =
            [
                new DdlColumnDefinition { Name = "Id", SqlType = "INT", IsNullable = false, IsIdentity = true },
                new DdlColumnDefinition { Name = "Label", SqlType = "NVARCHAR", MaxLength = 50 },
            ],
            PrimaryKeyName = "PK_T",
            PrimaryKeyColumns = ["Id"],
        };

        var result = _sut.GenerateSql(cmd);

        var lines = result.Value!.Split('\n');
        lines.Single(line => line.Contains("[Id]", StringComparison.Ordinal) && !line.Contains("CONSTRAINT", StringComparison.Ordinal))
            .ShouldContain("IDENTITY");
        lines.Single(line => line.Contains("[Label]", StringComparison.Ordinal)).ShouldNotContain("IDENTITY");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NoStatementEndsInGoAndTheScriptSeparatesStatementsWithGo()
    {
        IDdlCommand[] commands =
        [
            new CreateSchemaCommand { Name = "cfg" },
            new CreateTableCommand { SchemaName = "cfg", TableName = "T", Columns = [new DdlColumnDefinition { Name = "Id", SqlType = "INT" }] },
            new CreateIndexCommand
            {
                SchemaName = "cfg",
                TableName = "T",
                IndexName = "IX_T",
                Definition = new DdlIndexDefinition { Name = "IX_T", Columns = ["Id"] },
            },
            new DropIndexCommand { SchemaName = "cfg", TableName = "T", IndexName = "IX_T" },
            new DropTableCommand { SchemaName = "cfg", TableName = "T" },
            new DropSchemaCommand { Name = "cfg" },
        ];

        foreach (var command in commands)
            _sut.GenerateSql(command).Value!.TrimEnd().ShouldNotEndWith("GO");

        var script = _sut.GenerateScript(commands.Take(3).ToList());

        script.Value!.Split('\n').Count(line => string.Equals(line.Trim(), "GO", StringComparison.Ordinal)).ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AnInvalidNameRefusesTheCommandWithTheIdentifierCodeAndNotTheGenericOne()
    {
        var result = _sut.GenerateSql(new CreateTableCommand
        {
            TableName = " ",
            Columns = [new DdlColumnDefinition { Name = "Id", SqlType = "INT" }],
        });

        result.IsSuccess.ShouldBeFalse();
        result.FailureReason().Code.ShouldBe(DdlResultCodes.ByName("IdentifierInvalid").Code);
    }
}
