using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Planning;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the planner that pushes a graph's same-store joins and lookups into their store (FDW-6969): what it pushes, the query it states, and each
/// reason it leaves a join, union or lookup in memory.
/// </summary>
public sealed class GraphPushdownPlannerTests
{
    private static readonly Guid Shop = Guid.CreateVersion7();
    private static readonly Guid Warehouse = Guid.CreateVersion7();

    private readonly Dictionary<string, PushdownContainer> _containers = new(StringComparer.Ordinal)
    {
        ["Orders"] = Container(Shop, ["Id"], ("Id", typeof(int)), ("CustomerId", typeof(int)), ("Total", typeof(decimal))),
        ["Customers"] = Container(Shop, ["Id"], ("Id", typeof(int)), ("Name", typeof(string)), ("RegionId", typeof(int))),
        ["Regions"] = Container(Shop, ["Id"], ("Id", typeof(int)), ("Title", typeof(string))),
        ["Stock"] = Container(Warehouse, ["Id"], ("Id", typeof(int)), ("CustomerId", typeof(int))),
        ["Notes"] = Container(Shop, [], ("CustomerId", typeof(int)), ("Text", typeof(string))),
        ["Codes"] = Container(Shop, ["Code"], ("Code", typeof(string)), ("CustomerCode", typeof(string))),
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AJoinOfTwoContainersOfOneStoreIsOneQueryInThatStore()
    {
        var graph = Joined("Inner", "Orders", "Customers", ["CustomerId"], ["Id"]);

        var plan = await Plan(graph);

        var pushed = plan.Pushed.ShouldHaveSingleItem();
        pushed.Top.ShouldBe("join");
        pushed.Members.ShouldBe(["left", "right", "join"], ignoreOrder: true);
        pushed.Target.Container.ShouldBe("Orders");
        var join = pushed.Query.Joins.ShouldHaveSingleItem();
        join.JoinType.ShouldBe("Inner");
        join.TargetContainerName.ShouldBe("Customers");
        join.SourceContainerName.ShouldBeNull();
        join.JoinConditions.ShouldBe([("CustomerId", "Id")]);
        pushed.Query.Projection.ShouldNotBeNull().Fields.Select(field => field.Alias).ShouldBe(["Id", "CustomerId", "Total", "c_Id", "c_Name", "c_RegionId"]);
        pushed.Query.Ordering.ShouldNotBeNull().OrderedFields.Select(field => field.PropertyName).ShouldBe(["Orders.Id", "Customers.Id"]);
        pushed.Absence.ShouldBeNull();
        plan.InMemory.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ATwoPairJoinIsOneJoinWithBothConditions()
    {
        var graph = Joined("Inner", "Orders", "Customers", ["CustomerId", "Id"], ["Id", "RegionId"]);

        var plan = await Plan(graph);

        plan.Pushed.ShouldHaveSingleItem().Query.Joins.ShouldHaveSingleItem().JoinConditions.ShouldBe([("CustomerId", "Id"), ("Id", "RegionId")]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFullJoinIsPushedWithItsLeftlessRowsSentLast()
    {
        var plan = await Plan(Joined("FullOuter", "Orders", "Customers", ["CustomerId"], ["Id"]));

        var pushed = plan.Pushed.ShouldHaveSingleItem();
        pushed.Query.Joins.ShouldHaveSingleItem().JoinType.ShouldBe("Full");
        pushed.Absence.ShouldNotBeNull().SentLast.ShouldBeTrue();
        pushed.Query.Projection.ShouldNotBeNull().Fields.Last().Alias.ShouldBe(PushdownJoins.LeftPresentField);
        pushed.Query.Projection.Fields.Last().SourceContainer.ShouldBe("Orders");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AJoinChainedToAJoinIsOneQueryAndTheLowerJoinIsNotATopOfItsOwn()
    {
        var graph = Graph(
            [Source("o", "Orders"), Source("c", "Customers"), Join("j1", "Inner", "c_", ["CustomerId"], ["Id"]), Source("r", "Regions"), Join("j2", "LeftOuter", "r_", ["c_RegionId"], ["Id"])],
            [Link("o", "j1", input: "left"), Link("c", "j1", input: "right"), Link("j1", "j2", input: "left"), Link("r", "j2", input: "right")]);

        var plan = await Plan(graph, new GraphEnd("j2", "output"));

        var pushed = plan.Pushed.ShouldHaveSingleItem();
        pushed.Top.ShouldBe("j2");
        pushed.Members.ShouldBe(["o", "c", "j1", "r", "j2"], ignoreOrder: true);
        pushed.Query.Joins.Count.ShouldBe(2);
        pushed.Query.Joins[1].SourceContainerName.ShouldBe("Customers");
        pushed.Query.Joins[1].JoinType.ShouldBe("Left");
        pushed.Query.Ordering.ShouldNotBeNull().OrderedFields.Select(field => field.PropertyName).ShouldBe(["Orders.Id", "Customers.Id", "Regions.Id"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AFullJoinUnderAnotherJoinStaysTheTopOfItsOwnQuery()
    {
        var graph = Graph(
            [Source("o", "Orders"), Source("c", "Customers"), Join("j1", "FullOuter", "c_", ["CustomerId"], ["Id"]), Source("r", "Regions"), Join("j2", "Inner", "r_", ["c_RegionId"], ["Id"])],
            [Link("o", "j1", input: "left"), Link("c", "j1", input: "right"), Link("j1", "j2", input: "left"), Link("r", "j2", input: "right")]);

        var plan = await Plan(graph, new GraphEnd("j2", "output"));

        plan.Pushed.ShouldHaveSingleItem().Top.ShouldBe("j1");
        plan.InMemory.ShouldHaveSingleItem().Component.ShouldBe("j2");
        plan.InMemory[0].Reason.ShouldContain("pushed only where its rows leave the query");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AJoinTheReadEndsAtIsNotSwallowedByAJoinAboveIt()
    {
        var graph = Graph(
            [Source("o", "Orders"), Source("c", "Customers"), Join("j1", "Inner", "c_", ["CustomerId"], ["Id"]), Source("r", "Regions"), Join("j2", "Inner", "r_", ["c_RegionId"], ["Id"])],
            [Link("o", "j1", input: "left"), Link("c", "j1", input: "right"), Link("j1", "j2", input: "left"), Link("r", "j2", input: "right")],
            Dropped("j2"));

        var plan = await Plan(graph, new GraphEnd("j1", "output"));

        plan.Pushed.ShouldHaveSingleItem().Top.ShouldBe("j1");
        plan.InMemory.ShouldHaveSingleItem().Component.ShouldBe("j2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AJoinAcrossTwoDataStoresRunsInMemory()
    {
        var plan = await Plan(Joined("Inner", "Orders", "Stock", ["CustomerId"], ["CustomerId"]));

        plan.Pushed.ShouldBeEmpty();
        plan.InMemory.ShouldHaveSingleItem().Reason.ShouldContain("two data stores");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheReadsOfAJoinAcrossTwoDataStoresAreFederatedWithTheRightInputAsTheBuildSide()
    {
        var plan = await Plan(Joined("Inner", "Orders", "Stock", ["CustomerId"], ["CustomerId"]));

        plan.FederatedReads.Select(read => (read.Component, read.Combiner, read.BuildSide)).ShouldBe([("left", "join", false), ("right", "join", true)], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AJoinInMemoryWithinOneStoreHasNoFederatedReads()
    {
        var plan = await Plan(Joined("Inner", "Customers", "Codes", ["Name"], ["Code"]));

        plan.InMemory.ShouldHaveSingleItem();
        plan.FederatedReads.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task ADataSetSourceIsAReadOfItsOwnSoAJoinOfItWithAContainerIsFederated()
    {
        var dataSet = new PipelineComponentConfiguration { Name = "right", ComponentType = "DataSetSource", DataSetName = "Refunds", BatchSize = 2 };
        var plan = await Plan(Graph([Source("left", "Orders"), dataSet, Join("join", "LeftOuter", "c_", ["Id"], ["OrderId"])], Lines()));

        plan.FederatedReads.Count.ShouldBe(2);
        plan.FederatedReads.Single(read => read.BuildSide).Component.ShouldBe("right");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AJoinOnATextKeyRunsInMemory()
    {
        var plan = await Plan(Joined("Inner", "Customers", "Codes", ["Name"], ["Code"]));

        plan.InMemory.ShouldHaveSingleItem().Reason.ShouldContain("text");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AContainerWithNoPrimaryKeyIsReadInMemory()
    {
        var plan = await Plan(Joined("Inner", "Orders", "Notes", ["CustomerId"], ["CustomerId"]));

        plan.InMemory.ShouldHaveSingleItem().Reason.ShouldContain("no primary key");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AStoreWhoseConnectionDoesNotJoinIsReadInMemory()
    {
        _containers["Customers"] = _containers["Customers"] with { JoinsInStore = false };

        var plan = await Plan(Joined("Inner", "Orders", "Customers", ["CustomerId"], ["Id"]));

        plan.InMemory.ShouldHaveSingleItem().Reason.ShouldContain("does not run joins");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AContainerNamedOtherwiseThanItIsStoredIsReadInMemory()
    {
        _containers["Customers"] = _containers["Customers"] with { NamedAsStored = false };

        var plan = await Plan(Joined("Inner", "Orders", "Customers", ["CustomerId"], ["Id"]));

        plan.InMemory.ShouldHaveSingleItem().Reason.ShouldContain("not stored under its own plain name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AJoinFedByAScriptSourceRunsInMemory()
    {
        var script = new PipelineComponentConfiguration { Name = "right", ComponentType = "SqlSource", DataStoreName = "Shop", BatchSize = 2 };
        var graph = Graph([Source("left", "Orders"), script, Join("join", "Inner", "c_", ["CustomerId"], ["Id"])], Lines());

        var plan = await Plan(graph);

        plan.InMemory.ShouldHaveSingleItem().Reason.ShouldContain("SqlSource");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task APlainRenameOnAnInputIsAProjectionAliasAndAnyOtherMappingKeepsTheJoinInMemory()
    {
        var renamed = Join("join", "Inner", "c_", ["Buyer"], ["Id"]);
        renamed.Fields.Add(new PipelineComponentFieldConfiguration { SettingName = "mappings", InputName = "left", Ordinal = 0, TargetField = "Buyer", SourceField = "CustomerId" });
        var plan = await Plan(Graph([Source("left", "Orders"), Source("right", "Customers"), renamed], Lines()));
        plan.Pushed.ShouldHaveSingleItem().Query.Projection.ShouldNotBeNull().Fields[0].ShouldBe(new Fdw.Data.Abstractions.ProjectionField { PropertyName = "CustomerId", Alias = "Buyer", SourceContainer = "Orders" });

        var converted = Join("join", "Inner", "c_", ["Buyer"], ["Id"]);
        converted.Fields.Add(new PipelineComponentFieldConfiguration
        {
            SettingName = "mappings", InputName = "left", Ordinal = 0, TargetField = "Buyer", SourceField = "CustomerId", TargetType = "Int64",
        });
        var refused = await Plan(Graph([Source("left", "Orders"), Source("right", "Customers"), converted], Lines()));
        refused.InMemory.ShouldHaveSingleItem().Reason.ShouldContain("not a plain rename");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALookupOnTheReferencesPrimaryKeyIsALeftJoinThatRoutesRowsWithNoReferenceRow()
    {
        var lookup = new PipelineComponentConfiguration { Name = "lookup", ComponentType = "Lookup", BatchSize = 2, UnmatchedOutput = "nomatch" };
        Columns(lookup, "keys", "CustomerId");
        Columns(lookup, "referenceKeys", "Id");
        lookup.Fields.Add(new PipelineComponentFieldConfiguration { SettingName = "bring", Ordinal = 0, TargetField = "CustomerName", SourceField = "Name" });
        var graph = Graph(
            [Source("orders", "Orders"), Source("customers", "Customers"), lookup],
            [Link("orders", "lookup", input: "input"), Link("customers", "lookup", input: "reference")]);

        var plan = await Plan(graph, new GraphEnd("lookup", "output"));

        var pushed = plan.Pushed.ShouldHaveSingleItem();
        pushed.Query.Joins.ShouldHaveSingleItem().JoinType.ShouldBe("Left");
        var absence = pushed.Absence.ShouldNotBeNull();
        absence.Output.ShouldBe("nomatch");
        absence.Dropped.ShouldBe(["CustomerName"]);
        absence.Keys.ShouldBe(["CustomerId"]);
        absence.SentLast.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALookupWhoseReferenceKeysAreNotThePrimaryKeyRunsInMemory()
    {
        var lookup = new PipelineComponentConfiguration { Name = "lookup", ComponentType = "Lookup", BatchSize = 2 };
        Columns(lookup, "keys", "Id");
        Columns(lookup, "referenceKeys", "RegionId");
        lookup.Fields.Add(new PipelineComponentFieldConfiguration { SettingName = "bring", Ordinal = 0, TargetField = "CustomerName", SourceField = "Name" });
        var graph = Graph(
            [Source("orders", "Orders"), Source("customers", "Customers"), lookup],
            [Link("orders", "lookup", input: "input"), Link("customers", "lookup", input: "reference")]);

        var plan = await Plan(graph, new GraphEnd("lookup", "output"));

        plan.InMemory.ShouldHaveSingleItem().Reason.ShouldContain("primary key");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AUnionOfContainersOfOneStoreIsOneUnionAllOrderedByTheirKeys()
    {
        _containers["Archive"] = Container(Shop, ["Id"], ("Id", typeof(int)), ("CustomerId", typeof(int)), ("Total", typeof(decimal)));
        var union = new PipelineComponentConfiguration { Name = "union", ComponentType = "UnionAll" };
        var graph = Graph([Source("a", "Orders"), Source("b", "Archive"), union], [Link("a", "union", input: "a"), Link("b", "union", input: "b")]);

        var plan = await Plan(graph, new GraphEnd("union", "output"));

        var pushed = plan.Pushed.ShouldHaveSingleItem();
        pushed.Target.Container.ShouldBe("Orders");
        pushed.Query.Joins.ShouldBeEmpty();
        var branch = pushed.Query.Unions.ShouldHaveSingleItem();
        branch.ContainerName.ShouldBe("Archive");
        branch.Fields.Select(field => field.Alias).ShouldBe(["Id", "CustomerId", "Total"]);
        pushed.Query.Ordering.ShouldNotBeNull().OrderedFields.Select(field => field.PropertyName).ShouldBe(["Id"]);
        pushed.BatchSize.ShouldBe(2);
        pushed.Members.ShouldBe(["a", "b", "union"], ignoreOrder: true);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AUnionWhoseInputsGiveDifferentFieldsRunsInMemory()
    {
        var union = new PipelineComponentConfiguration { Name = "union", ComponentType = "UnionAll" };
        var graph = Graph([Source("a", "Orders"), Source("b", "Customers"), union], [Link("a", "union", input: "a"), Link("b", "union", input: "b")]);

        var plan = await Plan(graph, new GraphEnd("union", "output"));

        plan.InMemory.ShouldHaveSingleItem().Reason.ShouldContain("same fields in the same order");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AGatewayThatDoesNotDescribeStoresPushesNothing()
    {
        var gateway = new Mock<IDataGateway>(MockBehavior.Strict);

        var plan = await GraphPushdownPlanner.Plan(
            Joined("Inner", "Orders", "Customers", ["CustomerId"], ["Id"]), gateway.Object, null, NullLogger.Instance, TestContext.Current.CancellationToken);

        plan.ShouldBeSameAs(GraphPushdownPlan.None);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task AContainerTheGatewayCannotDescribeIsReadInMemoryWithTheGatewaysReason()
    {
        _containers.Remove("Customers");

        var plan = await Plan(Joined("Inner", "Orders", "Customers", ["CustomerId"], ["Id"]));

        plan.InMemory.ShouldHaveSingleItem().Reason.ShouldContain("no such container");
    }

    private static PushdownContainer Container(Guid store, string[] key, params (string Name, Type Type)[] fields) =>
        new(store, true, true, [.. fields.Select(field => new PushdownField(field.Name, field.Type))], key);

    private static PipelineComponentConfiguration Source(string name, string container) =>
        new() { Name = name, ComponentType = "ContainerSource", DataStoreName = "Shop", PathName = "dbo", ContainerName = container, BatchSize = 2 };

    private static PipelineComponentConfiguration Join(string name, string kind, string prefix, string[] leftKeys, string[] rightKeys)
    {
        var join = new PipelineComponentConfiguration { Name = name, ComponentType = "MergeJoin", JoinKind = kind, RightPrefix = prefix, BatchSize = 2 };
        Columns(join, "leftKeys", leftKeys);
        Columns(join, "rightKeys", rightKeys);
        return join;
    }

    private static void Columns(PipelineComponentConfiguration component, string setting, params string[] columns)
    {
        for (var index = 0; index < columns.Length; index++)
        {
            component.Columns.Add(new PipelineComponentColumnConfiguration { SettingName = setting, Ordinal = index, ColumnName = columns[index] });
        }
    }

    private static PipelineLinkConfiguration[] Lines() =>
        [Link("left", "join", input: "left"), Link("right", "join", input: "right")];

    private static GraphPipelineConfiguration Joined(string kind, string left, string right, string[] leftKeys, string[] rightKeys) =>
        Graph([Source("left", left), Source("right", right), Join("join", kind, "c_", leftKeys, rightKeys)], Lines());

    private Task<GraphPushdownPlan> Plan(GraphPipelineConfiguration graph) => Plan(graph, new GraphEnd("join", "output"));

    private Task<GraphPushdownPlan> Plan(GraphPipelineConfiguration graph, GraphEnd end) =>
        GraphPushdownPlanner.Plan(graph, Gateway().Object, end, NullLogger.Instance, TestContext.Current.CancellationToken);

    // A gateway that describes the containers this test states and nothing else; a container it does not hold is its own refusal.
    private Mock<IDataGateway> Gateway()
    {
        var gateway = new Mock<IDataGateway>(MockBehavior.Strict);
        gateway.As<IPushdownPlanningGateway>()
            .Setup(g => g.Describe(It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataStoreTarget target, CancellationToken _) => _containers.TryGetValue(target.Container, out var container)
                ? GenericResult<PushdownContainer>.Success(container)
                : GenericResult<PushdownContainer>.Failure(new GenericMessage($"no such container '{target.Container}'")));
        return gateway;
    }
}
