using System;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// A Simple data set converts to a Graph data set with the same identity and fields, its source a Source component feeding every field out
/// (FDW-6972); every part a Graph data set has no place for refuses the conversion by name, and none is dropped.
/// </summary>
public sealed class SimpleToGraphTests
{
    private static readonly Guid DataSetId = Guid.Parse("69720000-0000-0000-0000-000000000001");
    private static readonly Guid FieldId = Guid.Parse("69720000-0000-0000-0000-000000000002");

    private static SimpleDataSetConfiguration Orders() => new()
    {
        Id = DataSetId,
        Name = "Orders",
        Domain = "DataSet",
        Implementation = "Simple",
        Version = "2",
        Fields =
        [
            new DataSetFieldConfiguration { Id = FieldId, Name = "OrderId", Ordinal = 0, IsKey = true },
            new DataSetFieldConfiguration { Name = "Total", Ordinal = 1 },
        ],
        Sources =
        [
            new DataSetSourceConfiguration
            {
                Name = "Orders", SourceName = "Orders", SourceKind = "DataStore", DataStoreName = "Sales", PathValue = "dbo", ContainerName = "Order",
                Mappings = [new DataSetFieldMappingConfiguration { Id = Guid.CreateVersion7(), Name = "OrderId", LogicalFieldName = "OrderId", PhysicalFieldName = "order_id" }],
            },
        ],
        KeyFields = [new DataSetKeyFieldConfiguration { KeyName = "OrderId", KeyType = "Surrogate", Ordinal = 0 }],
    };

