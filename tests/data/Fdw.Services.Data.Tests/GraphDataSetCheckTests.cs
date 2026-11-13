using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>FDW-5290: the check before save of a Graph data set lists every problem with its own code and path, reports intended sources as not bound yet, and never blocks on them.</summary>
public sealed class GraphDataSetCheckTests
{
    private static GraphDataSetConfiguration Bound(params (string Field, string Component, string Output)[] fields)
    {
        var graph = new GraphDataSetConfiguration { Name = "Revenue" };
        foreach (var (field, component, output) in fields)
        {
            graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Name = field, FedByComponent = component, FedByField = output });
        }

        return graph;
    }

    private static GraphDataSetComponentConfiguration Component(string name, string kind, string? dataSet = null, string? dataStore = null) =>
        new() { Name = name, ComponentType = kind, DataSetName = dataSet, DataStoreName = dataStore };

    private static async Task<GraphDataSetCheckResult> Check(GraphDataSetConfiguration draft, FakeGraphDataSetSources sources) =>
        (await GraphDataSetCheck.Check(draft, sources, NullLogger.Instance, TestContext.Current.CancellationToken)).Value!;

    private static string CodeOf(string name) => DataServiceResultCodes.ByName(name).Code;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task ASoundBoundDraftHasNoProblem()
    {
        var draft = Bound(("Amount", "Calc", "Amount"));
        draft.Components.Add(Component("Orders", "DataSetSource", dataSet: "Orders"));
        draft.Components.Add(Component("Calc", "Calculate"));

        var found = await Check(draft, new FakeGraphDataSetSources().WithDataSet("Orders"));

        found.IsValid.ShouldBeTrue();
        found.NotBound.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task AnAbstractDraftWithIntendedSourcesIsValidAndReportsThemAsNotBound()
    {
        var draft = new GraphDataSetConfiguration { Name = "CustomerSegments" };
        draft.GraphFields.Add(new GraphDataSetFieldConfiguration { Name = "Segment" });
        draft.Intents.Add(new GraphDataSetIntentConfiguration { IntentKind = "DataStore", DataStoreName = "Weather feed" });
        draft.Intents.Add(new GraphDataSetIntentConfiguration { IntentKind = "DataSet", SourceDataSetName = "RawCustomers" });

        var found = await Check(draft, new FakeGraphDataSetSources().WithAbstractDataSet("RawCustomers"));

        found.IsValid.ShouldBeTrue();
        found.NotBound.Select(source => (string.Join('/', source.Path), source.Name, source.Exists)).ShouldBe(
            new[] { ("intendedSources/0", "Weather feed", false), ("intendedSources/1", "RawCustomers", true) });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task AnIntendedDataSetThatDoesNotExistIsAProblemAndAnIntendedStoreNeverIs()
    {
        var draft = new GraphDataSetConfiguration { Name = "Downstream" };
        draft.Intents.Add(new GraphDataSetIntentConfiguration { IntentKind = "DataStore", DataStoreName = "External feed" });
        draft.Intents.Add(new GraphDataSetIntentConfiguration { IntentKind = "DataSet", SourceDataSetName = "NotThereYet" });

        var found = await Check(draft, new FakeGraphDataSetSources());

        var problem = found.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetIntentSourceMissing"));
        string.Join('/', problem.Path).ShouldBe("intendedSources/1");
        found.NotBound.ShouldHaveSingleItem().Name.ShouldBe("External feed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task TheCheckStatesTheIdOfAnIntendedDataSetAsSavingDoes()
    {
        var draft = new GraphDataSetConfiguration { Name = "Downstream" };
        var intent = new GraphDataSetIntentConfiguration { IntentKind = "DataSet", SourceDataSetName = "Upstream" };
        draft.Intents.Add(intent);
        var sources = new FakeGraphDataSetSources().WithAbstractDataSet("Upstream");

        await Check(draft, sources);

        intent.SourceDataSetId.ShouldBe(sources.DataSetFacts["Upstream"].Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task IntendedSourcesOnAGraphThatHasComponentsAreRefusedAsSavingRefusesThem()
    {
        var draft = Bound(("Amount", "Calc", "Amount"));
        draft.Components.Add(Component("Calc", "Calculate"));
        draft.Intents.Add(new GraphDataSetIntentConfiguration { IntentKind = "DataStore", DataStoreName = "Feed" });

        var found = await Check(draft, new FakeGraphDataSetSources());

        var problem = found.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetIntentOnBoundGraph"));
        string.Join('/', problem.Path).ShouldBe("intendedSources");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task HalfAFeedsFromIsReportedWithItsOwnCodeAndNotAsUnfed()
    {
        var draft = Bound(("Amount", "Calc", "Amount"), ("Region", "Calc", string.Empty));
        draft.Components.Add(Component("Calc", "Calculate"));

        var found = await Check(draft, new FakeGraphDataSetSources());

        var problem = found.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetFieldFedByIncomplete"));
        string.Join('/', problem.Path).ShouldBe("fieldsOut/Region");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task AnAbstractDraftThatIntendsToReadAnAbstractDataSetIsNotBlocked()
    {
        var draft = new GraphDataSetConfiguration { Name = "Downstream" };
        draft.Intents.Add(new GraphDataSetIntentConfiguration { IntentKind = "DataSet", SourceDataSetName = "Upstream" });

        var found = await Check(draft, new FakeGraphDataSetSources().WithAbstractDataSet("Upstream"));

        found.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task ABoundDraftThatReadsAnAbstractDataSetIsRefusedAtTheComponent()
    {
        var draft = Bound(("Segment", "Segments", "Segment"));
        draft.Components.Add(Component("Segments", "DataSetSource", dataSet: "CustomerSegments"));

        var found = await Check(draft, new FakeGraphDataSetSources().WithAbstractDataSet("CustomerSegments"));

        var problem = found.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetSourceNotBound"));
        problem.Path.ShouldBe(new[] { "components", "Segments" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task AFieldOutNothingFeedsIsReportedWithItsPath()
    {
        var draft = Bound(("Amount", "Calc", "Amount"), ("Region", string.Empty, string.Empty));
        draft.Components.Add(Component("Calc", "Calculate"));

        var found = await Check(draft, new FakeGraphDataSetSources());

        var problem = found.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetFieldOutUnfed"));
        problem.Path.ShouldBe(new[] { "fieldsOut", "Region" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task AFieldOutFedByAComponentThatIsNotInTheGraphIsReported()
    {
        var draft = Bound(("Amount", "Missing", "Amount"));
        draft.Components.Add(Component("Calc", "Calculate"));

        var found = await Check(draft, new FakeGraphDataSetSources());

        var problem = found.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetFieldOutFeedMissing"));
        problem.Path.ShouldBe(new[] { "fieldsOut", "Amount" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task AbstractFieldsOutAreNeverReportedAsUnfed()
    {
        var draft = new GraphDataSetConfiguration { Name = "Abstract" };
        draft.GraphFields.Add(new GraphDataSetFieldConfiguration { Name = "Amount" });

        (await Check(draft, new FakeGraphDataSetSources())).IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task EveryRefusedComponentIsReportedWithItsFamilysCodeAndPath()
    {
        var draft = Bound(("Amount", "Calc", "Amount"));
        draft.Components.Add(Component("Calc", "Calculate"));
        draft.Components.Add(Component("Out", "DataSetDestination"));
        draft.Components.Add(Component("Mail", "Notify"));
        draft.Components.Add(Component("Again", "ForEach"));

        var found = await Check(draft, new FakeGraphDataSetSources());

        found.Problems.Select(problem => (problem.Code, string.Join('/', problem.Path))).ShouldBe(
        new[]
        {
            (CodeOf("GraphDataSetDestinationRefused"), "components/Out"),
            (CodeOf("GraphDataSetActionRefused"), "components/Mail"),
            (CodeOf("GraphDataSetLoopRefused"), "components/Again"),
        });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task ASourceThatDoesNotExistIsReportedForADataSetAndForADataStore()
    {
        var draft = Bound(("Amount", "Orders", "Amount"));
        draft.Components.Add(Component("Orders", "DataSetSource", dataSet: "NoSuchSet"));
        draft.Components.Add(Component("Raw", "ContainerSource", dataStore: "NoSuchStore"));
        draft.Components.Add(Component("Known", "ContainerSource", dataStore: "Crm"));
        var sources = new FakeGraphDataSetSources();
        sources.Stores.Add("Crm");

        var found = await Check(draft, sources);

        found.Problems.Select(problem => (problem.Code, string.Join('/', problem.Path))).ShouldBe(
        new[]
        {
            (CodeOf("GraphDataSetSourceMissing"), "components/Orders"),
            (CodeOf("GraphDataSetSourceMissing"), "components/Raw"),
        });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task DuplicateFieldsOutAndComponentsAreReportedOnceEach()
    {
        var draft = Bound(("Amount", "Calc", "Amount"), ("amount", "Calc", "Amount"), ("AMOUNT", "Calc", "Amount"));
        draft.Components.Add(Component("Calc", "Calculate"));
        draft.Components.Add(Component("calc", "Filter"));

        var found = await Check(draft, new FakeGraphDataSetSources());

        found.Problems.Select(problem => (problem.Code, string.Join('/', problem.Path))).ShouldBe(
        new[]
        {
            (CodeOf("GraphDataSetFieldOutDuplicate"), "fieldsOut/amount"),
            (CodeOf("GraphDataSetComponentDuplicate"), "components/calc"),
        });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task ACycleThroughDataSetsIsNamedAtTheComponentThatStartsIt()
    {
        var draft = Bound(("Amount", "Calc", "Amount"));
        draft.Components.Add(Component("Calc", "Calculate"));
        draft.Components.Add(Component("Orders", "DataSetSource", dataSet: "Orders"));

        var found = await Check(draft, new FakeGraphDataSetSources().WithDataSet("Orders", "Totals").WithDataSet("Totals", "Revenue"));

        var problem = found.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetCycle"));
        problem.Path.ShouldBe(new[] { "components", "Orders" });
        problem.Message.ShouldContain("Revenue -> Orders -> Totals -> Revenue");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task ADraftThatReadsItselfIsACycleAndNotAMissingSource()
    {
        var draft = Bound(("Amount", "Self", "Amount"));
        draft.Components.Add(Component("Self", "DataSetSource", dataSet: "Revenue"));

        var found = await Check(draft, new FakeGraphDataSetSources());

        found.Problems.ShouldHaveSingleItem().Code.ShouldBe(CodeOf("GraphDataSetCycle"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task EveryProblemIsListedInOneCallAndEachDataSetIsReadOnce()
    {
        var draft = Bound(("Amount", "Calc", "Amount"), ("Region", string.Empty, string.Empty));
        draft.Components.Add(Component("Calc", "Calculate"));
        draft.Components.Add(Component("A", "DataSetSource", dataSet: "Orders"));
        draft.Components.Add(Component("B", "DataSetSource", dataSet: "orders"));
        draft.Components.Add(Component("C", "DataSetSource", dataSet: "NoSuchSet"));
        draft.Components.Add(Component("Out", "ContainerDestination"));
        var sources = new FakeGraphDataSetSources().WithDataSet("Orders");

        var found = await Check(draft, sources);

        found.Problems.Count.ShouldBe(3);
        sources.DataSetsAsked.Count(name => string.Equals(name, "Orders", StringComparison.OrdinalIgnoreCase)).ShouldBe(1);
    }

    private static GraphDataSetComponentConfiguration Container(string name, string dataStore, string container, string? path = null) =>
        new() { Name = name, ComponentType = "ContainerSource", DataStoreName = dataStore, ContainerName = container, PathName = path };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task AContainerTheDataStoreDoesNotHoldIsReportedAtItsComponent()
    {
        var draft = Bound(("Amount", "Raw", "Amount"));
        draft.Components.Add(Container("Raw", "Crm", "Customers", "dbo"));

        var found = await Check(draft, new FakeGraphDataSetSources().WithContainer("Crm", "Orders", "dbo"));

        var problem = found.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetContainerNotFound"));
        string.Join('/', problem.Path).ShouldBe("components/Raw");
        problem.Message.ShouldContain("Customers");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task AContainerInAnotherPathThanTheComponentStatesHasItsOwnCode()
    {
        var draft = Bound(("Amount", "Raw", "Amount"));
        draft.Components.Add(Container("Raw", "Crm", "Customers", "dbo"));

        var found = await Check(draft, new FakeGraphDataSetSources().WithContainer("Crm", "Customers", "sales"));

        var problem = found.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetContainerInOtherPath"));
        problem.Message.ShouldContain("sales");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task ANameHeldInTwoPathsWithNoPathStatedIsReportedAsAmbiguousAndNotPickedFromReadOrder()
    {
        var draft = Bound(("Amount", "Raw", "Amount"));
        draft.Components.Add(Container("Raw", "Crm", "Customers"));

        var found = await Check(draft, new FakeGraphDataSetSources().WithContainer("Crm", "Customers", "dbo").WithContainer("Crm", "Customers", "archive"));

        var problem = found.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetContainerAmbiguous"));
        problem.Message.ShouldContain("dbo");
        problem.Message.ShouldContain("archive");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task ANameHeldInTwoPathsIsNotAmbiguousWhenTheComponentStatesOneOfThem()
    {
        var draft = Bound(("Amount", "Raw", "Amount"));
        draft.Components.Add(Container("Raw", "Crm", "Customers", "archive"));

        var found = await Check(draft, new FakeGraphDataSetSources().WithContainer("Crm", "Customers", "dbo").WithContainer("Crm", "Customers", "archive"));

        found.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task ANameHeldInTwoPathsWithAStatedPathOfNeitherIsInAnotherPathAndListsBoth()
    {
        var draft = Bound(("Amount", "Raw", "Amount"));
        draft.Components.Add(Container("Raw", "Crm", "Customers", "sales"));

        var found = await Check(draft, new FakeGraphDataSetSources().WithContainer("Crm", "Customers", "dbo").WithContainer("Crm", "Customers", "archive"));

        var problem = found.Problems.ShouldHaveSingleItem();
        problem.Code.ShouldBe(CodeOf("GraphDataSetContainerInOtherPath"));
        problem.Message.ShouldContain("dbo");
        problem.Message.ShouldContain("archive");
    }

    [Theory]
    [InlineData("dbo")]
    [InlineData("DBO")]
    [InlineData(null)]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task AContainerTheDataStoreHoldsInTheStatedPathOrWithNoPathStatedIsNotReported(string? path)
    {
        var draft = Bound(("Amount", "Raw", "Amount"));
        draft.Components.Add(Container("Raw", "Crm", "customers", path));

        var found = await Check(draft, new FakeGraphDataSetSources().WithContainer("Crm", "Customers", "dbo"));

        found.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task TheContainersOfOneDataStoreAreReadOnceWhateverTheNumberOfComponents()
    {
        var draft = Bound(("Amount", "A", "Amount"));
        draft.Components.Add(Container("A", "Crm", "Customers"));
        draft.Components.Add(Container("B", "crm", "Orders"));
        draft.Components.Add(Container("C", "Billing", "Invoices"));
        var sources = new FakeGraphDataSetSources().WithContainer("Crm", "Customers", "dbo").WithContainer("Crm", "Orders", "dbo").WithContainer("Billing", "Invoices", "dbo");

        var found = await Check(draft, sources);

        found.IsValid.ShouldBeTrue();
        sources.ContainersAsked.Count.ShouldBe(2);
        sources.ContainersAsked.Single(asked => string.Equals(asked.DataStore, "Crm", StringComparison.OrdinalIgnoreCase)).Names.Length.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task AContainerOfADataStoreThatDoesNotExistIsLeftToTheMissingDataStoreProblem()
    {
        var draft = Bound(("Amount", "Raw", "Amount"));
        draft.Components.Add(Container("Raw", "NoSuchStore", "Customers"));
        var sources = new FakeGraphDataSetSources();

        var found = await Check(draft, sources);

        found.Problems.ShouldHaveSingleItem().Code.ShouldBe(CodeOf("GraphDataSetSourceMissing"));
        sources.ContainersAsked.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5408")]
    public async Task AFailedReadOfTheContainersIsTheFailureOfTheCheckNotAProblem()
    {
        var draft = Bound(("Amount", "Raw", "Amount"));
        draft.Components.Add(Container("Raw", "Crm", "Customers"));

        var result = await GraphDataSetCheck.Check(
            draft, new FakeGraphDataSetSources { ContainersFail = true }.WithContainer("Crm", "Customers", "dbo"), NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5290")]
    public async Task AFailedReadIsTheFailureOfTheCheckNotAProblem()
    {
        var draft = Bound(("Amount", "Orders", "Amount"));
        draft.Components.Add(Component("Orders", "DataSetSource", dataSet: "Orders"));

        var result = await GraphDataSetCheck.Check(draft, new FakeGraphDataSetSources { Fails = true }, NullLogger.Instance, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }
}
