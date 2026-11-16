using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Abstractions.ColumnValues;
using Fdw.Services.Data.ColumnValues;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.ColumnValues;

/// <summary>
/// FDW-5666: a column's distinct values are searched by the store in one grouped query (a row count per value, a literal
/// contains match, value order, skip and take + 1), never read whole; each refusal answers its own code.
/// </summary>
public sealed class ContainerColumnValuesTests
{
    private const string Store = "Places";
    private const string Path = "dbo";
    private const string Container = "Visits";
    private const string Column = "City";

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TheStoreGroupsCountsFiltersOrdersAndPagesInOneQuery()
    {
        var fixture = new Fixture([]);

        var result = await fixture.Service.Search(Query(search: "ber", skip: 10, take: 12), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        fixture.Sent.Count.ShouldBe(1);
        fixture.Targets.Single().ShouldBe(new DataStoreTarget(Store, Path, Container));
        var command = fixture.Sent.Single().ShouldBeOfType<QueryCommand<Dictionary<string, object?>>>();
        command.Aggregation.ShouldNotBeNull().GroupByFields.ShouldBe([Column]);
        command.Aggregation.Aggregations.Values.ShouldBe(["COUNT(*)"]);
        command.Paging.ShouldNotBeNull().Skip.ShouldBe(10);
        command.Paging.Take.ShouldBe(13);
        var ordered = command.Ordering.ShouldNotBeNull().OrderedFields.Single();
        ordered.PropertyName.ShouldBe(Column);
        ordered.Direction.IsAscending.ShouldBeTrue();
        Conditions(command).ShouldContain(c => c.PropertyName == Column && c.Operator.Name == "Contains" && Equals(c.Value, "ber"));
        Conditions(command).ShouldContain(c => c.PropertyName == Column && c.Operator.Name == "IsNotNull");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task EachValueCarriesHowManyRowsHoldIt()
    {
        var fixture = new Fixture([Row("Berlin", 5), Row("Bern", 2)]);

        var result = await fixture.Service.Search(Query(search: "ber", take: 12), TestContext.Current.CancellationToken);

        result.Value!.Items.Select(v => (v.Value, v.Rows)).ShouldBe([("Berlin", 5L), ("Bern", 2L)]);
        result.Value.HasMore.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task MoreIsSaidWhenTheStoreAnswersOneValueBeyondThePage()
    {
        var fixture = new Fixture([Row("Berlin", 5), Row("Bern", 2), Row("Bergen", 1)]);

        var result = await fixture.Service.Search(Query(take: 2), TestContext.Current.CancellationToken);

        result.Value!.Items.Select(v => v.Value).ShouldBe(["Berlin", "Bern"]);
        result.Value.HasMore.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task NoSearchTextSendsNoContainsMatch()
    {
        var fixture = new Fixture([]);

        await fixture.Service.Search(Query(take: 12), TestContext.Current.CancellationToken);

        Conditions(fixture.Command).ShouldNotContain(c => c.Operator.Name == "Contains");
    }

    // The store escapes the text (LikePattern); the service sends it as the person typed it, never as a pattern of its own.
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TextWithWildcardCharactersIsSentAsTyped()
    {
        var fixture = new Fixture([]);

        await fixture.Service.Search(Query(search: "50%_[x]", take: 12), TestContext.Current.CancellationToken);

        Conditions(fixture.Command).ShouldContain(c => c.Operator.Name == "Contains" && Equals(c.Value, "50%_[x]"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AChosenValueIsMarkedOnThePage()
    {
        var fixture = new Fixture([Row("Berlin", 5), Row("Bern", 2)]);

        var result = await fixture.Service.Search(Query(take: 12, chosen: ["Bern"]), TestContext.Current.CancellationToken);

        result.Value!.Items.Select(v => (v.Value, v.Chosen)).ShouldBe([("Berlin", false), ("Bern", true)]);
        Conditions(fixture.Command).ShouldNotContain(c => c.Operator.Name == "NotIn");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ChosenValuesAreLeftOutByTheStoreWhenAsked()
    {
        var fixture = new Fixture([Row("Berlin", 5)]);

        await fixture.Service.Search(Query(take: 12, chosen: ["Bern", "Bergen"], leaveOutChosen: true), TestContext.Current.CancellationToken);

        var notIn = Conditions(fixture.Command).Single(c => c.Operator.Name == "NotIn");
        notIn.PropertyName.ShouldBe(Column);
        ((IEnumerable<object?>)notIn.Value!).Cast<string>().ShouldBe(["Bern", "Bergen"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ARequestThatStatesNoTakeGetsTheDeclaredDefault()
    {
        var fixture = new Fixture([], new ColumnValuesPaging(defaultTake: 12, maximumTake: 200));

        await fixture.Service.Search(Query(), TestContext.Current.CancellationToken);

        fixture.Command.Paging!.Take.ShouldBe(13);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ATakeAboveTheMaximumIsRefusedNotCut()
    {
        var fixture = new Fixture([], new ColumnValuesPaging(defaultTake: 12, maximumTake: 50));

        var result = await fixture.Service.Search(Query(take: 51), TestContext.Current.CancellationToken);

        Refused(result, "DATA-21660");
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ATakeOfZeroIsRefused()
    {
        var fixture = new Fixture([]);

        Refused(await fixture.Service.Search(Query(take: 0), TestContext.Current.CancellationToken), "DATA-21661");
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task ANegativeSkipIsRefused()
    {
        var fixture = new Fixture([]);

        Refused(await fixture.Service.Search(Query(skip: -1, take: 12), TestContext.Current.CancellationToken), "DATA-21662");
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task AnEmptyChosenValueIsRefused()
    {
        var fixture = new Fixture([]);

        Refused(await fixture.Service.Search(Query(take: 12, chosen: ["Bern", " "]), TestContext.Current.CancellationToken), "DATA-21663");
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AColumnTheContainerDoesNotHaveIsRefusedBeforeTheStoreIsAsked()
    {
        var fixture = new Fixture([]);

        var result = await fixture.Service.Search(Query(column: "Country", take: 12), TestContext.Current.CancellationToken);

        Refused(result, "DATA-31660");
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AContainerThatCannotBeReachedAnswersItsOwnFailure()
    {
        var unreachable = GenericResult<IDataContainer>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));
        var fixture = new Fixture([], containerAnswer: unreachable);

        var result = await fixture.Service.Search(Query(take: 12), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(DataServiceResultCodes.ByName("DataStoreNameRequired"));
        fixture.Sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task AReadTheGatewayRefusesAnswersTheGatewaysFailure()
    {
        var refused = GenericResult<IEnumerable<Dictionary<string, object?>>>.Failure(DataServiceResultCodes.ByName("DataStoreNameRequired"));
        var fixture = new Fixture([], gatewayAnswer: refused);

        var result = await fixture.Service.Search(Query(take: 12), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeSameAs(DataServiceResultCodes.ByName("DataStoreNameRequired"));
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Api")]
    public async Task TheCallersTokenReachesTheStoreAndTheGateway()
    {
        using var source = new CancellationTokenSource();
        var fixture = new Fixture([]);

        await fixture.Service.Search(Query(take: 12), source.Token);

        fixture.Tokens.ShouldAllBe(t => t == source.Token);
        fixture.Tokens.Count.ShouldBe(2);
    }

    private static void Refused(IGenericResult<ColumnValuesPage> result, string code)
    {
        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(m => m.Code).ShouldContain(code);
    }

    private static ColumnValuesQuery Query(
        string column = Column, string? search = null, int skip = 0, int? take = null, IReadOnlyList<string>? chosen = null, bool leaveOutChosen = false)
        => new()
        {
            DataStore = Store,
            Path = Path,
            Container = Container,
            Column = column,
            Search = search,
            Skip = skip,
            Take = take,
            Chosen = chosen ?? [],
            LeaveOutChosen = leaveOutChosen,
        };

    private static Dictionary<string, object?> Row(string value, int rows) => new() { [Column] = value, [ContainerColumnValues.RowsColumn] = rows };

    private static List<FilterCondition> Conditions(QueryCommand<Dictionary<string, object?>> command)
        => Flatten(command.Filter?.Root).ToList();

    private static IEnumerable<FilterCondition> Flatten(IFilterNode? node) => node switch
    {
        FilterCondition condition => [condition],
        FilterGroup group => group.Nodes.SelectMany(Flatten),
        _ => [],
    };

    private sealed class Fixture
    {
        public Fixture(
            IReadOnlyList<Dictionary<string, object?>> rows,
            ColumnValuesPaging? paging = null,
            IGenericResult<IDataContainer>? containerAnswer = null,
            IGenericResult<IEnumerable<Dictionary<string, object?>>>? gatewayAnswer = null)
        {
            var field = new Mock<IField>(MockBehavior.Strict);
            field.Setup(f => f.Name).Returns(Column);
            var schema = new Mock<IContainerSchema>(MockBehavior.Strict);
            schema.Setup(s => s.Fields).Returns([field.Object]);
            var container = new Mock<IDataContainer>(MockBehavior.Strict);
            container.Setup(c => c.Schema).Returns(schema.Object);

            var stores = new Mock<IDataStoreProvider>(MockBehavior.Strict);
            stores.Setup(s => s.Get(Store, Path, Container, It.IsAny<CancellationToken>()))
                .Callback<string, string, string, CancellationToken>((_, _, _, token) => Tokens.Add(token))
                .ReturnsAsync(containerAnswer ?? GenericResult<IDataContainer>.Success(container.Object));

            var gateway = new Mock<IDataGateway>(MockBehavior.Strict);
            gateway.Setup(g => g.Execute<IEnumerable<Dictionary<string, object?>>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
                .Callback<IDataCommand, DataStoreTarget, CancellationToken>((command, target, token) =>
                {
                    Sent.Add(command);
                    Targets.Add(target);
                    Tokens.Add(token);
                })
                .ReturnsAsync(gatewayAnswer ?? GenericResult<IEnumerable<Dictionary<string, object?>>>.Success(rows));
            var gateways = new Mock<IDataGatewayProvider>(MockBehavior.Strict);
            gateways.Setup(p => p.ByName(DataGatewayNames.Main)).Returns(gateway.Object);

            Service = new ContainerColumnValues(
                NullLogger<ContainerColumnValues>.Instance, gateways.Object, stores.Object, paging ?? new ColumnValuesPaging(defaultTake: 12, maximumTake: 200));
        }

        public ContainerColumnValues Service { get; }

        public List<IDataCommand> Sent { get; } = [];

        public List<DataStoreTarget> Targets { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];

        public QueryCommand<Dictionary<string, object?>> Command => Sent.Single().ShouldBeOfType<QueryCommand<Dictionary<string, object?>>>();
    }
}