    [Fact]
    [Trait("Category", "DataSets.FDW6972")]
    public void ASimpleDataSetBecomesAGraphWithItsIdentityFieldsAndOneSourceComponent()
    {
        var converted = SimpleToGraph.Convert(Orders(), 500, [], NullLogger.Instance);

        converted.IsSuccess.ShouldBeTrue(converted.CurrentMessage);
        var graph = converted.Value.ShouldNotBeNull();
        graph.Id.ShouldBe(DataSetId);
        graph.Name.ShouldBe("Orders");
        graph.Implementation.ShouldBe("Graph");
        graph.Version.ShouldBe("2");

        var source = graph.Components.ShouldHaveSingleItem();
        source.ComponentType.ShouldBe("ContainerSource");
        source.DataStoreName.ShouldBe("Sales");
        source.PathName.ShouldBe("dbo");
        source.ContainerName.ShouldBe("Order");
        source.BatchSize.ShouldBe(500);
        graph.FieldsOutComponent.ShouldBe("Orders");
        graph.FieldsOutOutput.ShouldBe("output");

        var orderId = graph.GraphFields.Single(field => string.Equals(field.Name, "OrderId", StringComparison.Ordinal));
        orderId.Id.ShouldBe(FieldId);
        orderId.IsKey.ShouldBeTrue();
        orderId.FedByComponent.ShouldBe("Orders");
        orderId.FedByField.ShouldBe("order_id", "a mapped field is fed by the column its mapping names");
        graph.GraphFields.Single(field => string.Equals(field.Name, "Total", StringComparison.Ordinal)).FedByField.ShouldBe("Total", "an unmapped field is read by its own name, as the Simple read does");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6972")]
    public void ADataSetSourceBecomesADataSetSourceComponent()
    {
        var orders = Orders();
        orders.Sources[0].SourceKind = "DataSet";
        orders.Sources[0].SourceDataSetName = "RawOrders";

        var converted = SimpleToGraph.Convert(orders, 100, [], NullLogger.Instance);

        converted.IsSuccess.ShouldBeTrue(converted.CurrentMessage);
        var source = converted.Value!.Components.ShouldHaveSingleItem();
        source.ComponentType.ShouldBe("DataSetSource");
        source.DataSetName.ShouldBe("RawOrders");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6972")]
    public void EveryPartWithNoGraphPlaceIsNamedInOneRefusal()
    {
        var orders = Orders();
        orders.Aggregates = [new DataSetAggregateConfiguration { Name = "TotalByDay" }];
        orders.Filters = [new DataSetFilterConditionConfiguration { FieldName = "Total", Operator = "In", Value = "1,2", DataType = "Decimal" }];
        orders.Fields[1].DefaultValue = "0";

        var converted = SimpleToGraph.Convert(orders, 100, ["OrderId"], NullLogger.Instance);

        converted.IsFailure.ShouldBeTrue();
        converted.Code!.Code.ShouldBe("DATASETS-20040");
        var message = converted.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("aggregate 'TotalByDay'");
        message.ShouldContain("filter on 'Total' (In 1,2)", customMessage: "an operator the condition language cannot state is refused by name");
        message.ShouldContain("the transformation chain of field 'OrderId'");
        message.ShouldContain("the default value of field 'Total'");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6972")]
    public void FiltersBecomeOneFilterComponentAfterTheSourceByTheColumnNames()
    {
        var orders = Orders();
        orders.Filters =
        [
            new DataSetFilterConditionConfiguration { FieldName = "Total", Operator = "GreaterThan", Value = "0", DataType = "Decimal", Ordinal = 1 },
            new DataSetFilterConditionConfiguration { FieldName = "OrderId", Operator = "IsNotNull", DataType = "Int32", Ordinal = 0 },
            new DataSetFilterConditionConfiguration { FieldName = "Total", Operator = "Equal", Value = "9", DataType = "Decimal", Ordinal = 2, IsCurrent = false },
        ];

        var converted = SimpleToGraph.Convert(orders, 100, [], NullLogger.Instance);

        converted.IsSuccess.ShouldBeTrue(converted.CurrentMessage);
        var graph = converted.Value!;
        graph.Components.Count.ShouldBe(2);
        var filter = graph.Components[1];
        filter.Name.ShouldBe(GraphFilters.ComponentName);
        filter.ComponentType.ShouldBe("Filter");
        var stage = filter.Stages.ShouldHaveSingleItem();
        stage.OperationType.ShouldBe("Filter");
        stage.FilterExpression.ShouldBe("order_id != null and Total > 0", "conditions in ordinal order, by the name the source gives the field, a number bare, a retired condition left out");
        var link = graph.Links.ShouldHaveSingleItem();
        (link.FromComponent, link.FromOutput, link.ToComponent, link.ToInput).ShouldBe(("Orders", "output", GraphFilters.ComponentName, "input"));
        graph.FieldsOutComponent.ShouldBe(GraphFilters.ComponentName);
        graph.GraphFields.ShouldAllBe(field => string.Equals(field.FedByComponent, GraphFilters.ComponentName, StringComparison.Ordinal));
        graph.GraphFields.Single(field => string.Equals(field.Name, "OrderId", StringComparison.Ordinal)).FedByField.ShouldBe("order_id");
    }

    [Theory]
    [Trait("Category", "DataSets.FDW6972")]
    [InlineData("String", "Contains", "north", "Total contains \"north\"")]
    [InlineData("String", "Equal", "say \"hi\"", "Total == 'say \"hi\"'")]
    [InlineData("Int32", "LessThanOrEqual", "12", "Total <= 12")]
    [InlineData("Boolean", "Equal", "true", "Total == true")]
    [InlineData("DateTime", "GreaterThanOrEqual", "2026-01-01", "Total >= \"2026-01-01\"")]
    public void AValueIsWrittenBareWhenItIsANumberOrTrueFalseAndQuotedOtherwise(string dataType, string op, string value, string expected)
    {
        var orders = Orders();
        orders.Filters = [new DataSetFilterConditionConfiguration { FieldName = "Total", Operator = op, Value = value, DataType = dataType }];

        var converted = SimpleToGraph.Convert(orders, 100, [], NullLogger.Instance);

        converted.IsSuccess.ShouldBeTrue(converted.CurrentMessage);
        converted.Value!.Components[1].Stages.ShouldHaveSingleItem().FilterExpression.ShouldBe(expected);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6972")]
    public void AValueHoldingBothQuotesAndASourceNamedFilterAreRefused()
    {
        var orders = Orders();
        orders.Sources[0].SourceName = GraphFilters.ComponentName;
        orders.Filters = [new DataSetFilterConditionConfiguration { FieldName = "Total", Operator = "Equal", Value = "it's \"x\"", DataType = "String" }];

        var converted = SimpleToGraph.Convert(orders, 100, [], NullLogger.Instance);

        converted.IsFailure.ShouldBeTrue();
        var message = converted.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("filter on 'Total'");
        message.ShouldContain("a component is already named 'filter'");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6972")]
    public void KeyFieldsInAnotherOrderThanTheirFieldsAreRefused()
    {
        var orders = Orders();
        orders.Fields[1].IsKey = true;
        orders.KeyFields =
        [
            new DataSetKeyFieldConfiguration { KeyName = "Total", KeyType = "Surrogate", Ordinal = 0 },
            new DataSetKeyFieldConfiguration { KeyName = "OrderId", KeyType = "Surrogate", Ordinal = 1 },
        ];

        var converted = SimpleToGraph.Convert(orders, 100, [], NullLogger.Instance);

        converted.IsFailure.ShouldBeTrue();
        converted.CurrentMessage.ShouldNotBeNull().ShouldContain("key fields Total, OrderId");
    }

    [Theory]
    [Trait("Category", "DataSets.FDW6972")]
    [InlineData(0)]
    [InlineData(-5)]
    public void ABatchSizeBelowOneIsRefused(int batchSize)
        => SimpleToGraph.Convert(Orders(), batchSize, [], NullLogger.Instance).IsFailure.ShouldBeTrue();

    [Fact]
    [Trait("Category", "DataSets.FDW6972")]
    public void AGraphDataSetIsRefusedWithItsOwnCode()
    {
        var converted = SimpleToGraph.Convert(new GraphDataSetConfiguration { Name = "Already" }, 100, [], NullLogger.Instance);

        converted.IsFailure.ShouldBeTrue();
        converted.Code!.Code.ShouldBe("DATASETS-20041");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6972")]
    public void ACompoundDataSetIsLeftToItsOwnRoute()
    {
        var converted = SimpleToGraph.Convert(new CompoundDataSetConfiguration { Name = "Joined", Implementation = "Compound" }, 100, [], NullLogger.Instance);

        converted.IsFailure.ShouldBeTrue();
        converted.CurrentMessage.ShouldNotBeNull().ShouldContain("Compound");
    }
}
