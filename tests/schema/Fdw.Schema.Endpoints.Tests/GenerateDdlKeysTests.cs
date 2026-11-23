using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Schema.Ddl.Commands;
using Fdw.Schema.Ddl.MsSql;
using Fdw.Schema.Endpoints.Ddl;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.DataNodes;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Schema.Endpoints.Tests;

/// <summary>
/// The DDL generated from a discovery carries the primary key the container declares, the whole key, and not the identity columns it
/// was taken from before (FDW-6309). A foreign key is not in the DDL, and each one left out is a coded warning.
/// </summary>
public sealed class GenerateDdlKeysTests
{
    private static readonly DiscoveredKeyMessages Messages = new(
        (key, schema, table, referencedSchema, referencedTable) => Message($"{key} -> {referencedSchema}.{referencedTable}"),
        (key, schema, table, column) => Message($"{key} {column}"));

    private sealed class Endpoint : GenerateDdlEndpointBase
    {
        protected override Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverContainers(
            string connectionName,
            DataStoreDiscoveryOptions options,
            CancellationToken ct)
            => throw new NotSupportedException("The commands are tested; discovery is not.");

        protected override Task<string?> GetConnectionType(string connectionName, CancellationToken ct)
            => throw new NotSupportedException("The commands are tested; the connection type is not.");

        public IReadOnlyList<IDdlCommand> Commands(IReadOnlyList<IStorageContainer> containers, string? schemaFilter)
            => BuildCreateTableCommands(containers, schemaFilter);

        public IList<GenerateDdlWarning> Warnings(IGenericResult discovery, IReadOnlyList<IStorageContainer> containers, string? schemaFilter)
            => WarningsOf(discovery, containers, schemaFilter);
    }

    private static IGenericMessage Message(string text)
    {
        var message = new Mock<IGenericMessage>();
        message.Setup(m => m.Message).Returns(text);
        return message.Object;
    }

    private static Field Column(string name, bool identity = false) => new Field
    {
        Name = name,
        FieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) },
        Role = DatumTypes.NotYetTyped,
        IsIdentity = identity,
    };

    private static DiscoveredContainerSpec Table(
        string name, IField[] columns, string[] primaryKey, params DiscoveredForeignKeySpec[] foreignKeys)
        => Container("Table", ["Query", "Insert", "Update", "Delete"], name, columns, primaryKey, foreignKeys);

    private static DiscoveredContainerSpec Container(
        string typeName,
        string[] operations,
        string name,
        IField[] columns,
        string[] primaryKey,
        params DiscoveredForeignKeySpec[] foreignKeys)
    {
        var type = new Mock<IContainerType>();
        type.Setup(t => t.SupportedOperations).Returns(operations);
        type.Setup(t => t.Name).Returns(typeName);
        var path = new Mock<IPath>();
        path.Setup(p => p.PathValue).Returns("dbo");
        return new DiscoveredContainerSpec(
            name, "dbo", type.Object, FormatTypes.Tabular, path.Object, columns,
            new Dictionary<string, object>(), primaryKey, foreignKeys);
    }

    private static IReadOnlyList<IStorageContainer> Discover(params DiscoveredContainerSpec[] specs)
        => DiscoveredContainers.Assemble(specs, Messages).Value!;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACompositePrimaryKeyIsGeneratedWholeInKeyOrder()
    {
        var containers = Discover(Table("OrderLine", [Column("Qty"), Column("OrderId"), Column("LineNo")], ["OrderId", "LineNo"]));

        var command = (CreateTableCommand)new Endpoint().Commands(containers, null).Single();

        command.PrimaryKeyName.ShouldBe("PK_OrderLine");
        command.PrimaryKeyColumns.ShouldNotBeNull().ShouldBe(new[] { "OrderId", "LineNo" });

        var script = new MsSqlDdlGenerator().GenerateScript([command]);
        script.IsSuccess.ShouldBeTrue();
        script.Value!.ShouldContain("CONSTRAINT [PK_OrderLine] PRIMARY KEY ([OrderId], [LineNo])");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnIdentityColumnThatIsNotThePrimaryKeyIsNoLongerTakenForIt()
    {
        var containers = Discover(Table("Customer", [Column("Id", identity: true), Column("Code")], ["Code"]));

        var command = (CreateTableCommand)new Endpoint().Commands(containers, null).Single();

        command.PrimaryKeyColumns.ShouldNotBeNull().ShouldBe(new[] { "Code" });
    }

    // FDW-6387: the DDL route skips a container whose type cannot Insert (its option's operations), not a container whose type is named View.
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerWhoseTypeCannotInsertIsGivenNoCreateTableWhateverItsTypeIsNamed()
    {
        var containers = Discover(
            Container("View", ["Query"], "ActiveCustomers", [Column("Id")], []),
            Container("StoredProcedure", ["Query"], "GetCustomers", [Column("Id")], []),
            Table("Customer", [Column("Id")], ["Id"]));

        var commands = new Endpoint().Commands(containers, null);

        commands.Count.ShouldBe(1);
        ((CreateTableCommand)commands.Single()).TableName.ShouldBe("Customer");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ATableThatDeclaresNoPrimaryKeyIsGeneratedWithNone()
    {
        var containers = Discover(Table("Log", [Column("Id", identity: true), Column("Message")], []));

        var command = (CreateTableCommand)new Endpoint().Commands(containers, null).Single();

        command.PrimaryKeyColumns.ShouldBeNull();
        command.PrimaryKeyName.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EachForeignKeyLeftOutOfTheDdlIsOneCodedWarning()
    {
        var containers = Discover(
            Table("Customer", [Column("Id")], ["Id"]),
            Table(
                "Order",
                [Column("Id"), Column("CustomerId")],
                ["Id"],
                new DiscoveredForeignKeySpec("FK_Order_Customer", [new DiscoveredForeignKeySpecColumn("CustomerId", "Id")], "dbo", "Customer")));

        var warnings = new Endpoint().Warnings(GenericResult.Success(), containers, null);

        var warning = warnings.Single();
        warning.Message.ShouldContain("FK_Order_Customer");
        warning.Message.ShouldContain("dbo.Order");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheWarningsOfTheDiscoveryAreCarriedOverToo()
    {
        var discovery = GenericResult<int>.Success(1, ResultStatuses.ByName("SuccessWithWarnings"), Message("target not read"));

        var warnings = new Endpoint().Warnings(discovery, Discover(Table("Customer", [Column("Id")], ["Id"])), null);

        warnings.Single().Message.ShouldBe("target not read");
    }
}
