using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests;

/// <summary>
/// A Compound or Federated data set converts to a Graph data set (FDW-6973): its sources Source components, its joins MergeJoins over the stream so
/// far, a union a UnionAll, each source's mappings and chains the input mappings of the input it feeds; every part with no Graph place refused.
/// </summary>
public sealed class JoinedToGraphTests
{
    private static readonly Guid TrimId = Guid.Parse("69730000-0000-0000-0000-000000000001");

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyList<AttachedFieldTransformation>>> NoChains =
        new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<AttachedFieldTransformation>>>();

    private static DataSetSourceConfiguration Source(string name, params (string Logical, string Physical)[] mappings) => new()
    {
        Name = name,
        SourceName = name,
        SourceKind = "DataStore",
        DataStoreName = "Sales",
        PathValue = "dbo",
        ContainerName = name,
        Mappings = [.. mappings.Select(m => new DataSetFieldMappingConfiguration { Id = Guid.CreateVersion7(), Name = m.Logical, LogicalFieldName = m.Logical, PhysicalFieldName = m.Physical })],
    };

    private static DataSetJoinConfiguration Join(int ordinal, string left, string leftField, string right, string rightField, string type) =>
        new() { Ordinal = ordinal, LeftSourceName = left, LeftFieldName = leftField, RightSourceName = right, RightFieldName = rightField, JoinType = type };

    private static CompoundDataSetConfiguration OrdersCustomers(string type) => new()
    {
        Name = "OrderCustomers",
        Implementation = "Compound",
        Fields = [new DataSetFieldConfiguration { Name = "OrderId", Ordinal = 0 }, new DataSetFieldConfiguration { Name = "CustomerName", Ordinal = 1 }],
        Sources = [Source("Orders", ("OrderId", "order_id"), ("CustomerCode", "customer_code")), Source("Customers", ("CustomerCode", "code"), ("CustomerName", "name"))],
        SourceCombination = "Join",
        Joins = [Join(0, "Orders", "CustomerCode", "Customers", "CustomerCode", type)],
    };

