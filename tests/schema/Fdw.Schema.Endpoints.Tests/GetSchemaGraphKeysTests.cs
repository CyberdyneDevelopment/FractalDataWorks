using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Schema.Endpoints.Discovery;
using Fdw.Services.Data.DataNodes;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Schema.Endpoints.Tests;

/// <summary>
/// The schema graph states what the catalog declares about keys (FDW-6309): a primary key's columns, every foreign key column, and one
/// relationship for each column pair of a foreign key whose container and target are both in the graph.
/// </summary>
public sealed class GetSchemaGraphKeysTests
{
    private static readonly DiscoveredKeyMessages Messages = new(
        (key, schema, table, referencedSchema, referencedTable) => Message($"{key} -> {referencedSchema}.{referencedTable}"),
        (key, schema, table, column) => Message($"{key} {column}"));

    private sealed class Endpoint : GetSchemaGraphEndpointBase
    {
        protected override Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverContainers(string connectionName, CancellationToken ct)
            => throw new NotSupportedException("The conversion is tested; discovery is not.");

        public SchemaGraphResponse Convert(string? schemaFilter, IReadOnlyList<IStorageContainer> containers)
            => ConvertToSchemaGraph(new GetSchemaGraphRequest { ConnectionName = "Db", SchemaFilter = schemaFilter }, containers);

        public static SchemaGraphResponse WithWarnings(SchemaGraphResponse graph, IGenericResult discovery)
        {
            AddWarnings(graph, discovery);
            return graph;
        }
    }

    private static IGenericMessage Message(string text, string? code = null)
    {
        var message = new Mock<IGenericMessage>();
        message.Setup(m => m.Message).Returns(text);
        message.Setup(m => m.Code).Returns(code);
        return message.Object;
    }

    private static Field Column(string name) => new Field
    {
        Name = name,
        FieldType = new SimpleFieldType { TypeName = "int", ClrType = typeof(int) },
        Role = DatumTypes.NotYetTyped,
    };

    private static DiscoveredContainerSpec Table(
        string schema, string name, string[] columns, string[] primaryKey, params DiscoveredForeignKeySpec[] foreignKeys)
    {
        var type = new Mock<IContainerType>();
        type.Setup(t => t.SupportedOperations).Returns(new[] { "Query", "Insert", "Update", "Delete" });
        type.Setup(t => t.Name).Returns("Table");
        var path = new Mock<IPath>();
        path.Setup(p => p.PathValue).Returns(schema);
        return new DiscoveredContainerSpec(
            name, schema, type.Object, FormatTypes.Tabular, path.Object, [.. columns.Select(Column)],
            new Dictionary<string, object>(), primaryKey, foreignKeys);
    }

    private static DiscoveredForeignKeySpec Foreign(string name, string schema, string table, params (string Column, string Referenced)[] pairs)
        => new(name, [.. pairs.Select(pair => new DiscoveredForeignKeySpecColumn(pair.Column, pair.Referenced))], schema, table);

    private static IReadOnlyList<IStorageContainer> Discover(params DiscoveredContainerSpec[] specs)
        => DiscoveredContainers.Assemble(specs, Messages).Value!;

    private static SchemaGraphFieldDto Field(SchemaGraphResponse graph, string entity, string field)
        => graph.Entities.Single(e => e.FullName == entity).Fields.Single(f => f.Name == field);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void EveryColumnOfAPrimaryKeyIsMarkedAndNoOtherColumnIs()
    {
        var graph = new Endpoint().Convert(null, Discover(Table("dbo", "OrderLine", ["OrderId", "LineNo", "Qty"], ["OrderId", "LineNo"])));

        Field(graph, "dbo.OrderLine", "OrderId").IsPrimaryKey.ShouldBeTrue();
        Field(graph, "dbo.OrderLine", "LineNo").IsPrimaryKey.ShouldBeTrue();
        Field(graph, "dbo.OrderLine", "Qty").IsPrimaryKey.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheEntityTypeIsTheContainerType()
    {
        var graph = new Endpoint().Convert(null, Discover(Table("dbo", "Customer", ["Id"], ["Id"])));

        graph.Entities.Single().EntityType.ShouldBe("Table");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AForeignKeyColumnIsMarkedAndOneRelationshipIsDrawnAcrossSchemas()
    {
        var graph = new Endpoint().Convert(
            null,
            Discover(
                Table("asset", "DeliveryZone", ["Code"], ["Code"]),
                Table("prc", "Price", ["RowId", "DeliveryZoneCode"], ["RowId"], Foreign("FK_Price_Zone", "asset", "DeliveryZone", ("DeliveryZoneCode", "Code")))));

        Field(graph, "prc.Price", "DeliveryZoneCode").IsForeignKey.ShouldBeTrue();
        Field(graph, "prc.Price", "RowId").IsForeignKey.ShouldBeFalse();
        var relationship = graph.Relationships.Single();
        relationship.SourceEntity.ShouldBe("prc.Price");
        relationship.SourceColumn.ShouldBe("DeliveryZoneCode");
        relationship.TargetEntity.ShouldBe("asset.DeliveryZone");
        relationship.TargetColumn.ShouldBe("Code");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACompositeForeignKeyDrawsOneRelationshipForEachColumnPair()
    {
        var graph = new Endpoint().Convert(
            null,
            Discover(
                Table("dbo", "OrderLine", ["OrderId", "LineNo"], ["OrderId", "LineNo"]),
                Table("dbo", "Shipment", ["OrderId", "LineNo"], [], Foreign("FK_Line", "dbo", "OrderLine", ("OrderId", "OrderId"), ("LineNo", "LineNo")))));

        graph.Relationships.Select(r => (r.SourceColumn, r.TargetColumn)).ShouldBe(new[] { ("OrderId", "OrderId"), ("LineNo", "LineNo") });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AForeignKeyWhoseTargetWasNotDiscoveredIsMarkedAndDrawsNoRelationship()
    {
        var graph = new Endpoint().Convert(
            null,
            Discover(Table("prc", "Price", ["RowId", "DeliveryZoneCode"], ["RowId"], Foreign("FK_Price_Zone", "asset", "DeliveryZone", ("DeliveryZoneCode", "Code")))));

        Field(graph, "prc.Price", "DeliveryZoneCode").IsForeignKey.ShouldBeTrue();
        graph.Relationships.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASchemaFilterThatLeavesTheTargetOutDrawsNoRelationshipToIt()
    {
        var graph = new Endpoint().Convert(
            "prc",
            Discover(
                Table("asset", "DeliveryZone", ["Code"], ["Code"]),
                Table("prc", "Price", ["RowId", "DeliveryZoneCode"], ["RowId"], Foreign("FK_Price_Zone", "asset", "DeliveryZone", ("DeliveryZoneCode", "Code")))));

        graph.Entities.Select(e => e.FullName).ShouldBe(new[] { "prc.Price" });
        graph.Relationships.ShouldBeEmpty();
        Field(graph, "prc.Price", "DeliveryZoneCode").IsForeignKey.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TheWarningsOfTheDiscoveryAreCarriedOntoTheGraph()
    {
        var discovery = GenericResult<int>.Success(
            1,
            ResultStatuses.ByName("SuccessWithWarnings"),
            Message("Foreign key 'FK_Price_Zone' references 'asset.DeliveryZone', which was not read", "MSSQL-41009"));

        var graph = Endpoint.WithWarnings(new SchemaGraphResponse(), discovery);

        var warning = graph.Warnings.Single();
        warning.Code.ShouldBe("MSSQL-41009");
        warning.Message.ShouldContain("asset.DeliveryZone");
    }
}
