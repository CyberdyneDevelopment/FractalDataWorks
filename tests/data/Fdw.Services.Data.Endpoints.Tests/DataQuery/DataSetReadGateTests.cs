using Fdw.Data.DataSets.Abstractions;

using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests.DataQuery;

/// <summary>
/// FDW-1054: every route that reads a data set asks its kind the same two questions, so a Federated, Compound or aggregating
/// set is refused by name instead of answered from its first source with the wrong rows.
/// </summary>
public sealed class DataSetReadGateTests
{
    private static SimpleDataSetConfiguration Simple(params DataSetAggregateConfiguration[] aggregates)
        => new() { Name = "NflTeams", Implementation = "Simple", Aggregates = [.. aggregates] };

    [Fact]
    public void ASimpleSetPagesAndGroupsInTheStore()
    {
        DataSetReadGate.Pageable(Simple(), NullLogger.Instance).IsSuccess.ShouldBeTrue();
        DataSetReadGate.Groupable(Simple(), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AFederatedSetIsRefusedForPagingBecauseItsJoinIsInMemory()
    {
        var federated = new FederatedDataSetConfiguration { Name = "BreweriesInNflCities", Implementation = "Federated" };

        var result = DataSetReadGate.Pageable(federated, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("kind 'Federated' does not page in the store");
    }

    [Fact]
    public void ACompoundSetPagesInItsStoreAndIsStillRefusedForGrouping()
    {
        var compound = new CompoundDataSetConfiguration { Name = "TeamsWithStadiums", Implementation = "Compound" };

        // FDW-4991: one store, one joined query, so it pages there; a grouping over a join is still refused.
        DataSetReadGate.Pageable(compound, NullLogger.Instance).IsSuccess.ShouldBeTrue();
        DataSetReadGate.Groupable(compound, NullLogger.Instance).CurrentMessage.ShouldNotBeNull().ShouldContain("kind 'Compound' does not run a grouped query in the store");
    }

    [Fact]
    public void ASetThatAggregatesIsRefusedForPagingAndForGroupingAgain()
    {
        var aggregating = Simple(new DataSetAggregateConfiguration());

        DataSetReadGate.Pageable(aggregating, NullLogger.Instance).CurrentMessage.ShouldNotBeNull().ShouldContain("it aggregates");
        DataSetReadGate.Groupable(aggregating, NullLogger.Instance).CurrentMessage.ShouldNotBeNull().ShouldContain("it already defines aggregates");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5300")]
    public void AFederatedSetIsStillRefusedByTheRowRoutesGateBecauseItSaysNothingAboutShapingItsOwnRead()
    {
        var federated = new FederatedDataSetConfiguration { Name = "BreweriesInNflCities", Implementation = "Federated" };

        var result = DataSetReadGate.Readable(federated, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("kind 'Federated' does not page in the store");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5300")]
    public void AGraphSetIsAnsweredAPagedQueryBecauseItShapesItsOwnReadYetItDoesNotPageInTheStore()
    {
        var graph = new GraphDataSetConfiguration { Name = "GasPrices", Implementation = "Graph" };

        DataSetReadGate.Readable(graph, NullLogger.Instance).IsSuccess.ShouldBeTrue();
        DataSetReadGate.Pageable(graph, NullLogger.Instance).IsFailure.ShouldBeTrue();
        DataSetTypes.ByName("Graph").PagesInStore.ShouldBeFalse();
        DataSetReadGate.Groupable(graph, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5300")]
    public void TheKindsThatPageInTheStoreAreAnsweredAsBeforeAndAnUnknownKindIsStillRefused()
    {
        DataSetReadGate.Readable(Simple(), NullLogger.Instance).IsSuccess.ShouldBeTrue();
        DataSetReadGate.Readable(new CompoundDataSetConfiguration { Name = "TeamsWithStadiums", Implementation = "Compound" }, NullLogger.Instance).IsSuccess.ShouldBeTrue();
        DataSetReadGate.Readable(Simple(new DataSetAggregateConfiguration()), NullLogger.Instance).IsFailure.ShouldBeTrue();
        DataSetReadGate.Readable(new SimpleDataSetConfiguration { Name = "Mystery", Implementation = "NotAKind" }, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5300")]
    public void ASetThatShapesItsOwnReadHasTheCapabilityAndNoOtherShippedKindDoes()
    {
        DataSetTypes.ByName("Graph").ShouldBeAssignableTo<IShapesItsOwnRead>().ShapesItsOwnRead.ShouldBeTrue();
        DataSetTypes.ByName("Simple").ShouldNotBeAssignableTo<IShapesItsOwnRead>();
        DataSetTypes.ByName("Compound").ShouldNotBeAssignableTo<IShapesItsOwnRead>();
        DataSetTypes.ByName("Federated").ShouldNotBeAssignableTo<IShapesItsOwnRead>();
        DataSetTypes.ByName("Variation").ShouldNotBeAssignableTo<IShapesItsOwnRead>();
    }

    [Fact]
    public void AnUnknownKindIsRefusedBecauseNothingKnowsHowItPages()
    {
        var unknown = new SimpleDataSetConfiguration { Name = "Mystery", Implementation = "NotAKind" };

        DataSetReadGate.Pageable(unknown, NullLogger.Instance).IsFailure.ShouldBeTrue();
        DataSetReadGate.Groupable(unknown, NullLogger.Instance).IsFailure.ShouldBeTrue();
    }
}