    [Fact]
    [Trait("Category", "DataSets.FDW6973")]
    public void AJoinBecomesAMergeJoinFedByBothSourcesWithTheirMappingsAsInputMappings()
    {
        var converted = JoinedToGraph.Convert(OrdersCustomers("Inner"), 100, NoChains, NullLogger.Instance);

        converted.IsSuccess.ShouldBeTrue(converted.CurrentMessage);
        var graph = converted.Value!;
        var join = graph.Components.Single(c => string.Equals(c.ComponentType, "MergeJoin", StringComparison.Ordinal));
        join.JoinKind.ShouldBe("Inner");
        join.RightPrefix.ShouldBe("Customers_");
        join.Columns.Select(c => (c.SettingName, c.ColumnName)).ShouldBe([("leftKeys", "CustomerCode"), ("rightKeys", "CustomerCode")]);
        graph.Links.Select(l => (l.FromComponent, l.ToComponent, l.ToInput)).ShouldBe([("Orders", "join1", "left"), ("Customers", "join1", "right")]);
        join.Fields.Where(f => string.Equals(f.InputName, "right", StringComparison.Ordinal)).Select(f => (f.TargetField, f.SourceField))
            .ShouldContain(("CustomerName", "name"));
        graph.FieldsOutComponent.ShouldBe("join1");
        graph.GraphFields.Single(f => string.Equals(f.Name, "OrderId", StringComparison.Ordinal)).FedByField.ShouldBe("OrderId");
        graph.GraphFields.Single(f => string.Equals(f.Name, "CustomerName", StringComparison.Ordinal)).FedByField.ShouldBe("Customers_CustomerName");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6973")]
    public void FiltersBecomeAFilterComponentAfterTheJoinByTheNamesTheJoinGivesTheFields()
    {
        var orders = OrdersCustomers("Inner");
        orders.Filters = [new DataSetFilterConditionConfiguration { FieldName = "CustomerName", Operator = "StartsWith", Value = "A", DataType = "String" }];

        var converted = JoinedToGraph.Convert(orders, 100, NoChains, NullLogger.Instance);

        converted.IsSuccess.ShouldBeTrue(converted.CurrentMessage);
        var graph = converted.Value!;
        var filter = graph.Components.Single(c => string.Equals(c.ComponentType, "Filter", StringComparison.Ordinal));
        filter.Stages.ShouldHaveSingleItem().FilterExpression.ShouldBe("Customers_CustomerName startswith \"A\"", "the right side's field carries the join's prefix");
        graph.Links.ShouldContain(l => string.Equals(l.FromComponent, "join1", StringComparison.Ordinal) && string.Equals(l.ToComponent, GraphFilters.ComponentName, StringComparison.Ordinal));
        graph.FieldsOutComponent.ShouldBe(GraphFilters.ComponentName);
        graph.GraphFields.Single(f => string.Equals(f.Name, "CustomerName", StringComparison.Ordinal)).FedByComponent.ShouldBe(GraphFilters.ComponentName);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6973")]
    public void ARightJoinIsTheLeftJoinOfItsSidesTurned()
    {
        var converted = JoinedToGraph.Convert(OrdersCustomers("Right"), 100, NoChains, NullLogger.Instance);

        converted.IsSuccess.ShouldBeTrue(converted.CurrentMessage);
        var join = converted.Value!.Components.Single(c => string.Equals(c.ComponentType, "MergeJoin", StringComparison.Ordinal));
        join.JoinKind.ShouldBe("LeftOuter");
        converted.Value.Links.Single(l => string.Equals(l.ToInput, "left", StringComparison.Ordinal)).FromComponent.ShouldBe("Customers");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6973")]
    public void AChainedJoinTakesTheStreamSoFarOnItsLeft()
    {
        var data = OrdersCustomers("Left");
        data.Sources = [.. data.Sources, Source("Regions", ("RegionCode", "code"), ("RegionName", "name"))];
        data.Sources[1].Mappings = [.. data.Sources[1].Mappings, new DataSetFieldMappingConfiguration { Id = Guid.CreateVersion7(), Name = "RegionCode", LogicalFieldName = "RegionCode", PhysicalFieldName = "region" }];
        data.Fields = [.. data.Fields, new DataSetFieldConfiguration { Name = "RegionName", Ordinal = 2 }];
        data.Joins = [.. data.Joins, Join(1, "Customers", "RegionCode", "Regions", "RegionCode", "Inner")];

        var converted = JoinedToGraph.Convert(data, 100, NoChains, NullLogger.Instance);

        converted.IsSuccess.ShouldBeTrue(converted.CurrentMessage);
        var second = converted.Value!.Components.Single(c => string.Equals(c.Name, "join2", StringComparison.Ordinal));
        second.Columns.Single(c => string.Equals(c.SettingName, "leftKeys", StringComparison.Ordinal)).ColumnName.ShouldBe("Customers_RegionCode");
        converted.Value.Links.ShouldContain(l => string.Equals(l.FromComponent, "join1", StringComparison.Ordinal) && string.Equals(l.ToComponent, "join2", StringComparison.Ordinal) && string.Equals(l.ToInput, "left", StringComparison.Ordinal));
        converted.Value.FieldsOutComponent.ShouldBe("join2");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6973")]
    public void AUnionBecomesAUnionAllFedByEverySource()
    {
        var union = new FederatedDataSetConfiguration
        {
            Name = "AllOrders",
            Implementation = "Federated",
            Fields = [new DataSetFieldConfiguration { Name = "OrderId", Ordinal = 0 }],
            Sources = [Source("Current", ("OrderId", "id")), Source("Archive", ("OrderId", "order_no"))],
            SourceCombination = "Union",
            FederationStrategy = "Sequential",
            FederationOptions = [new DataSetFederationOptionConfiguration { OptionName = "OrdersSources", OptionValue = "true" }],
        };
        var chains = new Dictionary<string, IReadOnlyDictionary<string, IReadOnlyList<AttachedFieldTransformation>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Archive"] = new Dictionary<string, IReadOnlyList<AttachedFieldTransformation>> { ["OrderId"] = [new AttachedFieldTransformation(TrimId, "Trim", 0)] },
        };

        var converted = JoinedToGraph.Convert(union, 100, chains, NullLogger.Instance);

        converted.IsSuccess.ShouldBeTrue(converted.CurrentMessage);
        var all = converted.Value!.Components.Single(c => string.Equals(c.ComponentType, "UnionAll", StringComparison.Ordinal));
        converted.Value.Links.Count(l => string.Equals(l.ToComponent, "union", StringComparison.Ordinal)).ShouldBe(2);
        all.Fields.Single(f => string.Equals(f.InputName, "Archive", StringComparison.Ordinal)).TransformationChain.ShouldHaveSingleItem().TransformationId.ShouldBe(TrimId);
        converted.Value.FederationStrategy.ShouldBe("Sequential", "a Federated data set's stated strategy becomes the Graph data set's");
        converted.Value.FederationOptions.ShouldHaveSingleItem().OptionName.ShouldBe("OrdersSources");
        converted.Value.GraphFields.ShouldHaveSingleItem().FedByField.ShouldBe("OrderId");
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6973")]
    public void ACrossJoinAndAnUnmappedSourceAreEachRefusedByName()
    {
        var federated = new FederatedDataSetConfiguration
        {
            Name = "Everything",
            Implementation = "Federated",
            FederationStrategy = "Parallel",
            Fields = [new DataSetFieldConfiguration { Name = "OrderId", Ordinal = 0 }],
            Sources = [Source("Orders", ("OrderId", "id")), Source("Blank")],
            SourceCombination = "Join",
            Joins = [Join(0, "Orders", "OrderId", "Blank", "OrderId", "Cross")],
        };

        var converted = JoinedToGraph.Convert(federated, 100, NoChains, NullLogger.Instance);

        converted.IsFailure.ShouldBeTrue();
        converted.Code!.Code.ShouldBe("DATASETS-20040");
        var message = converted.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("'Cross'");
        message.ShouldContain("source 'Blank'");
    }
}
