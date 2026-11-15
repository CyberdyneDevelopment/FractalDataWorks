using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataStores.SqlServer.Catalog;
using Fdw.Data.DataStores.SqlServer.Models;
using Fdw.Data.SchemaImporters.Abstractions.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataStores.SqlServer.Tests;

/// <summary>
/// FDW-5876: the catalog importer builds the discovered data store (a path per schema, a container per table, view and procedure, a field per
/// column or parameter) from the catalog read through a connection, as the SqlClient importer built it, and a failed read fails the import
/// where the SqlClient importer carried on with a smaller store. A recording fake connection answers each catalog read with canned rows.
/// </summary>
public sealed class SqlServerCatalogImporterTests
{
    private readonly Mock<IDataConnection> _connection = new();
    private readonly List<string> _containersRead = [];
    private readonly HashSet<Type> _failing = [];

    private readonly List<InformationSchemaSchemata> _schemata = [new InformationSchemaSchemata { CatalogName = "Shop", SchemaName = "dbo" }];
    private readonly List<InformationSchemaTable> _tables = [];
    private readonly List<InformationSchemaRoutine> _routines = [];
    private readonly List<InformationSchemaColumn> _columns = [];
    private readonly List<InformationSchemaParameter> _parameters = [];
    private readonly List<InformationSchemaTableConstraint> _constraints = [];
    private readonly List<InformationSchemaKeyColumnUsage> _keyColumns = [];
    private readonly List<SysSchema> _sysSchemas = [];
    private readonly List<SysObject> _sysObjects = [];
    private readonly List<SysColumn> _sysColumns = [];

    public SqlServerCatalogImporterTests()
    {
        Serve(_schemata);
        Serve(_tables);
        Serve(_routines);
        Serve(_columns);
        Serve(_parameters);
        Serve(_constraints);
        Serve(_keyColumns);
        Serve(_sysSchemas);
        Serve(_sysObjects);
        Serve(_sysColumns);
    }

    private static SchemaImporterOptions Options(
        IReadOnlyList<string>? include = null,
        IReadOnlyList<string>? exclude = null,
        bool skipStoredProcedures = false)
        => new() { IncludeExtendedProperties = false, IncludeSchemas = include, ExcludeSchemas = exclude, SkipStoredProcedures = skipStoredProcedures };

    private void Serve<TRow>(List<TRow> rows)
    {
        _connection
            .Setup(c => c.Execute<IEnumerable<TRow>>(It.IsAny<IDataCommand>(), It.IsAny<IDataContainer>(), It.IsAny<CancellationToken>()))
            .Returns((IDataCommand _, IDataContainer container, CancellationToken _) =>
            {
                _containersRead.Add(container.Name);
                return Task.FromResult<IGenericResult<IEnumerable<TRow>>>(_failing.Contains(typeof(TRow))
                    ? GenericResult<IEnumerable<TRow>>.Failure(new GenericMessage(MessageSeverity.Error, "the catalog read failed", "TST-70001"))
                    : GenericResult<IEnumerable<TRow>>.Success(rows.ToList()));
            });
    }

    private SqlServerCatalogImporter CreateSut()
        => new(new SqlServerCatalogReader(_connection.Object, NullLogger<SqlServerCatalogReader>.Instance), NullLogger<SqlServerCatalogImporter>.Instance);

