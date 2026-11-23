using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Schema;
using Fdw.Services.Data.DataNodes;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataNodes.Tests;

/// <summary>
/// The containers a schema discovery returns carry the primary key and the foreign keys the catalog declares on them, in the
/// <see cref="IContainerKey"/> model (FDW-6309). A foreign key whose target the discovery did not read stays on its container, and
/// one coded warning says which key and which target.
/// </summary>
public sealed class DiscoveredContainersTests
{
    private static readonly DiscoveredKeyMessages Messages = new(
        (key, schema, table, referencedSchema, referencedTable) =>
            Message($"target:{key}:{schema}.{table}->{referencedSchema}.{referencedTable}"),
        (key, schema, table, column) => Message($"column:{key}:{schema}.{table}.{column}"));

    private static IGenericMessage Message(string text)
    {
        var message = new Mock<IGenericMessage>();
        message.Setup(m => m.Message).Returns(text);
        return message.Object;
    }

    private static Field Column(string name) => new Field
    {
        Name = name,
        FieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) },
        Role = DatumTypes.NotYetTyped,
    };

    private static DiscoveredContainerSpec Table(
        string schema,
        string name,
        string[] columns,
        string[]? primaryKey = null,
        params DiscoveredForeignKeySpec[] foreignKeys)
    {
        var type = new Mock<IContainerType>();
        type.Setup(t => t.SupportedOperations).Returns(new[] { "Query", "Insert", "Update", "Delete" });
        type.Setup(t => t.Name).Returns("Table");
        var path = new Mock<IPath>();
        path.Setup(p => p.PathValue).Returns(schema);
        return new DiscoveredContainerSpec(
            name,
            schema,
            type.Object,
            FormatTypes.Tabular,
            path.Object,
            [.. columns.Select(Column)],
            new Dictionary<string, object>(),
            primaryKey ?? [],
            foreignKeys);
    }

    private static DiscoveredForeignKeySpec Foreign(string name, string referencedSchema, string referencedTable, params (string Column, string Referenced)[] pairs)
        => new(name, [.. pairs.Select(pair => new DiscoveredForeignKeySpecColumn(pair.Column, pair.Referenced))], referencedSchema, referencedTable);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASingleColumnPrimaryKeyIsCarriedAsAPrimaryKeyOfTheContainer()
    {
        var result = DiscoveredContainers.Assemble([Table("dbo", "Customer", ["Id", "Name"], ["Id"])], Messages);

        result.IsSuccess.ShouldBeTrue();
        var key = result.Value!.Single().Keys.Single();
        key.KeyType.Name.ShouldBe("PrimaryKey");
        key.IsPhysical.ShouldBeTrue();
        key.KeyFields.Select(field => field.LocalField.Name).ShouldBe(new[] { "Id" });
        result.Value!.Single().PrimaryKeyFieldNames().ShouldBe(new[] { "Id" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACompositePrimaryKeyIsCarriedWholeInKeyOrder()
    {
        var result = DiscoveredContainers.Assemble([Table("dbo", "OrderLine", ["Qty", "OrderId", "LineNo"], ["OrderId", "LineNo"])], Messages);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Single().PrimaryKeyFieldNames().ShouldBe(new[] { "OrderId", "LineNo" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AContainerWithNoKeyCarriesNone()
    {
        var result = DiscoveredContainers.Assemble([Table("dbo", "Log", ["Message"])], Messages);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Single().Keys.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AForeignKeyInsideTheDiscoveryIsResolvedToItsTargetContainerAndField()
    {
        var customer = Table("dbo", "Customer", ["Id"], ["Id"]);
        var order = Table("sales", "Order", ["Id", "CustomerId"], ["Id"], Foreign("FK_Order_Customer", "dbo", "Customer", ("CustomerId", "Id")));

        var result = DiscoveredContainers.Assemble([order, customer], Messages);

        result.IsSuccess.ShouldBeTrue();
        result.Messages.ShouldBeEmpty();
        var foreignKey = result.Value!.Single(container => container.Name == "Order").Keys.Single(key => key.KeyType.Name == "Foreign");
        foreignKey.KeyName.ShouldBe("FK_Order_Customer");
        foreignKey.ReferencedContainer!.Name.ShouldBe("Customer");
        var pair = foreignKey.KeyFields.Single();
        pair.LocalField.Name.ShouldBe("CustomerId");
        pair.ReferencedField!.Name.ShouldBe("Id");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheTargetContainerListsTheForeignKeysThatReferenceIt()
    {
        var customer = Table("dbo", "Customer", ["Id"], ["Id"]);
        var order = Table("sales", "Order", ["Id", "CustomerId"], ["Id"], Foreign("FK_Order_Customer", "dbo", "Customer", ("CustomerId", "Id")));

        var result = DiscoveredContainers.Assemble([order, customer], Messages);

        var inbound = result.Value!.Single(container => container.Name == "Customer").ReferencingKeys;
        inbound.IsSuccess.ShouldBeTrue();
        inbound.Value!.Single().Key.KeyName.ShouldBe("FK_Order_Customer");
        inbound.Value!.Single().Owner.Name.ShouldBe("Order");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoContainersThatReferenceEachOtherAreBothResolved()
    {
        var a = Table("dbo", "A", ["Id", "BId"], ["Id"], Foreign("FK_A_B", "dbo", "B", ("BId", "Id")));
        var b = Table("dbo", "B", ["Id", "AId"], ["Id"], Foreign("FK_B_A", "dbo", "A", ("AId", "Id")));

        var result = DiscoveredContainers.Assemble([a, b], Messages);

        result.IsSuccess.ShouldBeTrue();
        result.Messages.ShouldBeEmpty();
        result.Value!.Single(container => container.Name == "A").Keys.Single(key => key.KeyType.Name == "Foreign").ReferencedContainer!.Name.ShouldBe("B");
        result.Value!.Single(container => container.Name == "B").Keys.Single(key => key.KeyType.Name == "Foreign").ReferencedContainer!.Name.ShouldBe("A");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACompositeForeignKeyKeepsEveryColumnPair()
    {
        var line = Table("dbo", "OrderLine", ["OrderId", "LineNo"], ["OrderId", "LineNo"]);
        var shipment = Table("dbo", "Shipment", ["OrderId", "LineNo"], null, Foreign("FK_Shipment_Line", "dbo", "OrderLine", ("OrderId", "OrderId"), ("LineNo", "LineNo")));

        var result = DiscoveredContainers.Assemble([line, shipment], Messages);

        var key = result.Value!.Single(container => container.Name == "Shipment").Keys.Single();
        key.KeyFields.Select(field => (field.LocalField.Name, field.ReferencedField!.Name)).ShouldBe(new[] { ("OrderId", "OrderId"), ("LineNo", "LineNo") });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AForeignKeyWhoseTargetWasNotDiscoveredStaysAsAForeignKeyAndIsSaidOnce()
    {
        var order = Table("sales", "Order", ["Id", "CustomerId"], ["Id"], Foreign("FK_Order_Customer", "dbo", "Customer", ("CustomerId", "Id")));

        var result = DiscoveredContainers.Assemble([order], Messages);

        result.IsSuccess.ShouldBeTrue();
        var key = result.Value!.Single().Keys.Single(k => k.KeyType.Name == "Foreign");
        key.ReferencedContainer.ShouldBeNull();
        key.KeyFields.Single().LocalField.Name.ShouldBe("CustomerId");
        key.KeyFields.Single().ReferencedField.ShouldBeNull();
        result.Messages.Count.ShouldBe(1);
        result.Messages.Single().Message.ShouldBe("target:FK_Order_Customer:sales.Order->dbo.Customer");
        result.Status.Name.ShouldBe("SuccessWithWarnings");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AKeyNamingAColumnItsContainerLacksRefusesTheWholeDiscovery()
    {
        var order = Table("sales", "Order", ["Id"], ["Id", "Missing"]);

        var result = DiscoveredContainers.Assemble([order], Messages);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("column:PK_Order:sales.Order.Missing");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AForeignKeyNamingATargetColumnTheTargetLacksRefusesTheWholeDiscovery()
    {
        var customer = Table("dbo", "Customer", ["Id"], ["Id"]);
        var order = Table("sales", "Order", ["Id", "CustomerId"], ["Id"], Foreign("FK_Order_Customer", "dbo", "Customer", ("CustomerId", "Code")));

        var result = DiscoveredContainers.Assemble([order, customer], Messages);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("column:FK_Order_Customer:dbo.Customer.Code");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheDiscoveredColumnKeepsWhatTheDiscoveryReadOfIt()
    {
        var column = new Field
        {
            Name = "Id",
            FieldType = new SimpleFieldType { TypeName = "bigint", ClrType = typeof(long) },
            Role = DatumTypes.Identifier,
            IsIdentity = true,
        };
        var spec = Table("dbo", "Customer", ["Id"], ["Id"]) with { Fields = [column] };

        var result = DiscoveredContainers.Assemble([spec], Messages);

        var field = result.Value!.Single().Schema.Fields.Single();
        field.IsIdentity.ShouldBeTrue();
        field.FieldType.TypeName.ShouldBe("bigint");
        field.Role.ShouldBeSameAs(DatumTypes.Identifier);
    }
}
