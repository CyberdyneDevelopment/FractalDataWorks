using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Schema;
using Fdw.Services.Connections;
using Fdw.Services.Data.Builders;
using Fdw.Services.Data.DataNodes;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A schema discovery merged into a data store keeps the primary key and the foreign keys the catalog declares (FDW-6309), beside the
/// Surrogate key readers look up by name, and a store built from what was stored has the same keys again.
/// </summary>
public sealed class DiscoveredSchemaMergeKeysTests
{
    private static readonly DiscoveredKeyMessages Messages = new(
        (key, schema, table, referencedSchema, referencedTable) => Warning($"{key} -> {referencedSchema}.{referencedTable}"),
        (key, schema, table, column) => Warning($"{key} {column}"));

    private static IGenericMessage Warning(string text)
    {
        var message = new Mock<IGenericMessage>();
        message.Setup(m => m.Message).Returns(text);
        return message.Object;
    }

    private static Field Column(string name, bool identifier = false) => new Field
    {
        Name = name,
        FieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) },
        Role = identifier ? DatumTypes.Identifier : DatumTypes.NotYetTyped,
    };

    private static DiscoveredContainerSpec Table(
        string name, string[] columns, string[] primaryKey, params DiscoveredForeignKeySpec[] foreignKeys)
    {
        var type = new Mock<IContainerType>();
        type.Setup(t => t.SupportedOperations).Returns(new[] { "Query", "Insert", "Update", "Delete" });
        // A container type this test process registers (its assembly references Fdw.Data.Files): the store built from the merge below
        // refuses a type name ContainerTypes does not hold (FDW-6387), and the SQL Server and PostgreSQL options are not referenced here.
        type.Setup(t => t.Name).Returns("File");
        var path = new Mock<IPath>();
        path.Setup(p => p.PathValue).Returns("dbo");
        return new DiscoveredContainerSpec(
            name,
            "dbo",
            type.Object,
            FormatTypes.Tabular,
            path.Object,
            [.. columns.Select(column => Column(column, identifier: primaryKey.Contains(column)))],
            new Dictionary<string, object>(),
            primaryKey,
            foreignKeys);
    }

    private static DiscoveredForeignKeySpec Foreign(string name, string table, params (string Column, string Referenced)[] pairs)
        => new(name, [.. pairs.Select(pair => new DiscoveredForeignKeySpecColumn(pair.Column, pair.Referenced))], "dbo", table);

    private static IReadOnlyList<IStorageContainer> Discover(params DiscoveredContainerSpec[] specs)
        => DiscoveredContainers.Assemble(specs, Messages).Value!;

    private static DataContainerConfiguration Container(DataStoreImplementationConfiguration store, string name)
        => store.Paths.Single().Containers.Single(container => container.Name == name);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANewContainerStoresItsPrimaryKeyBesideTheSurrogateKey()
    {
        var store = new DataStoreImplementationConfiguration();

        DiscoveredSchemaMerge.Merge(store, Discover(Table("Customer", ["Id", "Name"], ["Id"])), refresh: false);

        var keys = Container(store, "Customer").Keys;
        keys.Select(key => (key.Name, key.TypeId)).ShouldBe(new[] { ("Surrogate_Customer", "Surrogate"), ("PK_Customer", "PrimaryKey") });
        var idField = Container(store, "Customer").Fields.Single(field => field.Name == "Id");
        keys.Single(key => key.TypeId == "PrimaryKey").KeyFields.Single().DataContainerFieldId.ShouldBe(idField.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AForeignKeyInsideTheDiscoveryIsStoredWithTheKeyItReferences()
    {
        var store = new DataStoreImplementationConfiguration();

        DiscoveredSchemaMerge.Merge(
            store,
            Discover(
                Table("Customer", ["Id"], ["Id"]),
                Table("Order", ["Id", "CustomerId"], ["Id"], Foreign("FK_Order_Customer", "Customer", ("CustomerId", "Id")))),
            refresh: false);

        var customerKey = Container(store, "Customer").Keys.Single(key => key.TypeId == "PrimaryKey");
        var foreign = Container(store, "Order").Keys.Single(key => key.TypeId == "Foreign");
        foreign.Name.ShouldBe("FK_Order_Customer");
        foreign.ReferencedKeyId.ShouldBe(customerKey.Id);
        foreign.ReferencedContainerName.ShouldBe("Customer");
        foreign.ReferencedKeyName.ShouldBe("PK_Customer");
        var customerIdField = Container(store, "Order").Fields.Single(field => field.Name == "CustomerId");
        foreign.KeyFields.Single().DataContainerFieldId.ShouldBe(customerIdField.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AForeignKeyToAContainerOutsideTheDiscoveryIsStoredWithNoReferencedKey()
    {
        var store = new DataStoreImplementationConfiguration();

        DiscoveredSchemaMerge.Merge(
            store,
            Discover(Table("Order", ["Id", "CustomerId"], ["Id"], Foreign("FK_Order_Customer", "Customer", ("CustomerId", "Id")))),
            refresh: false);

        var foreign = Container(store, "Order").Keys.Single(key => key.TypeId == "Foreign");
        foreign.ReferencedKeyId.ShouldBeNull();
        foreign.KeyFields.Single().DataContainerFieldId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AForeignKeyThatNamesOtherColumnsThanTheTargetsPrimaryKeyIsStoredWithNoReferencedKey()
    {
        var store = new DataStoreImplementationConfiguration();

        DiscoveredSchemaMerge.Merge(
            store,
            Discover(
                Table("Customer", ["Id", "Code"], ["Id"]),
                Table("Order", ["Id", "CustomerCode"], ["Id"], Foreign("FK_Order_Customer", "Customer", ("CustomerCode", "Code")))),
            refresh: false);

        Container(store, "Order").Keys.Single(key => key.TypeId == "Foreign").ReferencedKeyId.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARefreshAddsTheKeysAContainerDiscoveredBeforeLacksAndKeepsItsSurrogateKey()
    {
        // A store discovered before 1.6.0 holds the container with its Surrogate key and no other.
        var store = new DataStoreImplementationConfiguration();
        DiscoveredSchemaMerge.Merge(store, Discover(Table("Customer", ["Id"], ["Id"])), refresh: false);
        var surrogate = Container(store, "Customer").Keys.Single(key => key.TypeId == "Surrogate");
        Container(store, "Customer").Keys.RemoveAll(key => key.TypeId != "Surrogate");

        DiscoveredSchemaMerge.Merge(store, Discover(Table("Customer", ["Id"], ["Id"])), refresh: true);

        var keys = Container(store, "Customer").Keys;
        keys.Count(key => key.TypeId == "Surrogate").ShouldBe(1);
        keys.Single(key => key.TypeId == "Surrogate").Id.ShouldBe(surrogate.Id);
        keys.Count(key => key.TypeId == "PrimaryKey").ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASecondRefreshAddsNoKeyTwice()
    {
        var store = new DataStoreImplementationConfiguration();
        var specs = new[]
        {
            Table("Customer", ["Id"], ["Id"]),
            Table("Order", ["Id", "CustomerId"], ["Id"], Foreign("FK_Order_Customer", "Customer", ("CustomerId", "Id"))),
        };
        DiscoveredSchemaMerge.Merge(store, Discover(specs), refresh: false);
        var before = store.Paths.Single().Containers.Select(container => container.Keys.Count).ToList();

        DiscoveredSchemaMerge.Merge(store, Discover(specs), refresh: true);
        DiscoveredSchemaMerge.Merge(store, Discover(specs), refresh: true);

        store.Paths.Single().Containers.Select(container => container.Keys.Count).ShouldBe(before);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void APlainDiscoveryLeavesTheKeysOfAContainerTheStoreAlreadyHasAsTheyWere()
    {
        var store = new DataStoreImplementationConfiguration();
        DiscoveredSchemaMerge.Merge(store, Discover(Table("Customer", ["Id"], ["Id"])), refresh: false);
        Container(store, "Customer").Keys.RemoveAll(key => key.TypeId != "Surrogate");

        DiscoveredSchemaMerge.Merge(store, Discover(Table("Customer", ["Id"], ["Id"])), refresh: false);

        Container(store, "Customer").Keys.Select(key => key.TypeId).ShouldBe(new[] { "Surrogate" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TheKeysSurviveStoringAndBuildingTheStoreAgain()
    {
        var store = new DataStoreImplementationConfiguration { Id = Guid.NewGuid(), Name = "Discovered" };
        DiscoveredSchemaMerge.Merge(
            store,
            Discover(
                Table("OrderLine", ["OrderId", "LineNo", "Qty"], ["OrderId", "LineNo"]),
                Table("Shipment", ["Id", "OrderId", "LineNo"], ["Id"], Foreign("FK_Shipment_Line", "OrderLine", ("OrderId", "OrderId"), ("LineNo", "LineNo")))),
            refresh: false);
        foreach (var path in store.Paths)
            path.Id = Guid.NewGuid();

        var builder = new GenericDataStoreBuilder(FormatTypes.NotFound);
        builder.Configure(store).IsSuccess.ShouldBeTrue();
        var built = await builder.Build(TestContext.Current.CancellationToken);
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var containers = built.Value!.Paths.Single().Containers;

        containers.Single(container => container.Name == "OrderLine").PrimaryKeyFieldNames().ShouldBe(new[] { "OrderId", "LineNo" });
        var foreign = containers.Single(container => container.Name == "Shipment").Keys.Single(key => key.KeyType.Name == "Foreign");
        foreign.ReferencedContainer!.Name.ShouldBe("OrderLine");
        foreign.KeyFields.Select(field => (field.LocalField.Name, field.ReferencedField!.Name))
            .ShouldBe(new[] { ("OrderId", "OrderId"), ("LineNo", "LineNo") });
    }
}