    // dbo.Orders (a table), dbo.OrderTotals (a view), dbo.GetOrders (a procedure) and sales.Customers (a table), each in the sys catalog with columns.
    private void GivenAShop()
    {
        _tables.AddRange([
            new InformationSchemaTable { TableSchema = "dbo", TableName = "Orders", TableType = "BASE TABLE" },
            new InformationSchemaTable { TableSchema = "dbo", TableName = "OrderTotals", TableType = "VIEW" },
            new InformationSchemaTable { TableSchema = "sales", TableName = "Customers", TableType = "BASE TABLE" }]);
        _routines.Add(new InformationSchemaRoutine { RoutineSchema = "dbo", RoutineName = "GetOrders", RoutineType = "PROCEDURE" });
        _sysSchemas.AddRange([new SysSchema { SchemaId = 1, Name = "dbo" }, new SysSchema { SchemaId = 5, Name = "sales" }]);
        _sysObjects.AddRange([
            new SysObject { ObjectId = 10, Name = "Orders", SchemaId = 1 },
            new SysObject { ObjectId = 11, Name = "OrderTotals", SchemaId = 1 },
            new SysObject { ObjectId = 20, Name = "Customers", SchemaId = 5 }]);
        _sysColumns.AddRange([
            new SysColumn { ObjectId = 10, Name = "Id", IsIdentity = true },
            new SysColumn { ObjectId = 10, Name = "Note" },
            new SysColumn { ObjectId = 11, Name = "Total" },
            new SysColumn { ObjectId = 20, Name = "Name" }]);
        _columns.AddRange([
            new InformationSchemaColumn { TableSchema = "dbo", TableName = "Orders", OrdinalPosition = 1, ColumnName = "Id", DataType = "int", IsNullable = "NO" },
            new InformationSchemaColumn { TableSchema = "dbo", TableName = "Orders", OrdinalPosition = 2, ColumnName = "Note", DataType = "nvarchar", IsNullable = "YES" },
            new InformationSchemaColumn { TableSchema = "dbo", TableName = "OrderTotals", OrdinalPosition = 1, ColumnName = "Total", DataType = "int", IsNullable = "YES" },
            new InformationSchemaColumn { TableSchema = "sales", TableName = "Customers", OrdinalPosition = 1, ColumnName = "Name", DataType = "nvarchar", IsNullable = "NO" }]);
        _constraints.Add(new InformationSchemaTableConstraint { ConstraintName = "PK_Orders", TableSchema = "dbo", TableName = "Orders", ConstraintType = "PRIMARY KEY" });
        _keyColumns.Add(new InformationSchemaKeyColumnUsage { ConstraintName = "PK_Orders", TableSchema = "dbo", TableName = "Orders", ColumnName = "Id", OrdinalPosition = 1 });
        _parameters.Add(new InformationSchemaParameter { SpecificSchema = "dbo", SpecificName = "GetOrders", OrdinalPosition = 1, ParameterMode = "IN", ParameterName = "@Customer", DataType = "nvarchar" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ImportBuildsOnePathPerSchemaWithAContainerForEachTableViewAndProcedureAndAFieldForEachColumnOrParameter()
    {
        GivenAShop();

        var result = await CreateSut().Import(Options(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var store = result.Value.ShouldNotBeNull();
        store.Name.ShouldBe("Shop");
        store.Implementation.ShouldBe("MsSql");
        store.Paths.Select(p => p.Name).ShouldBe(["dbo", "sales"]);
        store.Paths.Single(p => p.Name == "dbo").PathType.ShouldBe("DatabasePath");
        var dbo = store.Paths.Single(p => p.Name == "dbo").Containers;
        dbo.Select(c => $"{c.TypeId}:{c.Name}").ShouldBe(["Table:Orders", "View:OrderTotals", "StoredProcedure:GetOrders"]);
        store.Paths.Single(p => p.Name == "sales").Containers.Select(c => $"{c.TypeId}:{c.Name}").ShouldBe(["Table:Customers"]);

        var orders = dbo.Single(c => c.Name == "Orders").Fields;
        orders.Select(f => f.Name).ShouldBe(["Id", "Note"]);
        orders.Select(f => f.Ordinal).ShouldBe([0, 1]);
        orders[0].DataType.ShouldBe("Int32");
        orders[0].IsNullable.ShouldBeFalse();
        orders[0].IsSystemProvided.ShouldBeTrue();
        orders[1].DataType.ShouldBe("String");
        orders[1].IsNullable.ShouldBeTrue();
        orders[1].IsSystemProvided.ShouldBeFalse();
        dbo.Single(c => c.Name == "OrderTotals").Fields.Single().DataType.ShouldBe("Nullable`1");
        dbo.Single(c => c.Name == "GetOrders").Fields.Single().Name.ShouldBe("@Customer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnIncludeListKeepsOnlyTheSchemasInItWithoutRegardToCase()
    {
        GivenAShop();

        var result = await CreateSut().Import(Options(include: ["SALES"]), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Paths.Select(p => p.Name).ShouldBe(["sales"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task AnExcludeListDropsTheSchemasInIt()
    {
        GivenAShop();

        var result = await CreateSut().Import(Options(exclude: ["dbo"]), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Paths.Select(p => p.Name).ShouldBe(["sales"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task SkippingStoredProceduresMakesNoProcedureContainerAndDoesNotReadTheRoutinesView()
    {
        GivenAShop();

        var result = await CreateSut().Import(Options(skipStoredProcedures: true), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().Paths.SelectMany(p => p.Containers).Select(c => c.TypeId).ShouldNotContain("StoredProcedure");
        _containersRead.ShouldNotContain("ROUTINES");
        _containersRead.ShouldNotContain("PARAMETERS");
    }

    [Theory]
    [InlineData("DatabaseName")]
    [InlineData("Tables")]
    [InlineData("Routines")]
    [InlineData("Columns")]
    [InlineData("Parameters")]
    [InlineData("SysColumns")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AFailedCatalogReadFailsTheImportAndNeverReturnsASmallerStore(string failing)
    {
        // Why: the SqlClient importer turned a failed view or procedure list into a warning and no objects, and skipped a table whose columns would
        // not load; each of those hid a failure as a store that looked complete. Tables here stands for the view list too (one view).
        GivenAShop();
        _failing.Add(failing switch
        {
            "DatabaseName" => typeof(InformationSchemaSchemata),
            "Tables" => typeof(InformationSchemaTable),
            "Routines" => typeof(InformationSchemaRoutine),
            "Columns" => typeof(InformationSchemaColumn),
            "Parameters" => typeof(InformationSchemaParameter),
            _ => typeof(SysColumn),
        });

        var result = await CreateSut().Import(Options(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("the catalog read failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ValidateIsTrueWhenTheCatalogCanBeReadAndTheFailureWhenItCannot()
    {
        var sut = CreateSut();
        (await sut.Validate(TestContext.Current.CancellationToken)).Value.ShouldBeTrue();

        _failing.Add(typeof(InformationSchemaSchemata));

        var failed = await sut.Validate(TestContext.Current.CancellationToken);

        failed.IsFailure.ShouldBeTrue();
        failed.CurrentMessage.ShouldNotBeNull().ShouldContain("the catalog read failed");
    }
}
