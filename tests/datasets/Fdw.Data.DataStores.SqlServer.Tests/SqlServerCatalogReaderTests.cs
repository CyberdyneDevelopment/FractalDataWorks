using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.SqlServer.Catalog;
using Fdw.Data.DataStores.SqlServer.Models;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataStores.SqlServer.Tests;

/// <summary>
/// FDW-5876: the SQL Server catalog is read through an <see cref="IDataConnection"/> with query commands, never through SqlClient. A recording
/// fake connection answers each read with canned catalog rows and notes what was asked. The reads the old importer wrote as joined SQL
/// (COLUMNPROPERTY, the primary-key derived table) are recomposed in memory, so the tests give the reader the catalog the way the database
/// holds it: the same table name in two schemas, a UNIQUE key beside the PRIMARY KEY, rows out of order.
/// </summary>
public sealed class SqlServerCatalogReaderTests
{
    private readonly Mock<IDataConnection> _connection = new();
    private readonly List<(string Container, string Schema, IDataCommand Command)> _reads = [];
    private readonly HashSet<Type> _failing = [];

    private readonly List<InformationSchemaTable> _tables = [];
    private readonly List<InformationSchemaSchemata> _schemata = [];
    private readonly List<InformationSchemaRoutine> _routines = [];
    private readonly List<InformationSchemaColumn> _columns = [];
    private readonly List<InformationSchemaParameter> _parameters = [];
    private readonly List<InformationSchemaTableConstraint> _constraints = [];
    private readonly List<InformationSchemaKeyColumnUsage> _keyColumns = [];
    private readonly List<SysSchema> _sysSchemas = [];
    private readonly List<SysObject> _sysObjects = [];
    private readonly List<SysColumn> _sysColumns = [];

    public SqlServerCatalogReaderTests()
    {
        Serve(_tables);
        Serve(_schemata);
        Serve(_routines);
        Serve(_columns);
        Serve(_parameters);
        Serve(_constraints);
        Serve(_keyColumns);
        Serve(_sysSchemas);
        Serve(_sysObjects);
        Serve(_sysColumns);
    }

    private static GenericMessage ReadFailure()
        => new(MessageSeverity.Error, "the catalog read failed", "TST-70001");

