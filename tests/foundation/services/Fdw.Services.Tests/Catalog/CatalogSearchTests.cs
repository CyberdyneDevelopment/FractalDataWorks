using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Catalog;

/// <summary>
/// FDW-1681: one search over every named thing of the catalog. The providers are doubles, so what is tested is the matching, the
/// closeness order, the paging, the kinds that are read and the failure of a read.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class CatalogSearchTests
{
    private static IDataSetImplementationConfiguration DataSet(string name, string description)
    {
        var set = new Mock<IDataSetImplementationConfiguration>();
        set.SetupGet(s => s.Name).Returns(name);
        set.SetupGet(s => s.Description).Returns(description);
        set.SetupGet(s => s.DisplayName).Returns((string?)null);
        return set.Object;
    }

    private static Mock<IDataSetConfigurationProvider> DataSets(params IDataSetImplementationConfiguration[] items)
    {
        var provider = new Mock<IDataSetConfigurationProvider>();
        provider.Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>.Success(items));
        return provider;
    }

    private static Mock<IDataStoreProvider> Stores(params IDataStore[] items)
    {
        var provider = new Mock<IDataStoreProvider>();
        provider.Setup(p => p.Get(It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<IReadOnlyList<IDataStore>>.Success(items));
        return provider;
    }

    private static Mock<IPipelineConfigurationProvider> Pipelines(params (string Name, string? Description)[] items)
    {
        var provider = new Mock<IPipelineConfigurationProvider>();
        var pipelines = items.Select(item =>
        {
            var pipeline = new Mock<IPipelineImplementationConfiguration>();
            pipeline.SetupGet(p => p.Name).Returns(item.Name);
            pipeline.SetupGet(p => p.Description).Returns(item.Description);
            pipeline.SetupGet(p => p.ModifyDate).Returns(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));
            return pipeline.Object;
        }).ToList();
        provider.Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success(pipelines));
        return provider;
    }

    private static Mock<ICalculationConfigurationProvider> Calculations(params (string Name, string? Description)[] items)
    {
        var provider = new Mock<ICalculationConfigurationProvider>();
        var calculations = items.Select(item =>
        {
            var calculation = new Mock<ICalculationEntityImplementationConfiguration>();
            calculation.SetupGet(c => c.Name).Returns(item.Name);
            calculation.SetupGet(c => c.Description).Returns(item.Description);
            return calculation.Object;
        }).ToList();
        provider.Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ICalculationEntityImplementationConfiguration>>.Success(calculations));
        return provider;
    }

    private static Mock<ITransformationConfigurationProvider> Transformations()
    {
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider.Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ITransformationImplementationConfiguration>>.Success([]));
        return provider;
    }

    private static CatalogSearch Search(
        Mock<IDataSetConfigurationProvider>? dataSets = null,
        Mock<IDataStoreProvider>? stores = null,
        Mock<IPipelineConfigurationProvider>? pipelines = null,
        Mock<ICalculationConfigurationProvider>? calculations = null,
        InMemoryTerms? terms = null,
        params IRankedSearch[] fields)
        => new(
            (dataSets ?? DataSets()).Object,
            (stores ?? Stores()).Object,
            (pipelines ?? Pipelines()).Object,
            (calculations ?? Calculations()).Object,
            Transformations().Object,
            [.. fields, new GlossaryTermSearch((terms ?? new InMemoryTerms([])).Provider.Object, NullLogger.Instance)],
            NullLogger.Instance);

    private static CatalogKindSet Kinds(params string[] names) => CatalogKindSet.Of(names).Kinds!;

    [Fact]
    public void AKindThatIsNotACatalogKindIsNamed()
    {
        var (kinds, unknown) = CatalogKindSet.Of(["DataSet", "Widget"]);

        kinds.ShouldBeNull();
        unknown.ShouldBe("Widget");
    }

    [Fact]
    public void KindsAreReadWithoutRegardToCaseAndNoneNamedMeansEveryKind()
    {
        CatalogKindSet.Of(["dataset"]).Kinds!.Names.ShouldBe(["DataSet"]);
        CatalogKindSet.Of([]).Kinds!.Names.ShouldBe(CatalogKinds.All);
    }

    [Fact]
    public async Task ClosestNamesComeFirstThenNamesThatOnlyTheDescriptionHolds()
    {
        var dataSets = DataSets(
            DataSet("Reorders", "Orders sent again"),
            DataSet("Orders", "Every order"),
            DataSet("Customers", "Who buys; see orders"),
            DataSet("Orders2", "Second orders table"),
            DataSet("Suppliers", "Who sells to us"));

        var found = await Search(dataSets).Run(Kinds("DataSet"), "orders", 0, 10, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value!.Items.Select(entry => entry.Name).ShouldBe(["Orders", "Orders2", "Reorders", "Customers"]);
        found.Value.Total.ShouldBe(4);
        found.Value.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task ABrowseListsEveryKindInOrderAndThenByName()
    {
        var search = Search(
            dataSets: DataSets(DataSet("B", "b"), DataSet("A", "a")),
            pipelines: Pipelines(("P", "p")),
            calculations: Calculations(("C", "c")),
            terms: new InMemoryTerms([InMemoryTerms.Term("T", "a term")]),
            fields: [new FakeRankedSearch("DataSetField", 1000, FakeRankedSearch.Hit("F", 1, 0)).Under(1, "A"), new FakeRankedSearch("ContainerField")]);

        var found = await search.Run(Kinds(), string.Empty, 0, 50, TestContext.Current.CancellationToken);

        found.Value!.Items.Select(entry => $"{entry.EntityType}:{entry.Name}").ShouldBe(
            ["DataSet:A", "DataSet:B", "DataSetField:F", "Calculation:C", "Pipeline:P", "GlossaryTerm:T"]);
    }

    [Fact]
    public async Task APageSaysHowManyMatchedAndWhetherMoreFollow()
    {
        var dataSets = DataSets(DataSet("a1", ""), DataSet("a2", ""), DataSet("a3", ""));

        var first = (await Search(dataSets).Run(Kinds("DataSet"), "a", 0, 2, TestContext.Current.CancellationToken)).Value!;
        var second = (await Search(dataSets).Run(Kinds("DataSet"), "a", 2, 2, TestContext.Current.CancellationToken)).Value!;

        first.Items.Select(entry => entry.Name).ShouldBe(["a1", "a2"]);
        first.HasMore.ShouldBeTrue();
        first.Total.ShouldBe(3);
        second.Items.Select(entry => entry.Name).ShouldBe(["a3"]);
        second.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task ADataSetFieldSaysWhichDataSetItIsIn()
    {
        var fields = new FakeRankedSearch("DataSetField", 1000, FakeRankedSearch.Hit("Amount", 7, 0, "Amount of Orders")).Under(7, "Orders");

        var found = await Search(fields: fields).Run(Kinds("DataSetField"), "amount", 0, 10, TestContext.Current.CancellationToken);

        var entry = found.Value!.Items.ShouldHaveSingleItem();
        entry.EntityType.ShouldBe("DataSetField");
        entry.Name.ShouldBe("Amount");
        entry.Location.ShouldBe("Orders");
        entry.Parents.ShouldBe(["Orders"]);
        entry.Description.ShouldBe("Amount of Orders");
    }

    [Fact]
    public async Task AContainerFieldIsPlacedUnderItsStorePathAndContainer()
    {
        var fields = new FakeRankedSearch("ContainerField", 1000, FakeRankedSearch.Hit("Revenue", 9, 0)).Under(9, "SalesDb", "dbo", "Orders");

        var found = await Search(fields: fields).Run(Kinds("ContainerField"), "revenue", 0, 10, TestContext.Current.CancellationToken);

        var entry = found.Value!.Items.ShouldHaveSingleItem();
        entry.Location.ShouldBe("SalesDb / dbo / Orders");
        entry.Parents.ShouldBe(["SalesDb", "dbo", "Orders"]);
    }

    [Fact]
    public async Task DataSetsAreReadWithoutTheirFieldsAndAFieldSearchIsOnlyAskedWhenFieldsAreWanted()
    {
        var dataSets = DataSets(DataSet("Orders", "Every order"));
        var fields = new FakeRankedSearch("DataSetField", 1000, FakeRankedSearch.Hit("Amount", 7, 0)).Under(7, "Orders");

        await Search(dataSets, fields: fields).Run(Kinds("DataSet"), "orders", 0, 10, TestContext.Current.CancellationToken);
        fields.Windows.ShouldBeEmpty();

        await Search(dataSets, fields: fields).Run(Kinds("DataSetField"), "amount", 0, 10, TestContext.Current.CancellationToken);

        fields.Windows.ShouldBe([10]);
        dataSets.Verify(p => p.Get(ConfigurationLevels.RecordOnly, It.IsAny<CancellationToken>()), Times.Once());
        dataSets.Verify(p => p.Get(ConfigurationLevels.Children, It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task EachSourceIsAskedForSkipPlusTakeAndNoMore()
    {
        var fields = new FakeRankedSearch("DataSetField", 1000, Enumerable.Range(1, 500).Select(i => FakeRankedSearch.Hit($"field{i:000}", i, 1)).ToArray());
        var containerFields = new FakeRankedSearch("ContainerField");

        await Search(fields: [fields, containerFields]).Run(Kinds("DataSetField", "ContainerField"), "field", 40, 25, TestContext.Current.CancellationToken);

        fields.Windows.ShouldBe([65]);
        containerFields.Windows.ShouldBe([65]);
    }

    [Fact]
    public async Task OnlyTheHitsOfThePageAreAskedWhereTheySit()
    {
        var hits = Enumerable.Range(1, 500).Select(i => FakeRankedSearch.Hit($"field{i:000}", i, 1)).ToArray();
        var fields = new FakeRankedSearch("DataSetField", 1000, hits);
        foreach (var hit in hits)
        {
            fields.Under(hit.Key, "Orders");
        }

        var found = await Search(fields: fields).Run(Kinds("DataSetField"), "field", 10, 5, TestContext.Current.CancellationToken);

        found.Value!.Items.Select(entry => entry.Name).ShouldBe(["field011", "field012", "field013", "field014", "field015"]);
        fields.Placed.ShouldBe(5);
    }

    [Fact]
    public async Task MatchesOfTheKindsAreMergedByClosenessThenKindThenName()
    {
        var search = Search(
            dataSets: DataSets(DataSet("Amount", "the amount"), DataSet("Totals", "the amount of it")),
            calculations: Calculations(("AmountDue", null)),
            terms: new InMemoryTerms([InMemoryTerms.Term("Amount", "How much")]),
            fields: [new FakeRankedSearch("DataSetField", 1000, FakeRankedSearch.Hit("Amount", 1, 0), FakeRankedSearch.Hit("Net", 2, 3, "the amount")).Under(1, "Orders").Under(2, "Orders"), new FakeRankedSearch("ContainerField")]);

        var found = await search.Run(Kinds(), "amount", 0, 10, TestContext.Current.CancellationToken);

        found.Value!.Items.Select(entry => $"{entry.EntityType}:{entry.Name}").ShouldBe(
            ["DataSet:Amount", "DataSetField:Amount", "GlossaryTerm:Amount", "Calculation:AmountDue", "DataSet:Totals", "DataSetField:Net"]);
        found.Value.Total.ShouldBe(6);
    }

    [Fact]
    public async Task TheTotalAddsWhatEverySourceCountsNotWhatItReturned()
    {
        var hits = Enumerable.Range(1, 300).Select(i => FakeRankedSearch.Hit($"field{i:000}", i, 1)).ToArray();
        var fields = new FakeRankedSearch("DataSetField", 1000, hits);
        foreach (var hit in hits)
        {
            fields.Under(hit.Key, "Orders");
        }

        var terms = new InMemoryTerms(Enumerable.Range(1, 50).Select(i => InMemoryTerms.Term($"field term {i:00}", "d")));
        var search = Search(dataSets: DataSets(DataSet("field set", "d")), terms: terms, fields: fields);

        var found = await search.Run(Kinds("DataSet", "DataSetField", "GlossaryTerm"), "field", 0, 25, TestContext.Current.CancellationToken);

        found.Value!.Total.ShouldBe(1 + 300 + 50);
        found.Value.HasMore.ShouldBeTrue();
        found.Value.Items.Count.ShouldBe(25);
    }

    [Fact]
    public async Task AKindWithNoSourceFailsTheSearchRatherThanLeavingTheKindOut()
    {
        var search = Search();

        var found = await search.Run(Kinds("ContainerField"), "x", 0, 10, TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task ASourceThatReadsLessThanThePageNeedsFailsTheSearch()
    {
        var narrow = new FakeRankedSearch("DataSetField", 50);

        var found = await Search(fields: narrow).Run(Kinds("DataSetField"), "x", 40, 25, TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void TwoSourcesOfOneKindOrASourceOfNoKindAreRefusedWhenTheSearchIsMade()
    {
        Should.Throw<ArgumentException>(() => { Search(fields: [new FakeRankedSearch("DataSetField"), new FakeRankedSearch("DataSetField")]); });
        Should.Throw<ArgumentException>(() => { Search(fields: new FakeRankedSearch("Pipeline")); });
    }

    [Fact]
    public async Task AFieldWhoseParentCannotBeReadIsLeftOutOfThePage()
    {
        var fields = new FakeRankedSearch("DataSetField", 1000, FakeRankedSearch.Hit("seen", 1, 1), FakeRankedSearch.Hit("hidden", 2, 1)).Under(1, "Orders");

        var found = await Search(fields: fields).Run(Kinds("DataSetField"), string.Empty, 0, 10, TestContext.Current.CancellationToken);

        found.Value!.Items.Select(entry => entry.Name).ShouldBe(["seen"]);
    }

    [Fact]
    public async Task AProviderOfAKindThatWasNotAskedForIsNotRead()
    {
        var pipelines = Pipelines(("Load", "Loads orders"));
        var dataSets = DataSets(DataSet("Orders", "Every order"));

        await Search(dataSets, pipelines: pipelines).Run(Kinds("Pipeline"), "orders", 0, 10, TestContext.Current.CancellationToken);

        dataSets.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AStoreIsWalkedToItsPathsAndContainers()
    {
        var container = new Mock<IDataContainer>();
        container.SetupGet(c => c.Name).Returns("Orders");
        container.As<IDataNode>().SetupGet(c => c.Name).Returns("Orders");
        container.As<IDataNode>().SetupGet(c => c.Description).Returns("Every order");
        var path = new Mock<IDataNodePath>();
        path.SetupGet(p => p.Name).Returns("dbo");
        path.SetupGet(p => p.Description).Returns((string?)null);
        path.SetupGet(p => p.Containers).Returns([container.Object]);
        var store = new Mock<IDataStore>();
        store.SetupGet(s => s.Name).Returns("SalesDb");
        store.SetupGet(s => s.Description).Returns("The sales database");
        store.SetupGet(s => s.Paths).Returns([path.Object]);

        var found = await Search(stores: Stores(store.Object))
            .Run(Kinds("DataStore", "DataPath", "Container"), string.Empty, 0, 50, TestContext.Current.CancellationToken);

        found.Value!.Items.Select(entry => $"{entry.EntityType}:{entry.Name}:{entry.Location}").ShouldBe(
            ["DataStore:SalesDb:", "DataPath:dbo:SalesDb", "Container:Orders:SalesDb / dbo"]);
        found.Value!.Items.Select(entry => string.Join("|", entry.Parents)).ShouldBe(["", "SalesDb", "SalesDb|dbo"]);
    }

    [Fact]
    public async Task ACalculationAndATermAreFoundByDescription()
    {
        var search = Search(
            calculations: Calculations(("GrossMargin", "Revenue less cost of sales")),
            terms: new InMemoryTerms([InMemoryTerms.Term("Turnover", "Revenue over a period")]));

        var found = await search.Run(Kinds("Calculation", "GlossaryTerm"), "revenue", 0, 10, TestContext.Current.CancellationToken);

        found.Value!.Items.Select(entry => $"{entry.EntityType}:{entry.Name}").ShouldBe(["Calculation:GrossMargin", "GlossaryTerm:Turnover"]);
        found.Value.Items[1].Owner.ShouldBe("Finance");
    }

    [Fact]
    public async Task AReadThatFailsFailsTheSearchRatherThanLeavingTheKindOut()
    {
        var pipelines = new Mock<IPipelineConfigurationProvider>();
        pipelines.Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Failure(new GenericMessage("the store did not answer")));

        var found = await Search(pipelines: pipelines).Run(Kinds("Pipeline"), "load", 0, 10, TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
    }
}