    private void Serve<TRow>(List<TRow> rows)
    {
        _connection
            .Setup(c => c.Execute<IEnumerable<TRow>>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand command, IDataContainer container, CancellationToken _) =>
            {
                _reads.Add((container.Name, container.Parent.Name, command));
                return Task.FromResult<IGenericResult<IEnumerable<TRow>>>(_failing.Contains(typeof(TRow))
                    ? GenericResult<IEnumerable<TRow>>.Failure(ReadFailure())
                    : GenericResult<IEnumerable<TRow>>.Success(rows.ToList()));
            });
    }

    private SqlServerCatalogReader CreateSut()
        => new(_connection.Object, NullLogger<SqlServerCatalogReader>.Instance);

    private static IEnumerable<IFilterCondition> Conditions(IFilterNode? node)
        => node switch
        {
            IFilterCondition condition => [condition],
            FilterGroup group => group.Nodes.SelectMany(Conditions),
            _ => [],
        };

    // A property name or a column name, however the command spells it: TableType, TABLE_TYPE and table_type are one.
    private static string Plain(string name) => name.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    private static List<(string Property, string Operator, object? Value)> FilterOf((string Container, string Schema, IDataCommand Command) read)
        => Conditions(((IQueryCommand)read.Command).Filter?.Root)
            .Select(c => (Plain(c.PropertyName), c.Operator.Name, c.Value))
            .ToList();

    private void GivenTwoOrdersTables()
    {
        // Same table name in two schemas: the old query keyed on schema and name together (OBJECT_ID(schema.table)).
        _sysSchemas.AddRange([new SysSchema { SchemaId = 1, Name = "dbo" }, new SysSchema { SchemaId = 5, Name = "sales" }]);
        _sysObjects.AddRange([
            new SysObject { ObjectId = 10, Name = "Orders", SchemaId = 1 },
            new SysObject { ObjectId = 20, Name = "Orders", SchemaId = 5 }]);
        _sysColumns.AddRange([
            new SysColumn { ObjectId = 10, Name = "Id", IsIdentity = true },
            new SysColumn { ObjectId = 10, Name = "Total" },
            new SysColumn { ObjectId = 10, Name = "Doubled", IsComputed = true },
            new SysColumn { ObjectId = 20, Name = "Id" },
            new SysColumn { ObjectId = 20, Name = "Total", IsComputed = true }]);
        _columns.AddRange([
            // Out of ordinal order on purpose.
            new InformationSchemaColumn { TableSchema = "dbo", TableName = "Orders", OrdinalPosition = 3, ColumnName = "Doubled", DataType = "int", IsNullable = "YES" },
            new InformationSchemaColumn { TableSchema = "dbo", TableName = "Orders", OrdinalPosition = 1, ColumnName = "Id", DataType = "int", IsNullable = "NO" },
            new InformationSchemaColumn
            {
                TableSchema = "dbo", TableName = "Orders", OrdinalPosition = 2, ColumnName = "Total", DataType = "decimal", IsNullable = "YES",
                NumericPrecision = 18, NumericScale = 2, ColumnDefault = "((0))",
            },
            new InformationSchemaColumn { TableSchema = "sales", TableName = "Orders", OrdinalPosition = 1, ColumnName = "Id", DataType = "bigint", IsNullable = "NO" },
            new InformationSchemaColumn { TableSchema = "sales", TableName = "Orders", OrdinalPosition = 2, ColumnName = "Total", DataType = "nvarchar", IsNullable = "NO", CharacterMaximumLength = 200 }]);
        _constraints.AddRange([
            new InformationSchemaTableConstraint { ConstraintName = "PK_dbo_Orders", TableSchema = "dbo", TableName = "Orders", ConstraintType = "PRIMARY KEY" },
            new InformationSchemaTableConstraint { ConstraintName = "UQ_dbo_Orders_Total", TableSchema = "dbo", TableName = "Orders", ConstraintType = "UNIQUE" },
            new InformationSchemaTableConstraint { ConstraintName = "PK_sales_Orders", TableSchema = "sales", TableName = "Orders", ConstraintType = "PRIMARY KEY" }]);
        _keyColumns.AddRange([
            new InformationSchemaKeyColumnUsage { ConstraintName = "PK_dbo_Orders", TableSchema = "dbo", TableName = "Orders", ColumnName = "Id", OrdinalPosition = 1 },
            new InformationSchemaKeyColumnUsage { ConstraintName = "UQ_dbo_Orders_Total", TableSchema = "dbo", TableName = "Orders", ColumnName = "Total", OrdinalPosition = 1 },
            new InformationSchemaKeyColumnUsage { ConstraintName = "PK_sales_Orders", TableSchema = "sales", TableName = "Orders", ColumnName = "Id", OrdinalPosition = 1 }]);
    }

    // ====================================================================
    // Which objects: tables, views, procedures
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ReadTablesAsksTheTablesViewForBaseTablesOutsideTheSystemSchemasAndKeepsTheOrderItWasGiven()
    {
        _tables.AddRange([
            new InformationSchemaTable { TableSchema = "dbo", TableName = "Orders", TableType = "BASE TABLE" },
            new InformationSchemaTable { TableSchema = "sales", TableName = "Customers", TableType = "BASE TABLE" }]);

        var result = await CreateSut().ReadTables(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Select(o => $"{o.SchemaName}.{o.ObjectName}").ShouldBe(["dbo.Orders", "sales.Customers"]);
        _reads.Count.ShouldBe(1);
        _reads[0].Container.ShouldBe("TABLES");
        _reads[0].Schema.ShouldBe("INFORMATION_SCHEMA");
        var filter = FilterOf(_reads[0]);
        filter.ShouldContain(f => f.Property == "TABLETYPE" && f.Operator == "Equal" && "BASE TABLE".Equals(f.Value));
        filter.Count(f => f.Property == "TABLESCHEMA" && f.Operator == "NotEqual").ShouldBe(2);
        filter.Where(f => f.Property == "TABLESCHEMA").Select(f => f.Value).ShouldBe(["sys", "INFORMATION_SCHEMA"], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ReadViewsAsksTheTablesViewForViewsOutsideTheSystemSchemas()
    {
        _tables.Add(new InformationSchemaTable { TableSchema = "dbo", TableName = "OrderTotals", TableType = "VIEW" });

        var result = await CreateSut().ReadViews(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Select(o => $"{o.SchemaName}.{o.ObjectName}").ShouldBe(["dbo.OrderTotals"]);
        _reads.Count.ShouldBe(1);
        _reads[0].Container.ShouldBe("TABLES");
        var filter = FilterOf(_reads[0]);
        filter.ShouldContain(f => f.Property == "TABLETYPE" && f.Operator == "Equal" && "VIEW".Equals(f.Value));
        filter.Count(f => f.Property == "TABLESCHEMA" && f.Operator == "NotEqual").ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ReadProceduresAsksTheRoutinesViewForProceduresOutsideTheSysSchema()
    {
        _routines.Add(new InformationSchemaRoutine { RoutineSchema = "dbo", RoutineName = "GetOrders", RoutineType = "PROCEDURE" });

        var result = await CreateSut().ReadProcedures(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Select(o => $"{o.SchemaName}.{o.ObjectName}").ShouldBe(["dbo.GetOrders"]);
        _reads.Count.ShouldBe(1);
        _reads[0].Container.ShouldBe("ROUTINES");
        _reads[0].Schema.ShouldBe("INFORMATION_SCHEMA");
        var filter = FilterOf(_reads[0]);
        filter.ShouldContain(f => f.Property == "ROUTINETYPE" && f.Operator == "Equal" && "PROCEDURE".Equals(f.Value));
        filter.ShouldContain(f => f.Property == "ROUTINESCHEMA" && f.Operator == "NotEqual" && "sys".Equals(f.Value));
    }

    // ====================================================================
    // Columns: INFORMATION_SCHEMA plus the sys flags plus the primary key, put together in memory
    // ====================================================================

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadColumnsReturnsTheColumnsOfThatSchemaAndTableInOrdinalOrderWithTheirFlags()
    {
        GivenTwoOrdersTables();

        var result = await CreateSut().ReadColumns("dbo", "Orders", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var columns = result.Value.ShouldNotBeNull();
        columns.Select(c => c.Name).ShouldBe(["Id", "Total", "Doubled"]);
        var id = columns[0];
        id.DataType.ShouldBe("int");
        id.IsNullable.ShouldBeFalse();
        id.IsPrimaryKey.ShouldBeTrue();
        id.IsIdentity.ShouldBeTrue();
        id.IsComputed.ShouldBeFalse();
        var total = columns[1];
        total.DataType.ShouldBe("decimal");
        total.IsNullable.ShouldBeTrue();
        total.Precision.ShouldBe((byte)18);
        total.Scale.ShouldBe(2);
        total.DefaultValue.ShouldBe("((0))");
        total.MaxLength.ShouldBeNull();
        total.IsPrimaryKey.ShouldBeFalse();
        total.IsIdentity.ShouldBeFalse();
        columns[2].IsComputed.ShouldBeTrue();
        columns[2].IsIdentity.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadColumnsOfOneSchemaNeverTakesTheFlagsOrTheKeyOfTheSameTableNameInAnotherSchema()
    {
        GivenTwoOrdersTables();

        var result = await CreateSut().ReadColumns("sales", "Orders", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var columns = result.Value.ShouldNotBeNull();
        columns.Select(c => c.Name).ShouldBe(["Id", "Total"]);
        columns[0].DataType.ShouldBe("bigint");
        columns[0].IsPrimaryKey.ShouldBeTrue();
        columns[0].IsIdentity.ShouldBeFalse();
        columns[1].IsNullable.ShouldBeFalse();
        columns[1].MaxLength.ShouldBe(200);
        columns[1].IsComputed.ShouldBeTrue();
        columns[1].IsPrimaryKey.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task OnlyAPrimaryKeyConstraintMakesAColumnAPrimaryKeyAndAUniqueKeyDoesNot()
    {
        GivenTwoOrdersTables();

        var result = await CreateSut().ReadColumns("dbo", "Orders", TestContext.Current.CancellationToken);

        // dbo.Orders.Total is in a UNIQUE key (UQ_dbo_Orders_Total) and nowhere in a PRIMARY KEY.
        result.Value.ShouldNotBeNull().Single(c => c.Name == "Total").IsPrimaryKey.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ReadColumnsAsksTheColumnsViewForThatTableAndNoOtherTableOnlyOnce()
    {
        GivenTwoOrdersTables();

        await CreateSut().ReadColumns("dbo", "Orders", TestContext.Current.CancellationToken);

        var columnsRead = _reads.Single(r => r.Container == "COLUMNS");
        columnsRead.Schema.ShouldBe("INFORMATION_SCHEMA");
        var filter = FilterOf(columnsRead);
        filter.ShouldContain(f => f.Property == "TABLESCHEMA" && f.Operator == "Equal" && "dbo".Equals(f.Value));
        filter.ShouldContain(f => f.Property == "TABLENAME" && f.Operator == "Equal" && "Orders".Equals(f.Value));
    }

    // ====================================================================
    // Parameters
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadParametersReturnsThatProceduresParametersInOrdinalOrderAndOnlyAnInOrUnstatedModeIsNullable()
    {
        _parameters.AddRange([
            new InformationSchemaParameter { SpecificSchema = "dbo", SpecificName = "GetOrders", OrdinalPosition = 2, ParameterMode = "OUT", ParameterName = "@Count", DataType = "int" },
            new InformationSchemaParameter
            {
                SpecificSchema = "dbo", SpecificName = "GetOrders", OrdinalPosition = 1, ParameterMode = "IN", ParameterName = "@Customer",
                DataType = "nvarchar", CharacterMaximumLength = 50,
            },
            new InformationSchemaParameter { SpecificSchema = "dbo", SpecificName = "GetOrders", OrdinalPosition = 3, ParameterMode = null, ParameterName = "@Note", DataType = "varchar" },
            new InformationSchemaParameter { SpecificSchema = "dbo", SpecificName = "Other", OrdinalPosition = 1, ParameterMode = "IN", ParameterName = "@Other", DataType = "int" }]);

        var result = await CreateSut().ReadParameters("dbo", "GetOrders", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var parameters = result.Value.ShouldNotBeNull();
        parameters.Select(p => p.Name).ShouldBe(["@Customer", "@Count", "@Note"]);
        parameters.Select(p => p.IsNullable).ShouldBe([true, false, true]);
        parameters[0].MaxLength.ShouldBe(50);
        var read = _reads.Single();
        read.Container.ShouldBe("PARAMETERS");
        var filter = FilterOf(read);
        filter.ShouldContain(f => f.Property == "SPECIFICSCHEMA" && f.Operator == "Equal" && "dbo".Equals(f.Value));
        filter.ShouldContain(f => f.Property == "SPECIFICNAME" && f.Operator == "Equal" && "GetOrders".Equals(f.Value));
    }

    // ====================================================================
    // A read that failed is a failure, never "no tables" (the old importer turned two of these into an empty list)
    // ====================================================================

    [Theory]
    [InlineData("Tables")]
    [InlineData("Views")]
    [InlineData("Procedures")]
    [InlineData("Columns")]
    [InlineData("Parameters")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailedCatalogReadKeepsItsCauseAndIsNeverAnEmptyList(string read)
    {
        GivenTwoOrdersTables();
        var sut = CreateSut();
        _failing.Add(read switch
        {
            "Tables" or "Views" => typeof(InformationSchemaTable),
            "Procedures" => typeof(InformationSchemaRoutine),
            "Columns" => typeof(InformationSchemaColumn),
            _ => typeof(InformationSchemaParameter),
        });
        var ct = TestContext.Current.CancellationToken;

        IGenericResult result = read switch
        {
            "Tables" => await sut.ReadTables(ct),
            "Views" => await sut.ReadViews(ct),
            "Procedures" => await sut.ReadProcedures(ct),
            "Columns" => await sut.ReadColumns("dbo", "Orders", ct),
            _ => await sut.ReadParameters("dbo", "GetOrders", ct),
        };

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("the catalog read failed");
    }

    [Theory]
    [InlineData("Constraints")]
    [InlineData("KeyColumns")]
    [InlineData("SysSchemas")]
    [InlineData("SysObjects")]
    [InlineData("SysColumns")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AColumnReadThatCannotGetOneOfItsSupportingReadsFailsAndDoesNotReturnColumnsWithTheFlagsLeftOut(string supporting)
    {
        GivenTwoOrdersTables();
        _failing.Add(supporting switch
        {
            "Constraints" => typeof(InformationSchemaTableConstraint),
            "KeyColumns" => typeof(InformationSchemaKeyColumnUsage),
            "SysSchemas" => typeof(SysSchema),
            "SysObjects" => typeof(SysObject),
            _ => typeof(SysColumn),
        });

        var result = await CreateSut().ReadColumns("dbo", "Orders", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("the catalog read failed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ColumnsOfATableTheSysCatalogDoesNotKnowAreRefusedAndNotReturnedWithoutFlags()
    {
        GivenTwoOrdersTables();
        _sysObjects.Clear();

        var result = await CreateSut().ReadColumns("dbo", "Orders", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsMissing(result).ShouldBeTrue();
    }

    // ====================================================================
    // The database name (the data store's name): the catalog name the dbo schema carries
    // ====================================================================

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadDatabaseNameReturnsTheCatalogNameOfTheDboSchemaFromTheSchemataView()
    {
        _schemata.AddRange([
            new InformationSchemaSchemata { CatalogName = "Shop", SchemaName = "dbo" },
            new InformationSchemaSchemata { CatalogName = "Shop", SchemaName = "sales" }]);

        var result = await CreateSut().ReadDatabaseName(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Shop");
        _reads.Count.ShouldBe(1);
        _reads[0].Container.ShouldBe("SCHEMATA");
        _reads[0].Schema.ShouldBe("INFORMATION_SCHEMA");
        FilterOf(_reads[0]).ShouldContain(f => f.Property == "SCHEMANAME" && f.Operator == "Equal" && "dbo".Equals(f.Value));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadDatabaseNameIsAMissingFailureWhenTheCatalogListsNoSchemaAndNeverAnEmptyName()
    {
        var result = await CreateSut().ReadDatabaseName(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        FailureCategory.IsMissing(result).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ReadDatabaseNameKeepsTheCauseOfAFailedRead()
    {
        _failing.Add(typeof(InformationSchemaSchemata));

        var result = await CreateSut().ReadDatabaseName(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("the catalog read failed");
    }
}
